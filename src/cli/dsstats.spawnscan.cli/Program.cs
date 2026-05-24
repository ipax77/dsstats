using System.Diagnostics;
using System.Globalization;
using dsstats.parser;
using Sc2DirectStrike.Parser;

namespace dsstats.spawnscan.cli;

internal static class Program
{
    private const string DefaultReplayPath =
        @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer";
    private const string ReplayFilter = "Direct Strike*.SC2Replay";
    private const double GameloopsPerSecond = 22.4;
    private const int MapWidth = 256;
    private const int MapHeight = 240;
    private const int MiddleLineSum = MapWidth / 2 + MapHeight / 2;

    public static async Task<int> Main(string[] args)
    {
        string inputPath = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : DefaultReplayPath;
        int limit = ParsePositiveInt(args, 1, 25);

        IReadOnlyList<string> replayPaths;
        try
        {
            replayPaths = GetReplayPaths(inputPath, limit);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        Console.WriteLine("Direct Strike spawn timing scan");
        Console.WriteLine($"Input: {inputPath}");
        Console.WriteLine($"Replays: {replayPaths.Count:N0}");
        Console.WriteLine();
        Console.WriteLine("Replay | DecodeMs | ParseMs | Players | FirstMid | SpawnIntervalGL | SpawnWindowGL | FirstSpawns | SpeedWU/GL | SpeedWU/Sec | Candidate | Lifetimes");

        int failures = 0;
        foreach (string replayPath in replayPaths)
        {
            try
            {
                ScanResult result = await ScanReplay(replayPath);
                PrintResult(result);
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"{Path.GetFileName(replayPath)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private static IReadOnlyList<string> GetReplayPaths(string inputPath, int limit)
    {
        if (File.Exists(inputPath))
        {
            return [Path.GetFullPath(inputPath)];
        }

        if (!Directory.Exists(inputPath))
        {
            throw new DirectoryNotFoundException($"Replay path not found: {inputPath}");
        }

        return Directory.EnumerateFiles(inputPath, ReplayFilter, SearchOption.AllDirectories)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static async Task<ScanResult> ScanReplay(string replayPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath)
            ?? throw new InvalidOperationException("Replay decoder returned null.");
        long decodeMs = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        DirectStrikeReplay replay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
        long parseMs = stopwatch.ElapsedMilliseconds;

        SpawnTimingStats spawnStats = GetSpawnTimingStats(replay);
        MiddleSpeedEstimate? speedEstimate = GetMiddleSpeedEstimate(replay);

        return new(
            Path.GetFileName(replayPath),
            decodeMs,
            parseMs,
            replay.Players.Count,
            replay.FirstMiddleControlTeam,
            replay.MiddleChanges.Length > 0 ? ToGameloop(replay.MiddleChanges[0]) : 0,
            spawnStats,
            speedEstimate,
            GetLifetimeSummary(replay));
    }

    private static SpawnTimingStats GetSpawnTimingStats(DirectStrikeReplay replay)
    {
        List<int> intervals = [];
        List<int> windows = [];
        List<string> firstSpawns = new(replay.Players.Count);

        foreach (DirectStrikePlayer player in replay.Players)
        {
            DirectStrikePlayerSpawn? previousSpawn = null;
            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                if (spawn.Units.Count == 0)
                {
                    continue;
                }

                windows.Add(spawn.EndGameloop - spawn.StartGameloop);
                if (previousSpawn is not null)
                {
                    intervals.Add(spawn.StartGameloop - previousSpawn.StartGameloop);
                }
                else
                {
                    firstSpawns.Add($"P{player.GamePos}:{spawn.StartGameloop}-{spawn.EndGameloop}");
                }

                previousSpawn = spawn;
            }
        }

        return new(
            ToRange(intervals),
            ToRange(windows),
            string.Join(", ", firstSpawns));
    }

    private static MiddleSpeedEstimate? GetMiddleSpeedEstimate(DirectStrikeReplay replay)
    {
        if (replay.FirstMiddleControlTeam is not (1 or 2) || replay.MiddleChanges.Length == 0)
        {
            return null;
        }

        int firstMiddleGameloop = ToGameloop(replay.MiddleChanges[0]);
        List<MiddleCandidate> candidates = [];

        foreach (DirectStrikePlayer player in replay.Players)
        {
            if (player.TeamId != replay.FirstMiddleControlTeam)
            {
                continue;
            }

            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                foreach (DirectStrikeSpawnUnit unit in spawn.Units)
                {
                    if (unit.Gameloop >= firstMiddleGameloop)
                    {
                        continue;
                    }

                    double distance = GetDistanceToMiddle(unit.X, unit.Y);
                    if (distance <= 0)
                    {
                        continue;
                    }

                    double speed = distance / (firstMiddleGameloop - unit.Gameloop);
                    if (IsFirstMiddleCandidate(replay, firstMiddleGameloop, speed))
                    {
                        candidates.Add(new(player.GamePos, unit.Name, unit.Gameloop, unit.X, unit.Y, distance, speed));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        candidates.Sort(static (left, right) =>
        {
            int speedComparison = left.SpeedWorldUnitsPerGameloop.CompareTo(right.SpeedWorldUnitsPerGameloop);
            if (speedComparison != 0)
            {
                return speedComparison;
            }

            int gameloopComparison = left.SpawnGameloop.CompareTo(right.SpawnGameloop);
            return gameloopComparison != 0
                ? gameloopComparison
                : string.Compare(left.UnitName, right.UnitName, StringComparison.Ordinal);
        });

        MiddleCandidate candidate = candidates[0];
        return new(
            candidate.SpeedWorldUnitsPerGameloop,
            candidate.SpeedWorldUnitsPerGameloop * GameloopsPerSecond,
            string.Create(
                CultureInfo.InvariantCulture,
                $"P{candidate.GamePos} {candidate.UnitName} @{candidate.SpawnGameloop} ({candidate.X},{candidate.Y}) d={candidate.DistanceToMiddle:0.00}"));
    }

    private static bool IsFirstMiddleCandidate(DirectStrikeReplay replay, int firstMiddleGameloop, double speed)
    {
        foreach (DirectStrikePlayer player in replay.Players)
        {
            if (player.TeamId != replay.FirstMiddleControlTeam)
            {
                continue;
            }

            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                foreach (DirectStrikeSpawnUnit unit in spawn.Units)
                {
                    if (unit.Gameloop >= firstMiddleGameloop)
                    {
                        continue;
                    }

                    double crossingGameloop = unit.Gameloop + GetDistanceToMiddle(unit.X, unit.Y) / speed;
                    if (crossingGameloop < firstMiddleGameloop - 0.5)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static string GetLifetimeSummary(DirectStrikeReplay replay)
    {
        Dictionary<string, LifetimeBuilder> builders = new(StringComparer.Ordinal);
        foreach (DirectStrikePlayer player in replay.Players)
        {
            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                foreach (DirectStrikeSpawnUnit unit in spawn.Units)
                {
                    string key = $"T{player.TeamId}:{unit.Name}";
                    if (!builders.TryGetValue(key, out LifetimeBuilder? builder))
                    {
                        builder = new(key);
                        builders.Add(key, builder);
                    }

                    if (unit.DiedGameloop is int diedGameloop)
                    {
                        builder.AddLifetime(unit.Gameloop, diedGameloop);
                    }
                    else
                    {
                        builder.MissingDeathCount++;
                    }
                }
            }
        }

        if (builders.Count == 0)
        {
            return "-";
        }

        List<LifetimeBuilder> sortedBuilders = [.. builders.Values];
        sortedBuilders.Sort(static (left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));

        List<string> summaries = new(sortedBuilders.Count);
        foreach (LifetimeBuilder builder in sortedBuilders)
        {
            summaries.Add(builder.ToSummary());
        }

        return string.Join("; ", summaries);
    }

    private static double GetDistanceToMiddle(int x, int y)
    {
        double startSum = x + y;
        double targetSum = MapWidth + MapHeight - startSum;
        double denominator = startSum - targetSum;
        if (Math.Abs(denominator) < double.Epsilon)
        {
            return 0;
        }

        double progress = (startSum - MiddleLineSum) / denominator;
        progress = Math.Clamp(progress, 0, 1);
        double targetX = MapWidth - x;
        double targetY = MapHeight - y;
        double dx = targetX - x;
        double dy = targetY - y;
        return Math.Sqrt(dx * dx + dy * dy) * progress;
    }

    private static string ToRange(List<int> values)
    {
        if (values.Count == 0)
        {
            return "-";
        }

        values.Sort();
        int median = values[values.Count / 2];
        return $"{values[0]}..{values[^1]} med {median}";
    }

    private static void PrintResult(ScanResult result)
    {
        string firstMid = result.FirstMiddleGameloop > 0
            ? $"T{result.FirstMiddleTeam}@{result.FirstMiddleGameloop}"
            : "-";
        string speed = result.SpeedEstimate is null
            ? "-"
            : result.SpeedEstimate.SpeedWorldUnitsPerGameloop.ToString("0.0000", CultureInfo.InvariantCulture);
        string speedPerSecond = result.SpeedEstimate is null
            ? "-"
            : result.SpeedEstimate.SpeedWorldUnitsPerSecond.ToString("0.00", CultureInfo.InvariantCulture);
        string candidate = result.SpeedEstimate?.Candidate ?? "-";
        string lifetimes = result.LifetimeSummary.Length == 0 ? "-" : result.LifetimeSummary;

        Console.WriteLine(
            $"{result.ReplayName} | {result.DecodeMs} | {result.ParseMs} | {result.PlayerCount} | {firstMid} | {result.SpawnStats.IntervalRange} | {result.SpawnStats.WindowRange} | {result.SpawnStats.FirstSpawns} | {speed} | {speedPerSecond} | {candidate} | {lifetimes}");
    }

    private static int ParsePositiveInt(string[] args, int index, int fallback)
    {
        return args.Length > index
            && int.TryParse(args[index], out int parsed)
            && parsed > 0
            ? parsed
            : fallback;
    }

    private static int ToGameloop(TimeSpan value)
    {
        return value <= TimeSpan.Zero ? 0 : (int)Math.Round(value.TotalSeconds * GameloopsPerSecond);
    }

    private sealed record ScanResult(
        string ReplayName,
        long DecodeMs,
        long ParseMs,
        int PlayerCount,
        int FirstMiddleTeam,
        int FirstMiddleGameloop,
        SpawnTimingStats SpawnStats,
        MiddleSpeedEstimate? SpeedEstimate,
        string LifetimeSummary);

    private sealed record SpawnTimingStats(
        string IntervalRange,
        string WindowRange,
        string FirstSpawns);

    private sealed record MiddleSpeedEstimate(
        double SpeedWorldUnitsPerGameloop,
        double SpeedWorldUnitsPerSecond,
        string Candidate);

    private readonly record struct MiddleCandidate(
        int GamePos,
        string UnitName,
        int SpawnGameloop,
        int X,
        int Y,
        double DistanceToMiddle,
        double SpeedWorldUnitsPerGameloop);

    private sealed class LifetimeBuilder(string key)
    {
        private readonly List<int> lifetimes = [];

        public string Key { get; } = key;

        public int MissingDeathCount { get; set; }

        public void AddLifetime(int spawnGameloop, int diedGameloop)
        {
            lifetimes.Add(diedGameloop - spawnGameloop);
            FirstSpawnGameloop ??= spawnGameloop;
            FirstDiedGameloop ??= diedGameloop;
        }

        private int? FirstSpawnGameloop { get; set; }

        private int? FirstDiedGameloop { get; set; }

        public string ToSummary()
        {
            if (lifetimes.Count == 0)
            {
                return $"{Key} no death x{MissingDeathCount}";
            }

            lifetimes.Sort();
            string firstRange = FirstSpawnGameloop is int spawnGameloop && FirstDiedGameloop is int diedGameloop
                ? $" first {spawnGameloop}->{diedGameloop}"
                : string.Empty;
            string missingDeaths = MissingDeathCount > 0
                ? $" missing {MissingDeathCount}"
                : string.Empty;

            return $"{Key} n={lifetimes.Count} life {lifetimes[0]}..{lifetimes[^1]} med {lifetimes[lifetimes.Count / 2]}{firstRange}{missingDeaths}";
        }
    }
}
