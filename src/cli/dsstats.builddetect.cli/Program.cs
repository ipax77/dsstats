using System.Collections.Concurrent;
using dsstats.parser;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using dsstats.dbServices;

namespace dsstats.builddetect.cli;

internal static class Program
{
    private const string DefaultReplayDirectory =
        @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer";

    private const string ReplayFilter = "Direct Strike TE*.SC2Replay";
    private const int DefaultLimit = 100;

    public static async Task<int> Main(string[] args)
    {
        var replayDirectory = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : DefaultReplayDirectory;
        var limit = ParsePositiveInt(args, 1, DefaultLimit);
        var maxParallelism = ParsePositiveInt(args, 2, Environment.ProcessorCount);
        var inspectBuildNone = args.Length > 3
            && (string.Equals(args[3], "build-none", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[3], "protoss-none", StringComparison.OrdinalIgnoreCase));

        if (!Directory.Exists(replayDirectory))
        {
            Console.Error.WriteLine($"Replay directory not found: {replayDirectory}");
            return 1;
        }

        var replayFiles = Directory
            .EnumerateFiles(replayDirectory, ReplayFilter, SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        if (replayFiles.Length == 0)
        {
            Console.Error.WriteLine($"No replays found matching '{ReplayFilter}' in: {replayDirectory}");
            return 1;
        }

        var selectedFiles = replayFiles
            .Take(limit)
            .ToArray();

        var result = new ScanResult();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
        };

        await Parallel.ForEachAsync(selectedFiles, parallelOptions, async (file, _) =>
        {
            await ProcessReplay(file, result, inspectBuildNone);
        });

        PrintSummary(replayDirectory, replayFiles.Length, selectedFiles.Length, maxParallelism, result);
        return 0;
    }

    private static int ParsePositiveInt(string[] args, int index, int defaultValue)
    {
        if (args.Length <= index || !int.TryParse(args[index], out var value) || value <= 0)
        {
            return defaultValue;
        }

        return value;
    }

    private static async Task ProcessReplay(FileInfo file, ScanResult result, bool inspectBuildNone)
    {
        Interlocked.Increment(ref result.Attempted);

        try
        {
            var sc2Replay = await DsstatsParser.GetSc2Replay(file.FullName);
            if (sc2Replay is null)
            {
                result.AddFailure(file.Name, "Replay decoder returned no data.");
                return;
            }

            var replayDto = DsstatsParser.ParseReplay(sc2Replay);
            ImportService.AdjustReplayResult(replayDto);
            var details = DetailBuilds.DetectStandardBuild(replayDto);
            if (details is null)
            {
                Interlocked.Increment(ref result.Skipped);
                return;
            }

            Interlocked.Increment(ref result.Detected);
            CountPlayerBuilds(details, result);
            CountTeamBuilds(details, result);

            if (inspectBuildNone)
            {
                InspectBuildNone(file, replayDto, details, result);
            }
        }
        catch (Exception ex)
        {
            result.AddFailure(file.Name, ex.Message);
        }
    }

    private static void CountPlayerBuilds(ReplayBuildDetails details, ScanResult result)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            result.IncrementPlayerBuild(matchup.P1.Commander, matchup.P1.BuildName);
            result.IncrementPlayerBuild(matchup.P2.Commander, matchup.P2.BuildName);
        }
    }

    private static void CountTeamBuilds(ReplayBuildDetails details, ScanResult result)
    {
        foreach (var teamBuildInfo in details.TeamBuildInfos)
        {
            result.IncrementTeamBuild(teamBuildInfo.TeamBuildName);
        }
    }

    private static void InspectBuildNone(FileInfo file, ReplayDto replayDto, ReplayBuildDetails details, ScanResult result)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            InspectBuildNone(file, replayDto, matchup.P1, result);
            InspectBuildNone(file, replayDto, matchup.P2, result);
        }
    }

    private static void InspectBuildNone(FileInfo file, ReplayDto replayDto, PlayerBuildInfo buildInfo, ScanResult result)
    {
        if (!string.Equals(buildInfo.BuildName, "None", StringComparison.Ordinal))
        {
            return;
        }

        var player = replayDto.Players.FirstOrDefault(player => player.GamePos == buildInfo.GamePos);
        var spawn = player?.Spawns.FirstOrDefault(spawn => spawn.Breakpoint == Breakpoint.Min5);
        var units = spawn is null || spawn.Units.Count == 0
            ? "(no Min5 units)"
            : string.Join(", ", spawn.Units
                .OrderBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase)
                .Select(unit => $"{unit.Name}:{unit.Count}"));

        result.BuildNoneDetails.Add(
            $"{file.Name} | {file.LastWriteTime:yyyy-MM-dd HH:mm:ss} | pos {buildInfo.GamePos} | {buildInfo.Commander} | {player?.Name ?? "(unknown)"} | {units}");
    }

    private static void PrintSummary(
        string replayDirectory,
        int discovered,
        int selected,
        int maxParallelism,
        ScanResult result)
    {
        Console.WriteLine("Direct Strike TE detail build scan");
        Console.WriteLine($"Directory: {replayDirectory}");
        Console.WriteLine($"Filter: {ReplayFilter}");
        Console.WriteLine($"Discovered: {discovered}");
        Console.WriteLine($"Attempted: {result.Attempted} of {selected}");
        Console.WriteLine($"Detected: {result.Detected}");
        Console.WriteLine($"Skipped/not detected: {result.Skipped}");
        Console.WriteLine($"Failed: {result.Failed}");
        Console.WriteLine($"Max parallelism: {maxParallelism}");
        Console.WriteLine();

        PrintCounts("Player builds", result.PlayerBuildCounts);
        PrintCounts("Team builds", result.TeamBuildCounts);

        if (!result.Failures.IsEmpty)
        {
            Console.WriteLine("Failures");
            foreach (var failure in result.Failures.OrderBy(failure => failure, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  {failure}");
            }
        }

        if (!result.BuildNoneDetails.IsEmpty)
        {
            Console.WriteLine("Build.None details");
            foreach (var detail in result.BuildNoneDetails.OrderBy(detail => detail, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  {detail}");
            }
        }
    }

    private static void PrintCounts(string title, ConcurrentDictionary<string, int> counts)
    {
        Console.WriteLine(title);
        if (counts.IsEmpty)
        {
            Console.WriteLine("  (none)");
            Console.WriteLine();
            return;
        }

        foreach (var pair in counts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  {pair.Key}: {pair.Value}");
        }

        Console.WriteLine();
    }

    private sealed class ScanResult
    {
        public int Attempted;
        public int Detected;
        public int Skipped;
        public int Failed;

        public ConcurrentDictionary<string, int> PlayerBuildCounts { get; } = new(StringComparer.Ordinal);
        public ConcurrentDictionary<string, int> TeamBuildCounts { get; } = new(StringComparer.Ordinal);
        public ConcurrentBag<string> Failures { get; } = [];
        public ConcurrentBag<string> BuildNoneDetails { get; } = [];

        public void IncrementPlayerBuild(Enum commander, string buildName)
        {
            PlayerBuildCounts.AddOrUpdate($"{commander}.{buildName}", 1, static (_, count) => count + 1);
        }

        public void IncrementTeamBuild(string teamBuildName)
        {
            TeamBuildCounts.AddOrUpdate(teamBuildName, 1, static (_, count) => count + 1);
        }

        public void AddFailure(string fileName, string message)
        {
            Interlocked.Increment(ref Failed);
            Failures.Add($"{fileName}: {message}");
        }
    }
}
