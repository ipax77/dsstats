using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using dsstats.db;
using dsstats.parser;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using dsstats.shared.Units;
using dsstats.dbServices;
using Microsoft.EntityFrameworkCore;

namespace dsstats.builddetect.cli;

internal static class Program
{
    private const string DefaultReplayDirectory =
        @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer";

    private const string ReplayFilter = "Direct Strike TE*.SC2Replay";
    private const string DefaultDevelopmentSettingsPath = @"src\server\dsstats.api\appsettings.Development.json";
    private const int DefaultLimit = 1000;
    private const int DefaultDbBatchSize = 1000;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && IsFilesMode(args[0]))
        {
            return await RunFileScan(args);
        }

        return await RunDatabaseScan(args);
    }

    private static async Task<int> RunDatabaseScan(string[] args)
    {
        var limit = ParsePositiveInt(args, 0, DefaultLimit);
        var batchSize = ParsePositiveInt(args, 1, DefaultDbBatchSize);
        var inspectBuildNone = IsBuildNoneInspection(args, 2);
        var inspectTerranMech = IsTerranMechInspection(args, 2);
        var connectionString = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3])
            ? args[3]
            : GetDefaultConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                $"Dev DB connection string not found. Expected dsstats:ConnectionString in {DefaultDevelopmentSettingsPath}, or pass it as args[3].");
            return 1;
        }

        var result = new ScanResult();
        int discovered;
        int selected;

        try
        {
            var options = CreateDbOptions(connectionString);
            await using var context = new DsstatsContext(options);

            discovered = await GetReplayQuery(context).CountAsync();
            if (discovered == 0)
            {
                Console.Error.WriteLine("No Direct Strike TE replays found in the dev database.");
                return 1;
            }

            selected = Math.Min(discovered, limit);
            int? beforeReplayId = null;

            while (result.Attempted < selected)
            {
                var take = Math.Min(batchSize, selected - result.Attempted);
                var batch = await FetchReplayBatch(context, beforeReplayId, take);
                if (batch.Count == 0)
                {
                    break;
                }

                foreach (var replay in batch)
                {
                    ProcessReplay(replay, result, inspectBuildNone, inspectTerranMech);
                }

                beforeReplayId = batch[^1].ReplayId;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Dev DB scan failed: {GetInnermostMessage(ex)}");
            return 1;
        }

        PrintSummary(
            source: $"Dev database{Environment.NewLine}Connection string: {connectionString}",
            discovered: discovered,
            selected: selected,
            batchInfo: $"DB batch size: {batchSize}",
            result: result);

        return 0;
    }

    private static async Task<int> RunFileScan(string[] args)
    {
        var replayDirectory = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1]
            : DefaultReplayDirectory;
        var limit = ParsePositiveInt(args, 2, DefaultLimit);
        var maxParallelism = ParsePositiveInt(args, 3, Environment.ProcessorCount);
        var inspectBuildNone = IsBuildNoneInspection(args, 4);
        var inspectTerranMech = IsTerranMechInspection(args, 4);

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
            await ProcessReplay(file, result, inspectBuildNone, inspectTerranMech);
        });

        PrintSummary(
            source: $"Replay directory: {replayDirectory}{Environment.NewLine}Filter: {ReplayFilter}",
            discovered: replayFiles.Length,
            selected: selectedFiles.Length,
            batchInfo: $"Max parallelism: {maxParallelism}",
            result: result);

        return 0;
    }

    private static bool IsFilesMode(string value)
    {
        return string.Equals(value, "files", StringComparison.OrdinalIgnoreCase)
            || Directory.Exists(value);
    }

    private static bool IsBuildNoneInspection(string[] args, int index)
    {
        return args.Length > index
            && (string.Equals(args[index], "build-none", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[index], "protoss-none", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTerranMechInspection(string[] args, int index)
    {
        return args.Length > index
            && (string.Equals(args[index], "terran-mech", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[index], "mech", StringComparison.OrdinalIgnoreCase));
    }

    private static int ParsePositiveInt(string[] args, int index, int defaultValue)
    {
        if (args.Length <= index || !int.TryParse(args[index], out var value) || value <= 0)
        {
            return defaultValue;
        }

        return value;
    }

    private static async Task ProcessReplay(
        FileInfo file,
        ScanResult result,
        bool inspectBuildNone,
        bool inspectTerranMech)
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
                InspectBuildNone(file.Name, file.LastWriteTime, replayDto, details, result);
            }

            if (inspectTerranMech)
            {
                InspectTerranMech(file.Name, file.LastWriteTime, replayDto, details, result);
            }
        }
        catch (Exception ex)
        {
            result.AddFailure(file.Name, ex.Message);
        }
    }

    private static void ProcessReplay(
        DatabaseReplayDto replay,
        ScanResult result,
        bool inspectBuildNone,
        bool inspectTerranMech)
    {
        Interlocked.Increment(ref result.Attempted);

        try
        {
            var details = DetailBuilds.DetectStandardBuild(replay.Replay);
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
                InspectBuildNone(replay.DisplayName, replay.Replay.Gametime, replay.Replay, details, result);
            }

            if (inspectTerranMech)
            {
                InspectTerranMech(replay.DisplayName, replay.Replay.Gametime, replay.Replay, details, result);
            }
        }
        catch (Exception ex)
        {
            result.AddFailure(replay.DisplayName, ex.Message);
        }
    }

    private static void CountPlayerBuilds(ReplayBuildDetails details, ScanResult result)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            result.IncrementPlayerBuild(matchup.P1, matchup.P2, matchup.P1Won);
            result.IncrementPlayerBuild(matchup.P2, matchup.P1, matchup.P2Won);
        }
    }

    private static void CountTeamBuilds(ReplayBuildDetails details, ScanResult result)
    {
        foreach (var teamBuildInfo in details.TeamBuildInfos)
        {
            result.IncrementTeamBuild(
                teamBuildInfo.TeamBuildName,
                IsPlayerBuildWinner(details, teamBuildInfo.LeaderGamePos));
        }
    }

    private static bool IsPlayerBuildWinner(ReplayBuildDetails details, int gamePos)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            if (matchup.P1.GamePos == gamePos)
            {
                return matchup.P1Won;
            }

            if (matchup.P2.GamePos == gamePos)
            {
                return matchup.P2Won;
            }
        }

        return false;
    }

    private static void InspectBuildNone(
        string replayName,
        DateTime replayTime,
        ReplayDto replayDto,
        ReplayBuildDetails details,
        ScanResult result)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            InspectBuildNone(replayName, replayTime, replayDto, matchup.P1, result);
            InspectBuildNone(replayName, replayTime, replayDto, matchup.P2, result);
        }
    }

    private static void InspectBuildNone(
        string replayName,
        DateTime replayTime,
        ReplayDto replayDto,
        PlayerBuildInfo buildInfo,
        ScanResult result)
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
            $"{replayName} | {replayTime:yyyy-MM-dd HH:mm:ss} | pos {buildInfo.GamePos} | {buildInfo.Commander} | {player?.Name ?? "(unknown)"} | {units}");
    }

    private static void InspectTerranMech(
        string replayName,
        DateTime replayTime,
        ReplayDto replayDto,
        ReplayBuildDetails details,
        ScanResult result)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            InspectTerranMech(replayName, replayTime, replayDto, matchup.P1, matchup.P2, matchup.P1Won, result);
            InspectTerranMech(replayName, replayTime, replayDto, matchup.P2, matchup.P1, matchup.P2Won, result);
        }
    }

    private static void InspectTerranMech(
        string replayName,
        DateTime replayTime,
        ReplayDto replayDto,
        PlayerBuildInfo buildInfo,
        PlayerBuildInfo opponentBuildInfo,
        bool won,
        ScanResult result)
    {
        if (buildInfo.Commander != Commander.Terran
            || !string.Equals(buildInfo.BuildName, nameof(TerranBuild.Mech), StringComparison.Ordinal))
        {
            return;
        }

        var player = replayDto.Players.FirstOrDefault(player => player.GamePos == buildInfo.GamePos);
        var spawn = player?.Spawns.FirstOrDefault(spawn => spawn.Breakpoint == Breakpoint.Min5);
        var signature = CreateTerranMechSignature(spawn);
        var units = FormatUnits(spawn, Commander.Terran);
        var resultText = won ? "W" : "L";

        result.IncrementTerranMechSignature(signature, won);
        result.TerranMechDetails.Add(
            $"{replayName} | {replayTime:yyyy-MM-dd HH:mm:ss} | {resultText} | pos {buildInfo.GamePos} | {player?.Name ?? "(unknown)"} | vs {opponentBuildInfo.Commander}.{opponentBuildInfo.BuildName} | {signature} | {units}");
    }

    private static void PrintSummary(
        string source,
        int discovered,
        int selected,
        string batchInfo,
        ScanResult result)
    {
        Console.WriteLine("Direct Strike TE detail build scan");
        Console.WriteLine(source);
        Console.WriteLine($"Discovered: {discovered}");
        Console.WriteLine($"Attempted: {result.Attempted} of {selected}");
        Console.WriteLine($"Detected: {result.Detected}");
        Console.WriteLine($"Skipped/not detected: {result.Skipped}");
        Console.WriteLine($"Failed: {result.Failed}");
        Console.WriteLine(batchInfo);
        Console.WriteLine();

        PrintCounts("Player builds", result.PlayerBuildCounts, includeTopOpponents: true);
        PrintCounts("Team builds", result.TeamBuildCounts, includeTopOpponents: false);

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

        if (!result.TerranMechSignatureCounts.IsEmpty)
        {
            PrintCounts("Terran.Mech signatures", result.TerranMechSignatureCounts, includeTopOpponents: false);
        }

        if (!result.TerranMechDetails.IsEmpty)
        {
            Console.WriteLine("Terran.Mech details");
            foreach (var detail in result.TerranMechDetails.OrderBy(detail => detail, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  {detail}");
            }
        }
    }

    private static string FormatUnits(SpawnDto? spawn, Commander commander)
    {
        if (spawn is null || spawn.Units.Count == 0)
        {
            return "(no Min5 units)";
        }

        return string.Join(", ", spawn.Units
            .Where(unit => unit.Count > 0)
            .Select(unit => new
            {
                Name = UnitMap.GetNormalizedUnitName(unit.Name, commander),
                unit.Count,
            })
            .OrderBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase)
            .Select(unit => $"{unit.Name}:{unit.Count}"));
    }

    private static string CreateTerranMechSignature(SpawnDto? spawn)
    {
        if (spawn is null || spawn.Units.Count == 0)
        {
            return "(no mech signature)";
        }

        var signature = string.Join("+", spawn.Units
            .Where(unit => unit.Count > 0)
            .Select(unit => new
            {
                Name = UnitMap.GetNormalizedUnitName(unit.Name, Commander.Terran),
                unit.Count,
            })
            .Where(unit => IsTerranMechUnit(unit.Name))
            .OrderBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase)
            .Select(unit => $"{unit.Name}:{unit.Count}"));

        return string.IsNullOrEmpty(signature) ? "(no mech signature)" : signature;
    }

    private static bool IsTerranMechUnit(string normalizedName)
    {
        return normalizedName is "Raven"
            or "Viking"
            or "Thor"
            or "Widow Mine"
            or "Siege Tank"
            or "Hellion"
            or "Hellbat"
            or "Banshee"
            or "Cyclone";
    }

    private static DbContextOptions<DsstatsContext> CreateDbOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<DsstatsContext>();
        var serverVersion = new MySqlServerVersion(new Version(9, 7, 0));

        builder.UseMySql(connectionString, serverVersion, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        });

        return builder.Options;
    }

    private static IQueryable<Replay> GetReplayQuery(DsstatsContext context)
    {
        return context.Replays
            .AsNoTracking()
            .Where(replay => replay.GameMode == GameMode.Standard
                && replay.PlayerCount == 6
                && replay.WinnerTeam != 0
                && replay.TE
                && replay.Title.StartsWith("Direct Strike TE"));
    }

    private static Task<List<DatabaseReplayDto>> FetchReplayBatch(
        DsstatsContext context,
        int? beforeReplayId,
        int take)
    {
        var query = GetReplayQuery(context);

        if (beforeReplayId.HasValue)
        {
            query = query.Where(replay => replay.ReplayId < beforeReplayId.Value);
        }

        return query
            .OrderByDescending(replay => replay.ReplayId)
            .Take(take)
            .Select(replay => new DatabaseReplayDto(
                replay.ReplayId,
                replay.FileName ?? string.Empty,
                new ReplayDto
                {
                    Title = replay.Title,
                    FileName = replay.FileName ?? string.Empty,
                    GameMode = replay.GameMode,
                    Gametime = replay.Gametime,
                    WinnerTeam = replay.WinnerTeam,
                    Players = replay.Players
                        .OrderBy(player => player.GamePos)
                        .Select(player => new ReplayPlayerDto
                        {
                            Name = player.Name,
                            Race = player.Race,
                            GamePos = player.GamePos,
                            TeamId = player.TeamId,
                            Result = player.Result,
                            Duration = player.Duration,
                            Spawns = player.Spawns
                                .Where(spawn => spawn.Breakpoint == Breakpoint.Min5)
                                .Select(spawn => new SpawnDto
                                {
                                    Breakpoint = spawn.Breakpoint,
                                    GasCount = spawn.GasCount,
                                    Income = spawn.Income,
                                    ArmyValue = spawn.ArmyValue,
                                    KilledValue = spawn.KilledValue,
                                    UpgradeSpent = spawn.UpgradeSpent,
                                    Units = spawn.Units
                                        .OrderBy(unit => unit.Unit!.Name)
                                        .Select(unit => new UnitDto
                                        {
                                            Count = unit.Count,
                                            Name = unit.Unit!.Name,
                                        })
                                        .ToList(),
                                })
                                .ToList(),
                        })
                        .ToList(),
                }))
            .AsSplitQuery()
            .ToListAsync();
    }

    private static string GetDefaultConnectionString()
    {
        if (!File.Exists(DefaultDevelopmentSettingsPath))
        {
            return string.Empty;
        }

        using var stream = File.OpenRead(DefaultDevelopmentSettingsPath);
        using var json = JsonDocument.Parse(stream);

        if (json.RootElement.TryGetProperty("dsstats", out var dsstats)
            && dsstats.TryGetProperty("ConnectionString", out var connectionString))
        {
            return connectionString.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetInnermostMessage(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception.Message;
    }

    private static void PrintCounts(
        string title,
        ConcurrentDictionary<string, BuildStats> counts,
        bool includeTopOpponents)
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
            var snapshot = pair.Value.GetSnapshot();
            Console.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {pair.Key}: {snapshot.Total} (W {snapshot.Wins} / L {snapshot.Losses}, WR {snapshot.Winrate:0.0}%)"));

            if (includeTopOpponents)
            {
                PrintTopOpponentBuilds(pair.Value);
            }
        }

        Console.WriteLine();
    }

    private static void PrintTopOpponentBuilds(BuildStats stats)
    {
        var opponentBuilds = stats.OpponentBuilds;
        if (opponentBuilds is null || opponentBuilds.IsEmpty)
        {
            return;
        }

        foreach (var pair in opponentBuilds
            .Select(pair => new
            {
                pair.Key,
                Snapshot = pair.Value.GetSnapshot(),
            })
            .OrderByDescending(pair => pair.Snapshot.Total)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3))
        {
            Console.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"    vs {pair.Key}: {pair.Snapshot.Total} (W {pair.Snapshot.Wins} / L {pair.Snapshot.Losses}, WR {pair.Snapshot.Winrate:0.0}%)"));
        }
    }

    private sealed class ScanResult
    {
        public int Attempted;
        public int Detected;
        public int Skipped;
        public int Failed;

        public ConcurrentDictionary<string, BuildStats> PlayerBuildCounts { get; } = new(StringComparer.Ordinal);
        public ConcurrentDictionary<string, BuildStats> TeamBuildCounts { get; } = new(StringComparer.Ordinal);
        public ConcurrentDictionary<string, BuildStats> TerranMechSignatureCounts { get; } = new(StringComparer.Ordinal);
        public ConcurrentBag<string> Failures { get; } = [];
        public ConcurrentBag<string> BuildNoneDetails { get; } = [];
        public ConcurrentBag<string> TerranMechDetails { get; } = [];

        public void IncrementPlayerBuild(PlayerBuildInfo buildInfo, PlayerBuildInfo opponentBuildInfo, bool won)
        {
            var opponentKey = CreateBuildKey(opponentBuildInfo);
            var stats = PlayerBuildCounts
                .GetOrAdd(CreateBuildKey(buildInfo), static _ => new BuildStats(trackOpponents: true));

            stats.Increment(won);
            stats.IncrementOpponent(opponentKey, won);
        }

        public void IncrementTeamBuild(string teamBuildName, bool won)
        {
            TeamBuildCounts
                .GetOrAdd(teamBuildName, static _ => new BuildStats(trackOpponents: false))
                .Increment(won);
        }

        public void IncrementTerranMechSignature(string signature, bool won)
        {
            TerranMechSignatureCounts
                .GetOrAdd(signature, static _ => new BuildStats(trackOpponents: false))
                .Increment(won);
        }

        public void AddFailure(string fileName, string message)
        {
            Interlocked.Increment(ref Failed);
            Failures.Add($"{fileName}: {message}");
        }

        private static string CreateBuildKey(PlayerBuildInfo buildInfo)
        {
            return $"{buildInfo.Commander}.{buildInfo.BuildName}";
        }
    }

    private sealed class BuildStats
    {
        private int wins;
        private int losses;
        private readonly ConcurrentDictionary<string, BuildStats>? opponentBuilds;

        public BuildStats(bool trackOpponents)
        {
            if (trackOpponents)
            {
                opponentBuilds = new ConcurrentDictionary<string, BuildStats>(StringComparer.Ordinal);
            }
        }

        public ConcurrentDictionary<string, BuildStats>? OpponentBuilds => opponentBuilds;

        public void Increment(bool won)
        {
            if (won)
            {
                Interlocked.Increment(ref wins);
            }
            else
            {
                Interlocked.Increment(ref losses);
            }
        }

        public void IncrementOpponent(string opponentBuild, bool won)
        {
            opponentBuilds?
                .GetOrAdd(opponentBuild, static _ => new BuildStats(trackOpponents: false))
                .Increment(won);
        }

        public BuildStatsSnapshot GetSnapshot()
        {
            var currentWins = Volatile.Read(ref wins);
            var currentLosses = Volatile.Read(ref losses);
            var total = currentWins + currentLosses;
            var winrate = total == 0 ? 0 : currentWins * 100.0 / total;

            return new BuildStatsSnapshot(currentWins, currentLosses, total, winrate);
        }
    }

    private readonly record struct BuildStatsSnapshot(int Wins, int Losses, int Total, double Winrate);

    private sealed record DatabaseReplayDto(int ReplayId, string FileName, ReplayDto Replay)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(FileName) ? $"ReplayId {ReplayId}" : FileName;
    }
}
