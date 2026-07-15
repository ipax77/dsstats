using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using dsstats.parser;
using Sc2DirectStrike.Parser;

namespace dsstats.validate.cli;

internal static class Program
{
    private const string DefaultValidationRoot = @"C:\data\ds\validate";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Any(arg => arg is "-h" or "--help"))
        {
            PrintUsage();
            return 0;
        }

        string validationRoot = Path.GetFullPath(args.FirstOrDefault() ?? DefaultValidationRoot);
        string outputPath = Path.GetFullPath(args.Skip(1).FirstOrDefault()
            ?? Path.Combine(validationRoot, "validation-results.json"));

        if (!Directory.Exists(validationRoot))
        {
            Console.Error.WriteLine($"Validation root not found: {validationRoot}");
            return 1;
        }

        List<ValidationCaseFiles> cases;
        try
        {
            cases = DiscoverCases(validationRoot);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (cases.Count == 0)
        {
            Console.Error.WriteLine($"No validation cases found below: {validationRoot}");
            return 1;
        }

        Console.WriteLine("Direct Strike replay decoding validation");
        Console.WriteLine($"Root: {validationRoot}");
        Console.WriteLine($"Cases: {cases.Count}");
        Console.WriteLine();

        var results = new List<CaseResult>(cases.Count);
        int failures = 0;
        foreach (ValidationCaseFiles validationCase in cases)
        {
            try
            {
                CaseResult result = await ValidateCase(validationCase);
                results.Add(result);
                PrintResult(result);
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"{validationCase.Name}: {ex.GetType().Name}: {ex.Message}");
                results.Add(CaseResult.Failed(validationCase, ex));
            }
        }

        ValidationRunResult runResult = CreateRunResult(validationRoot, results);
        await using (FileStream output = File.Create(outputPath))
        {
            await JsonSerializer.SerializeAsync(output, runResult, JsonOptions);
        }

        Console.WriteLine();
        Console.WriteLine($"Exact comparisons: {runResult.Summary.Exact:N0}/{runResult.Summary.Compared:N0}");
        Console.WriteLine($"Differences: {runResult.Summary.Different:N0}");
        Console.WriteLine($"Output: {outputPath}");

        return failures == 0 ? 0 : 2;
    }

    private static List<ValidationCaseFiles> DiscoverCases(string validationRoot)
    {
        var cases = new List<ValidationCaseFiles>();
        foreach (string directory in Directory.EnumerateDirectories(validationRoot)
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            string[] replayPaths = Directory.GetFiles(directory, "*.SC2Replay", SearchOption.TopDirectoryOnly);
            string[] expectedPaths = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            if (replayPaths.Length == 0 && expectedPaths.Length == 0)
            {
                continue;
            }

            if (replayPaths.Length != 1 || expectedPaths.Length != 1)
            {
                throw new InvalidDataException(
                    $"Case '{directory}' must contain exactly one .SC2Replay and one screenshot JSON; " +
                    $"found {replayPaths.Length} replay(s) and {expectedPaths.Length} JSON file(s).");
            }

            cases.Add(new(
                Path.GetFileName(directory),
                Path.GetFullPath(directory),
                replayPaths[0],
                expectedPaths[0]));
        }

        return cases;
    }

    private static async Task<CaseResult> ValidateCase(ValidationCaseFiles validationCase)
    {
        ScreenshotData expected;
        await using (FileStream expectedStream = File.OpenRead(validationCase.ExpectedPath))
        {
            expected = await JsonSerializer.DeserializeAsync<ScreenshotData>(expectedStream, JsonOptions)
                ?? throw new InvalidDataException($"Empty screenshot JSON: {validationCase.ExpectedPath}");
        }

        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
        TimeSpan cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
        var stopwatch = Stopwatch.StartNew();
        var sc2Replay = await DsstatsParser.GetSc2Replay(validationCase.ReplayPath)
            ?? throw new InvalidDataException("Replay decoder returned null.");
        long decodeMilliseconds = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        DirectStrikeReplay replay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
        long parseMilliseconds = stopwatch.ElapsedMilliseconds;
        long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;
        long cpuMilliseconds = (long)(Process.GetCurrentProcess().TotalProcessorTime - cpuBefore).TotalMilliseconds;

        Dictionary<string, RawSpawnDiagnostics> rawSpawnDiagnostics = GetRawSpawnDiagnostics(sc2Replay, replay);
        List<DecodedPlayerData> decodedPlayers = replay.Players
            .Select(player => DetectPlayer(player, rawSpawnDiagnostics.GetValueOrDefault(player.Name)))
            .OrderBy(player => player.GamePos)
            .ToList();
        Dictionary<string, DecodedPlayerData> parsedPlayers = decodedPlayers
            .ToDictionary(player => player.Name, StringComparer.OrdinalIgnoreCase);
        var playerResults = new List<PlayerResult>(expected.Players.Count);

        foreach (ScreenshotPlayer expectedPlayer in expected.Players)
        {
            if (!parsedPlayers.TryGetValue(expectedPlayer.Name, out DecodedPlayerData? detected))
            {
                playerResults.Add(PlayerResult.Missing(expectedPlayer));
                continue;
            }

            playerResults.Add(PlayerResult.Create(expectedPlayer, detected));
        }

        return new(
            validationCase.Name,
            Path.GetFileName(validationCase.ReplayPath),
            Path.GetFileName(validationCase.ExpectedPath),
            expected.Screenshot,
            replay.GameTime,
            replay.Duration,
            replay.Players.Count,
            decodeMilliseconds,
            parseMilliseconds,
            cpuMilliseconds,
            allocatedBytes,
            playerResults,
            decodedPlayers,
            null);
    }

    private static DecodedPlayerData DetectPlayer(DirectStrikePlayer player, RawSpawnDiagnostics? rawDiagnostics)
    {
        DirectStrikePlayerStats? finalStats = player.Stats.LastOrDefault();
        int spawnedUnitCount = 0;
        foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
        {
            spawnedUnitCount += spawn.Units.Count;
        }

        return new(
            player.Name,
            player.GamePos,
            player.TeamId,
            player.Commander.ToString(),
            finalStats?.MineralsKilledArmy,
            finalStats?.MineralsLostArmy,
            spawnedUnitCount,
            player.Spawns.Count,
            finalStats?.Gameloop,
            player.BuildUnitNames,
            rawDiagnostics?.ControlPlayerId,
            rawDiagnostics?.CandidateCount,
            rawDiagnostics?.UntrackedUnitCounts);
    }

    private static Dictionary<string, RawSpawnDiagnostics> GetRawSpawnDiagnostics(
        s2protocol.NET.Sc2Replay sc2Replay,
        DirectStrikeReplay replay)
    {
        var finalStatsByPlayerId = new Dictionary<int, s2protocol.NET.Models.SPlayerStatsEvent>();
        foreach (s2protocol.NET.Models.SPlayerStatsEvent stats in sc2Replay.TrackerEvents?.SPlayerStatsEvents ?? [])
        {
            if (!finalStatsByPlayerId.TryGetValue(stats.PlayerId, out s2protocol.NET.Models.SPlayerStatsEvent? previous)
                || stats.Gameloop >= previous.Gameloop)
            {
                finalStatsByPlayerId[stats.PlayerId] = stats;
            }
        }

        var diagnostics = new Dictionary<string, RawSpawnDiagnostics>(StringComparer.OrdinalIgnoreCase);
        foreach (DirectStrikePlayer player in replay.Players)
        {
            DirectStrikePlayerStats? finalStats = player.Stats.LastOrDefault();
            if (finalStats is null)
            {
                continue;
            }

            int? controlPlayerId = null;
            foreach ((int playerId, s2protocol.NET.Models.SPlayerStatsEvent stats) in finalStatsByPlayerId)
            {
                if (stats.MineralsKilledArmy == finalStats.MineralsKilledArmy
                    && stats.MineralsLostArmy == finalStats.MineralsLostArmy
                    && stats.Gameloop == finalStats.Gameloop)
                {
                    controlPlayerId = playerId;
                    break;
                }
            }

            if (controlPlayerId is null)
            {
                continue;
            }

            var rawCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (s2protocol.NET.Models.SUnitBornEvent born in sc2Replay.TrackerEvents?.SUnitBornEvents ?? [])
            {
                if (born.Gameloop != 0
                    && born.ControlPlayerId == controlPlayerId
                    && IsInSpawnArea(born.X, born.Y, player.TeamId))
                {
                    AddCount(rawCounts, born.UnitTypeName);
                }
            }

            var trackedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                foreach (DirectStrikeSpawnUnit unit in spawn.Units)
                {
                    AddCount(trackedCounts, unit.Name);
                }
            }

            var untrackedCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach ((string unitName, int rawCount) in rawCounts)
            {
                int difference = rawCount - trackedCounts.GetValueOrDefault(unitName);
                if (difference > 0)
                {
                    untrackedCounts[unitName] = difference;
                }
            }

            diagnostics[player.Name] = new(controlPlayerId.Value, rawCounts.Values.Sum(), untrackedCounts);
        }

        return diagnostics;
    }

    private static void AddCount(Dictionary<string, int> counts, string value)
    {
        counts[value] = counts.GetValueOrDefault(value) + 1;
    }

    private static bool IsInSpawnArea(int x, int y, int teamId)
    {
        ReadOnlySpan<(int X, int Y)> points = teamId switch
        {
            1 => [(165, 174), (182, 157), (171, 146), (154, 163)],
            2 => [(84, 93), (101, 76), (90, 65), (73, 82)],
            _ => []
        };
        if (points.Length == 0)
        {
            return false;
        }

        bool inside = false;
        for (int current = 0, previous = points.Length - 1; current < points.Length; previous = current++)
        {
            (int currentX, int currentY) = points[current];
            (int previousX, int previousY) = points[previous];
            long cross = (long)(x - previousX) * (currentY - previousY)
                - (long)(y - previousY) * (currentX - previousX);
            if (cross == 0
                && x >= Math.Min(previousX, currentX) && x <= Math.Max(previousX, currentX)
                && y >= Math.Min(previousY, currentY) && y <= Math.Max(previousY, currentY))
            {
                return true;
            }

            if ((currentY > y) != (previousY > y)
                && x < (double)(previousX - currentX) * (y - currentY) / (previousY - currentY) + currentX)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static ValidationRunResult CreateRunResult(string validationRoot, List<CaseResult> cases)
    {
        List<MetricComparison> comparisons = cases
            .SelectMany(result => result.Players)
            .SelectMany(player => player.Comparisons)
            .ToList();

        int compared = comparisons.Count(comparison => comparison.Status is ComparisonStatus.Exact or ComparisonStatus.Different);
        int exact = comparisons.Count(comparison => comparison.Status == ComparisonStatus.Exact);
        int different = comparisons.Count(comparison => comparison.Status == ComparisonStatus.Different);
        return new(
            1,
            DateTimeOffset.UtcNow,
            validationRoot,
            cases,
            new(compared, exact, different, comparisons.Count - compared));
    }

    private static void PrintResult(CaseResult result)
    {
        Console.WriteLine($"Case {result.CaseName}: {result.ReplayFile}");
        if (result.Error is not null)
        {
            Console.WriteLine($"  ERROR {result.Error}");
            return;
        }

        Console.WriteLine(
            $"  decode {result.DecodeMilliseconds:N0} ms, parse {result.ParseMilliseconds:N0} ms, " +
            $"CPU {result.CpuMilliseconds:N0} ms, allocated {result.AllocatedBytes / 1_048_576d:N1} MiB");
        foreach (PlayerResult player in result.Players)
        {
            if (player.Error is not null)
            {
                Console.WriteLine($"  {player.Name}: {player.Error}");
                continue;
            }

            string comparisons = string.Join(", ", player.Comparisons.Select(comparison =>
                $"{comparison.Metric} {FormatValue(comparison.Expected)} -> {FormatValue(comparison.Detected)} ({comparison.Status})"));
            Console.WriteLine($"  {player.Name}: {comparisons}");
        }

        HashSet<string> expectedNames = result.Players
            .Select(player => player.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (DecodedPlayerData decoded in result.DecodedPlayers.Where(player => !expectedNames.Contains(player.Name)))
        {
            Console.WriteLine(
                $"  unmatched decoded P{decoded.GamePos} {decoded.Name}: " +
                $"killed {FormatValue(decoded.MineralValueKilled)}, spawned {decoded.SpawnedUnitCount:N0}");
        }

        foreach (DecodedPlayerData decoded in result.DecodedPlayers.Where(player => player.RawSpawnCandidateCount != player.SpawnedUnitCount))
        {
            string untracked = decoded.UntrackedSpawnUnitCounts is { Count: > 0 }
                ? string.Join(", ", decoded.UntrackedSpawnUnitCounts.Select(pair => $"{pair.Key} x{pair.Value}"))
                : "none identified";
            Console.WriteLine(
                $"  raw spawn candidates {decoded.Name}: {FormatValue(decoded.RawSpawnCandidateCount)} " +
                $"(tracked {decoded.SpawnedUnitCount:N0}); untracked {untracked}");
        }
    }

    private static string FormatValue(int? value) => value?.ToString("N0") ?? "n/a";

    private static void PrintUsage()
    {
        Console.WriteLine("""
        Usage:
          dsstats.validate.cli [validation-root] [output-json]

        Defaults:
          validation-root  C:\data\ds\validate
          output-json      <validation-root>\validation-results.json

        Each immediate child directory must contain exactly one .SC2Replay file and
        one screenshot JSON file. Mineral values come from the final tracker stats;
        spawned-unit count is the sum of units in all parsed spawn groups.
        """);
    }
}

internal sealed record ValidationCaseFiles(
    string Name,
    string Directory,
    string ReplayPath,
    string ExpectedPath);

internal sealed record ScreenshotData(
    int SchemaVersion,
    string Screenshot,
    List<string> Columns,
    List<ScreenshotPlayer> Players,
    List<string>? Notes);

internal sealed record ScreenshotPlayer(
    string Name,
    int? MineralValueKilled,
    int? MineralValueLost,
    int? SpawnedUnitCount,
    int? SpawnedUnitValue,
    int? ExpiredUnitCount,
    int? ExpiredUnitValue);

internal sealed record DecodedPlayerData(
    string Name,
    int GamePos,
    int TeamId,
    string Commander,
    int? MineralValueKilled,
    int? MineralValueLost,
    int SpawnedUnitCount,
    int SpawnGroupCount,
    int? FinalStatsGameloop,
    IReadOnlyList<string> BuildUnitNames,
    int? ControlPlayerId,
    int? RawSpawnCandidateCount,
    IReadOnlyDictionary<string, int>? UntrackedSpawnUnitCounts);

internal sealed record RawSpawnDiagnostics(
    int ControlPlayerId,
    int CandidateCount,
    IReadOnlyDictionary<string, int> UntrackedUnitCounts);

internal sealed record PlayerResult(
    string Name,
    ScreenshotPlayer Expected,
    DecodedPlayerData? Detected,
    List<MetricComparison> Comparisons,
    string? Error)
{
    public static PlayerResult Missing(ScreenshotPlayer expected) =>
        new(
            expected.Name,
            expected,
            null,
            [
                MetricComparison.Create("mineralValueKilled", expected.MineralValueKilled, null),
                MetricComparison.Create("spawnedUnitCount", expected.SpawnedUnitCount, null)
            ],
            "Player was not found in the decoded replay.");

    public static PlayerResult Create(ScreenshotPlayer expected, DecodedPlayerData detected) =>
        new(
            expected.Name,
            expected,
            detected,
            [
                MetricComparison.Create("mineralValueKilled", expected.MineralValueKilled, detected.MineralValueKilled),
                MetricComparison.Create("spawnedUnitCount", expected.SpawnedUnitCount, detected.SpawnedUnitCount)
            ],
            null);
}

internal sealed record MetricComparison(
    string Metric,
    int? Expected,
    int? Detected,
    int? Difference,
    ComparisonStatus Status)
{
    public static MetricComparison Create(string metric, int? expected, int? detected)
    {
        ComparisonStatus status = (expected, detected) switch
        {
            (null, _) or (_, null) => ComparisonStatus.NotAvailable,
            _ when expected == detected => ComparisonStatus.Exact,
            _ => ComparisonStatus.Different
        };
        return new(metric, expected, detected, expected is not null && detected is not null ? detected - expected : null, status);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<ComparisonStatus>))]
internal enum ComparisonStatus
{
    NotAvailable,
    Exact,
    Different
}

internal sealed record CaseResult(
    string CaseName,
    string ReplayFile,
    string ExpectedFile,
    string ScreenshotFile,
    DateTime GameTime,
    TimeSpan Duration,
    int ParsedPlayerCount,
    long DecodeMilliseconds,
    long ParseMilliseconds,
    long CpuMilliseconds,
    long AllocatedBytes,
    List<PlayerResult> Players,
    List<DecodedPlayerData> DecodedPlayers,
    string? Error)
{
    public static CaseResult Failed(ValidationCaseFiles validationCase, Exception ex) =>
        new(
            validationCase.Name,
            Path.GetFileName(validationCase.ReplayPath),
            Path.GetFileName(validationCase.ExpectedPath),
            string.Empty,
            default,
            default,
            0,
            0,
            0,
            0,
            0,
            [],
            [],
            $"{ex.GetType().Name}: {ex.Message}");
}

internal sealed record ValidationRunResult(
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    string ValidationRoot,
    List<CaseResult> Cases,
    ValidationSummary Summary);

internal sealed record ValidationSummary(
    int Compared,
    int Exact,
    int Different,
    int NotAvailable);
