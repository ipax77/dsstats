using dsstats.shared;
using dsstats.shared.Upload;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace dsstats.inspect;

internal static class Program
{
    private const string DefaultReplayBlobRoot = @"D:\backup\sc2dsstats\replayblobs_2026-06-28_07-09-58.tar\replayblobs";
    private const string DefaultExtractRoot = @"C:\tmp\dsstats.inspect";
    private static readonly DateTime DefaultImportStartUtc = new(2026, 06, 27, 07, 19, 25, DateTimeKind.Utc);
    private static readonly DateTime DefaultImportEndUtc = new(2026, 06, 27, 07, 22, 24, DateTimeKind.Utc);
    private static readonly TimeSpan DefaultPadding = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly LogExpectations SuspectImportExpectations = new(
        PackageCount: 61,
        Imported: 2945,
        Duplicates: 12,
        Replaced: 1);

    private static async Task<int> Main(string[] args)
    {
        var options = InspectOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        var needsReplayBlobRoot = options.NeedsReplayBlobRoot;
        if (needsReplayBlobRoot && !Directory.Exists(options.ReplayBlobRoot))
        {
            Console.Error.WriteLine($"Replayblob root not found: {options.ReplayBlobRoot}");
            return 1;
        }

        var spawnPlaybackRoot = needsReplayBlobRoot
            ? Path.Combine(options.ReplayBlobRoot, "spawn-playbacks")
            : string.Empty;
        if (needsReplayBlobRoot && !Directory.Exists(spawnPlaybackRoot))
        {
            Console.Error.WriteLine($"Spawn playback package directory not found: {spawnPlaybackRoot}");
            return 1;
        }

        if (options.IsScoreMode)
        {
            return await ScoreReplays(spawnPlaybackRoot, options);
        }

        if (!string.IsNullOrWhiteSpace(options.ExtractPackageId))
        {
            return await ExtractPackage(spawnPlaybackRoot, options.ExtractPackageId, options.OutputDirectory);
        }

        var localStart = ConvertUtcToFileTime(options.ImportStartUtc, options.OffsetOverride);
        var localEnd = ConvertUtcToFileTime(options.ImportEndUtc, options.OffsetOverride);

        PrintHeader(options, localStart, localEnd);

        var summary = new ImportSummary();

        Console.WriteLine("Packages");
        Console.WriteLine("LastWriteTime          PackageId                         Replays UniqueHashes Manifest RequestNames Version");
        Console.WriteLine(new string('-', 118));

        foreach (var packageDirectory in EnumerateCandidatePackages(spawnPlaybackRoot, localStart, localEnd))
        {
            var package = await InspectPackage(packageDirectory);
            summary.Add(package);
            PrintPackage(package);
        }

        PrintSummary(summary);
        return summary.UnreadablePackages == 0 ? 0 : 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
        Usage:
          dsstats.inspect [replayblobRoot] [importStartUtc] [importEndUtc] [offset]
          dsstats.inspect --extract-package <packageId> [--output <directory>] [replayblobRoot]
          dsstats.inspect --score-package <packageId> [--score-package <packageId>] [replayblobRoot]
          dsstats.inspect --score-path <directory> [--score-path <directory>]
          dsstats.inspect --score-window [replayblobRoot] [importStartUtc] [importEndUtc] [offset]

        Defaults:
          replayblobRoot  D:\backup\sc2dsstats\replayblobs_2026-06-28_07-09-58.tar\replayblobs
          importStartUtc  2026-06-27T07:18:25Z
          importEndUtc    2026-06-27T07:23:24Z
          offset          TimeZoneInfo.Local offset at importStartUtc

        Examples:
          dsstats.inspect
          dsstats.inspect D:\backup\...\replayblobs 2026-06-27T07:18:25Z 2026-06-27T07:23:24Z
          dsstats.inspect D:\backup\...\replayblobs 2026-06-27T07:18:25Z 2026-06-27T07:23:24Z +02:00
          dsstats.inspect --extract-package a7eb78402e7e4a728125412b3e41a221
          dsstats.inspect --extract-package a7eb78402e7e4a728125412b3e41a221 --output C:\tmp\replays
          dsstats.inspect --score-package a7eb78402e7e4a728125412b3e41a221
          dsstats.inspect --score-path C:\tmp\dsstats.inspect\a7eb78402e7e4a728125412b3e41a221
          dsstats.inspect --score-window
        """);
    }

    private static async Task<int> ExtractPackage(
        string spawnPlaybackRoot,
        string packageId,
        string? outputDirectory)
    {
        if (!IsSafePackageId(packageId))
        {
            Console.Error.WriteLine($"Invalid package id: {packageId}");
            return 1;
        }

        var packageDirectory = Path.Combine(spawnPlaybackRoot, packageId);
        if (!Directory.Exists(packageDirectory))
        {
            Console.Error.WriteLine($"Package id not found: {packageId}");
            Console.Error.WriteLine($"Expected directory: {packageDirectory}");
            return 1;
        }

        var requestPath = Path.Combine(packageDirectory, "request.json.gz");
        if (!File.Exists(requestPath))
        {
            Console.Error.WriteLine($"Package request file not found: {requestPath}");
            return 1;
        }

        var resolvedOutputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(DefaultExtractRoot, packageId)
            : outputDirectory;

        Directory.CreateDirectory(resolvedOutputDirectory);

        UploadRequestDto? uploadRequest;
        await using (var requestStream = File.OpenRead(requestPath))
        await using (var gzip = new GZipStream(requestStream, CompressionMode.Decompress))
        {
            uploadRequest = await JsonSerializer.DeserializeAsync<UploadRequestDto>(gzip, UploadJsonOptions);
        }

        if (uploadRequest is null)
        {
            Console.Error.WriteLine($"Package request file could not be deserialized: {requestPath}");
            return 1;
        }

        var exportOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        var replayHashCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var candidateHashCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var manifestEntries = await LoadManifestEntries(packageDirectory);
        var replayIndex = 0;

        foreach (var replay in uploadRequest.Replays)
        {
            replayIndex++;
            var replayHash = replay.ComputeHash();
            var candidateHash = replay.ComputeCandidateHash();
            AddCount(replayHashCounts, replayHash);
            AddCount(candidateHashCounts, candidateHash);

            var replayFileName = $"{replayIndex:0000}_{SafeFilePart(replayHash)}.json";
            var replayPath = Path.Combine(resolvedOutputDirectory, replayFileName);
            await using var replayFile = File.Create(replayPath);
            await JsonSerializer.SerializeAsync(replayFile, replay, exportOptions);
        }

        var packageSummary = new ExtractedPackageSummary(
            PackageId: packageId,
            SourcePackageDirectory: packageDirectory,
            SourceRequestPath: requestPath,
            OutputDirectory: Path.GetFullPath(resolvedOutputDirectory),
            AppVersion: uploadRequest.AppVersion,
            RequestNames: uploadRequest.RequestNames,
            ReplayCount: uploadRequest.Replays.Count,
            UniqueReplayHashes: replayHashCounts.Count,
            DuplicateReplayHashRows: uploadRequest.Replays.Count - replayHashCounts.Count,
            UniqueCandidateHashes: candidateHashCounts.Count,
            DuplicateCandidateHashRows: uploadRequest.Replays.Count - candidateHashCounts.Count,
            ManifestEntries: manifestEntries.Count,
            UniqueManifestHashes: manifestEntries
                .Select(entry => entry.ReplayHash)
                .Where(hash => !string.IsNullOrWhiteSpace(hash))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            TopReplayHashDuplicates: GetDuplicateSummaries(replayHashCounts),
            TopCandidateHashDuplicates: GetDuplicateSummaries(candidateHashCounts));

        var summaryPath = Path.Combine(resolvedOutputDirectory, "package-summary.json");
        await using (var summaryFile = File.Create(summaryPath))
        {
            await JsonSerializer.SerializeAsync(summaryFile, packageSummary, exportOptions);
        }

        Console.WriteLine($"Extracted package: {packageId}");
        Console.WriteLine($"ReplayDtos: {uploadRequest.Replays.Count}");
        Console.WriteLine($"Output directory: {Path.GetFullPath(resolvedOutputDirectory)}");
        Console.WriteLine($"Summary: {summaryPath}");
        Console.WriteLine($"Unique replay hashes: {replayHashCounts.Count}");
        Console.WriteLine($"Unique candidate hashes: {candidateHashCounts.Count}");
        Console.WriteLine($"Manifest entries: {manifestEntries.Count}");

        return 0;
    }

    private static async Task<List<SpawnPlaybackUploadManifestEntryDto>> LoadManifestEntries(string packageDirectory)
    {
        var manifestPath = Path.Combine(packageDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        await using var manifestStream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<List<SpawnPlaybackUploadManifestEntryDto>>(
            manifestStream,
            UploadJsonOptions) ?? [];
    }

    private static bool IsSafePackageId(string packageId)
    {
        return packageId.Length > 0
            && packageId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && packageId.All(char.IsAsciiHexDigit);
    }

    private static string SafeFilePart(string value)
    {
        Span<char> chars = stackalloc char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            chars[i] = char.IsAsciiLetterOrDigit(value[i]) ? value[i] : '_';
        }

        return new string(chars);
    }

    private static async Task<int> ScoreReplays(string spawnPlaybackRoot, InspectOptions options)
    {
        var scoredReplays = new List<ScoredReplay>();

        foreach (var packageId in options.ScorePackageIds)
        {
            if (!IsSafePackageId(packageId))
            {
                Console.Error.WriteLine($"Invalid package id: {packageId}");
                return 1;
            }

            var packageDirectory = Path.Combine(spawnPlaybackRoot, packageId);
            if (!Directory.Exists(packageDirectory))
            {
                Console.Error.WriteLine($"Package id not found: {packageId}");
                Console.Error.WriteLine($"Expected directory: {packageDirectory}");
                return 1;
            }

            var uploadRequest = await LoadUploadRequest(packageDirectory);
            if (uploadRequest is null)
            {
                Console.Error.WriteLine($"Package request file could not be deserialized: {packageDirectory}");
                return 1;
            }

            AddScoredReplays(scoredReplays, uploadRequest.Replays, packageId, packageDirectory, "request.json.gz");
        }

        if (options.ScoreWindow)
        {
            var localStart = ConvertUtcToFileTime(options.ImportStartUtc, options.OffsetOverride);
            var localEnd = ConvertUtcToFileTime(options.ImportEndUtc, options.OffsetOverride);
            foreach (var packageDirectory in EnumerateCandidatePackages(spawnPlaybackRoot, localStart, localEnd))
            {
                var uploadRequest = await LoadUploadRequest(packageDirectory.FullName);
                if (uploadRequest is null)
                {
                    Console.Error.WriteLine($"Skipping unreadable package request: {packageDirectory.FullName}");
                    continue;
                }

                AddScoredReplays(scoredReplays, uploadRequest.Replays, packageDirectory.Name, packageDirectory.FullName, "request.json.gz");
            }
        }

        foreach (var scorePath in options.ScorePaths)
        {
            if (!Directory.Exists(scorePath))
            {
                Console.Error.WriteLine($"Score path not found: {scorePath}");
                return 1;
            }

            await AddScoredReplayFiles(scoredReplays, scorePath);
        }

        if (scoredReplays.Count == 0)
        {
            Console.WriteLine("No ReplayDtos found to score.");
            return 0;
        }

        var clusters = BuildSuspiciousClusters(scoredReplays);
        PrintScoreReport(scoredReplays, clusters);
        return 0;
    }

    private static async Task<UploadRequestDto?> LoadUploadRequest(string packageDirectory)
    {
        var requestPath = Path.Combine(packageDirectory, "request.json.gz");
        if (!File.Exists(requestPath))
        {
            return null;
        }

        await using var requestStream = File.OpenRead(requestPath);
        await using var gzip = new GZipStream(requestStream, CompressionMode.Decompress);
        return await JsonSerializer.DeserializeAsync<UploadRequestDto>(gzip, UploadJsonOptions);
    }

    private static void AddScoredReplays(
        List<ScoredReplay> target,
        IReadOnlyList<ReplayDto> replays,
        string packageId,
        string sourceRoot,
        string sourceName)
    {
        for (var i = 0; i < replays.Count; i++)
        {
            target.Add(CreateScoredReplay(
                replays[i],
                packageId,
                sourceRoot,
                $"{sourceName}#{i + 1:0000}"));
        }
    }

    private static async Task AddScoredReplayFiles(List<ScoredReplay> target, string directory)
    {
        var packageId = await TryReadExtractedPackageId(directory) ?? new DirectoryInfo(directory).Name;
        foreach (var file in new DirectoryInfo(directory)
            .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .Where(file => !string.Equals(file.Name, "package-summary.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await using var stream = File.OpenRead(file.FullName);
                var replay = await JsonSerializer.DeserializeAsync<ReplayDto>(stream, UploadJsonOptions);
                if (replay is not null)
                {
                    target.Add(CreateScoredReplay(replay, packageId, directory, file.Name));
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Skipping unreadable replay json {file.FullName}: {ex.Message}");
            }
        }
    }

    private static async Task<string?> TryReadExtractedPackageId(string directory)
    {
        var summaryPath = Path.Combine(directory, "package-summary.json");
        if (!File.Exists(summaryPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(summaryPath);
            using var document = await JsonDocument.ParseAsync(stream);
            return document.RootElement.TryGetProperty("packageId", out var packageId)
                ? packageId.GetString()
                : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not read package summary {summaryPath}: {ex.Message}");
            return null;
        }
    }

    private static ScoredReplay CreateScoredReplay(
        ReplayDto replay,
        string packageId,
        string sourceRoot,
        string sourceName)
    {
        var replayHash = replay.ComputeHash();
        var candidateHash = replay.ComputeCandidateHash();
        var commanderLayout = GetCommanderLayout(replay);
        var messageVector = GetMessageVector(replay);

        return new(
            Replay: replay,
            PackageId: packageId,
            SourceRoot: sourceRoot,
            SourceName: sourceName,
            ReplayHash: replayHash,
            CandidateHash: candidateHash,
            ContentFingerprint: ComputeContentFingerprint(replay, commanderLayout, messageVector),
            TimelineFingerprint: ComputeTimelineFingerprint(replay),
            CommanderLayout: commanderLayout,
            MessageVector: messageVector,
            Players: GetPlayersDisplay(replay));
    }

    private static List<SuspiciousCluster> BuildSuspiciousClusters(List<ScoredReplay> replays)
    {
        var clusters = new List<SuspiciousCluster>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(replay => replay.ReplayHash, StringComparer.OrdinalIgnoreCase),
            group => group.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1,
            group => CreateCluster(100, ["SameReplayHash"], group));

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(replay => replay.ContentFingerprint, StringComparer.Ordinal),
            group => group.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1,
            group =>
            {
                var reasons = new List<string> { "SameContent" };
                if (HasMeaningfullyDifferentGametime(group))
                {
                    reasons.Add("DifferentGametime");
                }
                return CreateCluster(HasMeaningfullyDifferentGametime(group) ? 90 : 85, reasons, group);
            });

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(replay => replay.CandidateHash + "|" + replay.TimelineFingerprint, StringComparer.Ordinal),
            group => group.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1,
            group =>
            {
                var score = HasNearSameGametime(group) ? 60 : 75;
                return CreateCluster(score, ["SameCandidateHash", "SameTimeline"], group);
            });

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(GetSimilarMetadataKey, StringComparer.Ordinal),
            group => group.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1,
            group =>
            {
                var score = HasNearSameGametime(group) ? 45 : 60;
                return CreateCluster(score, ["SameCandidateHash", "SimilarMetadata", "SameMessageVector"], group);
            });

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(replay => replay.CandidateHash, StringComparer.OrdinalIgnoreCase),
            group => group.Count >= 3 && group.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1,
            group => CreateCluster(40, ["RepeatedCandidateHash"], group));

        AddGroupedClusters(
            clusters,
            seen,
            replays.GroupBy(replay => replay.PackageId + "|" + replay.CandidateHash, StringComparer.OrdinalIgnoreCase),
            group => group.Count() >= 3,
            group => CreateCluster(45, ["ManyFromPackage", "SameCandidateHash"], group));

        return clusters
            .Where(cluster => cluster.Score >= 40)
            .OrderByDescending(cluster => cluster.Score)
            .ThenByDescending(cluster => cluster.Replays.Count)
            .ThenBy(cluster => cluster.ClusterKey, StringComparer.Ordinal)
            .ToList();
    }

    private static void AddGroupedClusters<TKey>(
        List<SuspiciousCluster> clusters,
        HashSet<string> seen,
        IEnumerable<IGrouping<TKey, ScoredReplay>> groups,
        Func<List<ScoredReplay>, bool> include,
        Func<List<ScoredReplay>, SuspiciousCluster> create)
    {
        foreach (var group in groups)
        {
            var replays = group
                .OrderBy(replay => replay.PackageId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(replay => replay.SourceName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (replays.Count <= 1 || !include(replays))
            {
                continue;
            }

            var cluster = create(replays);
            if (seen.Add(cluster.ClusterKey))
            {
                clusters.Add(cluster);
            }
        }
    }

    private static SuspiciousCluster CreateCluster(
        int score,
        IReadOnlyCollection<string> reasons,
        List<ScoredReplay> replays)
    {
        var orderedReasons = reasons
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reason => reason, StringComparer.Ordinal)
            .ToList();
        var key = string.Join(
            "|",
            orderedReasons.Concat(replays
                .Select(replay => replay.ReplayHash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(hash => hash, StringComparer.OrdinalIgnoreCase)));

        return new(score, orderedReasons, replays, key);
    }

    private static void PrintScoreReport(
        List<ScoredReplay> replays,
        List<SuspiciousCluster> clusters)
    {
        Console.WriteLine("Fake replay score report");
        Console.WriteLine($"ReplayDtos scored: {replays.Count}");
        Console.WriteLine($"Packages/sources: {replays.Select(replay => replay.PackageId).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
        Console.WriteLine($"Unique replay hashes: {replays.Select(replay => replay.ReplayHash).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
        Console.WriteLine($"Unique candidate hashes: {replays.Select(replay => replay.CandidateHash).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
        Console.WriteLine($"Suspicious clusters: {clusters.Count}");
        Console.WriteLine();

        if (clusters.Count == 0)
        {
            Console.WriteLine("No suspicious copy clusters found with the current ReplayDto-only rules.");
            return;
        }

        foreach (var cluster in clusters.Take(30))
        {
            Console.WriteLine($"Score {cluster.Score,3} | {string.Join(", ", cluster.Reasons)} | Replays {cluster.Replays.Count}");
            foreach (var replay in cluster.Replays.Take(12))
            {
                Console.WriteLine(
                    $"  {replay.PackageId}/{replay.SourceName} " +
                    $"hash={ShortHash(replay.ReplayHash)} cand={ShortHash(replay.CandidateHash)} " +
                    $"time={replay.Replay.Gametime:yyyy-MM-dd HH:mm:ss} dur={replay.Replay.Duration}s " +
                    $"winner={replay.Replay.WinnerTeam} players={TrimForConsole(replay.Players, 80)}");
                Console.WriteLine(
                    $"    cmdrs={TrimForConsole(replay.CommanderLayout, 80)} msg={TrimForConsole(replay.MessageVector, 80)}");
            }

            if (cluster.Replays.Count > 12)
            {
                Console.WriteLine($"  ... +{cluster.Replays.Count - 12} more");
            }

            Console.WriteLine();
        }
    }

    private static string ComputeContentFingerprint(
        ReplayDto replay,
        string commanderLayout,
        string messageVector)
    {
        var sb = new StringBuilder();
        sb.Append("title=").Append(replay.Title);
        sb.Append("|mode=").Append((int)replay.GameMode);
        sb.Append("|region=").Append(replay.RegionId);
        sb.Append("|duration=").Append(replay.Duration);
        sb.Append("|winner=").Append(replay.WinnerTeam);
        sb.Append("|middle=").AppendJoin(',', replay.MiddleChanges);
        sb.Append("|cmd=").Append(commanderLayout);
        sb.Append("|msg=").Append(messageVector);
        AppendPlayerTimelines(sb, replay, includeSpawnValues: false);
        return Sha256Hex(sb.ToString());
    }

    private static string ComputeTimelineFingerprint(ReplayDto replay)
    {
        var sb = new StringBuilder();
        sb.Append("mode=").Append((int)replay.GameMode);
        sb.Append("|players=").Append(replay.Players.Count);
        AppendPlayerTimelines(sb, replay, includeSpawnValues: false);
        return Sha256Hex(sb.ToString());
    }

    private static void AppendPlayerTimelines(StringBuilder sb, ReplayDto replay, bool includeSpawnValues)
    {
        foreach (var player in replay.Players.OrderBy(player => player.GamePos))
        {
            sb.Append("|p=").Append(player.GamePos);
            sb.Append(':').Append((int)player.Race);
            sb.Append(':').Append((int)player.SelectedRace);
            sb.Append("|tier=").AppendJoin(',', player.TierUpgrades.Order());
            sb.Append("|ref=").AppendJoin(',', player.Refineries.Order());
            sb.Append("|up=");
            foreach (var upgrade in player.Upgrades
                .OrderBy(upgrade => upgrade.Gameloop)
                .ThenBy(upgrade => upgrade.Name, StringComparer.Ordinal))
            {
                sb.Append(upgrade.Gameloop).Append(':').Append(upgrade.Name).Append(',');
            }

            sb.Append("|sp=");
            foreach (var spawn in player.Spawns.OrderBy(spawn => spawn.Breakpoint))
            {
                sb.Append((int)spawn.Breakpoint).Append('[');
                if (includeSpawnValues)
                {
                    sb.Append(spawn.Income).Append(':')
                        .Append(spawn.GasCount).Append(':')
                        .Append(spawn.ArmyValue).Append(':')
                        .Append(spawn.KilledValue).Append(':')
                        .Append(spawn.LostValue).Append(':')
                        .Append(spawn.UpgradeSpent).Append('|');
                }

                foreach (var unit in spawn.Units
                    .OrderBy(unit => unit.Name, StringComparer.Ordinal)
                    .ThenBy(unit => unit.Count))
                {
                    sb.Append(unit.Name).Append(':').Append(unit.Count).Append(',');
                }

                sb.Append(']');
            }
        }
    }

    private static string GetSimilarMetadataKey(ScoredReplay replay)
    {
        var durationBucket = replay.Replay.Duration / 30;
        return string.Join(
            '|',
            replay.CandidateHash,
            durationBucket.ToString(CultureInfo.InvariantCulture),
            replay.Replay.WinnerTeam.ToString(CultureInfo.InvariantCulture),
            replay.CommanderLayout,
            replay.MessageVector);
    }

    private static string GetCommanderLayout(ReplayDto replay)
    {
        return string.Join(
            ',',
            replay.Players
                .OrderBy(player => player.GamePos)
                .Select(player => $"{player.GamePos}:{player.TeamId}:{(int)player.Race}:{(int)player.SelectedRace}"));
    }

    private static string GetMessageVector(ReplayDto replay)
    {
        return string.Join(
            ',',
            replay.Players
                .OrderBy(player => player.GamePos)
                .Select(player => $"{player.GamePos}:{player.Messages}:{player.Apm}:{player.Pings}"));
    }

    private static string GetPlayersDisplay(ReplayDto replay)
    {
        return string.Join(
            " vs ",
            replay.Players
                .OrderBy(player => player.TeamId)
                .ThenBy(player => player.GamePos)
                .GroupBy(player => player.TeamId)
                .Select(team => string.Join(
                    ",",
                    team.Select(player => $"{player.Name}({player.Player.ToonId.Region}/{player.Player.ToonId.Realm}/{player.Player.ToonId.Id})"))));
    }

    private static bool HasMeaningfullyDifferentGametime(IReadOnlyCollection<ScoredReplay> replays)
    {
        return (replays.Max(replay => replay.Replay.Gametime) - replays.Min(replay => replay.Replay.Gametime)).TotalMinutes > 2;
    }

    private static bool HasNearSameGametime(IReadOnlyCollection<ScoredReplay> replays)
    {
        return (replays.Max(replay => replay.Replay.Gametime) - replays.Min(replay => replay.Replay.Gametime)).TotalMinutes <= 1.5;
    }

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ShortHash(string hash)
    {
        return hash.Length <= 12 ? hash : hash[..12];
    }

    private static void PrintHeader(InspectOptions options, DateTime localStart, DateTime localEnd)
    {
        Console.WriteLine("dsstats replayblob import inspector");
        Console.WriteLine($"Replayblob root: {options.ReplayBlobRoot}");
        Console.WriteLine($"Import window UTC: {options.ImportStartUtc:O} .. {options.ImportEndUtc:O}");
        Console.WriteLine($"File LastWriteTime window: {localStart:yyyy-MM-dd HH:mm:ss} .. {localEnd:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Timezone source: {(options.OffsetOverride is null ? TimeZoneInfo.Local.DisplayName : options.OffsetOverride.Value.ToString())}");
        Console.WriteLine();
    }

    private static DateTime ConvertUtcToFileTime(DateTime utc, TimeSpan? offsetOverride)
    {
        utc = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        if (offsetOverride is not null)
        {
            return DateTime.SpecifyKind(utc + offsetOverride.Value, DateTimeKind.Unspecified);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
    }

    private static IEnumerable<DirectoryInfo> EnumerateCandidatePackages(
        string spawnPlaybackRoot,
        DateTime localStart,
        DateTime localEnd)
    {
        return new DirectoryInfo(spawnPlaybackRoot)
            .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
            .Where(directory => directory.LastWriteTime >= localStart && directory.LastWriteTime <= localEnd)
            .OrderBy(directory => directory.LastWriteTime)
            .ThenBy(directory => directory.Name, StringComparer.Ordinal);
    }

    private static async Task<PackageSummary> InspectPackage(DirectoryInfo packageDirectory)
    {
        var requestPath = Path.Combine(packageDirectory.FullName, "request.json.gz");
        var manifestPath = Path.Combine(packageDirectory.FullName, "manifest.json");
        var summary = new PackageSummary(packageDirectory.Name, packageDirectory.LastWriteTime);

        try
        {
            if (!File.Exists(requestPath))
            {
                return summary.WithError("request.json.gz missing");
            }

            await using var requestStream = File.OpenRead(requestPath);
            await using var gzip = new GZipStream(requestStream, CompressionMode.Decompress);
            var uploadRequest = await JsonSerializer.DeserializeAsync<UploadRequestDto>(gzip, UploadJsonOptions);
            if (uploadRequest is null)
            {
                return summary.WithError("request.json.gz deserialized to null");
            }

            summary.AppVersion = uploadRequest.AppVersion;
            summary.RequestNames = FormatRequestNames(uploadRequest.RequestNames);

            foreach (var replay in uploadRequest.Replays)
            {
                var replayHash = replay.ComputeHash();
                var candidateHash = replay.ComputeCandidateHash();
                summary.RecordReplayHash(replayHash);
                summary.RecordCandidateHash(candidateHash);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            return summary.WithError($"{ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (File.Exists(manifestPath))
            {
                await using var manifestStream = File.OpenRead(manifestPath);
                var manifest = await JsonSerializer.DeserializeAsync<List<SpawnPlaybackUploadManifestEntryDto>>(
                    manifestStream,
                    UploadJsonOptions);

                foreach (var entry in manifest ?? [])
                {
                    summary.RecordManifestHash(entry.ReplayHash);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            summary.ManifestError = $"{ex.GetType().Name}: {ex.Message}";
        }

        return summary;
    }

    private static string FormatRequestNames(IReadOnlyCollection<RequestNames> requestNames)
    {
        if (requestNames.Count == 0)
        {
            return "-";
        }

        return string.Join(
            ", ",
            requestNames
                .Take(4)
                .Select(name => $"{name.Name}({name.RegionId}/{name.RealmId}/{name.ToonId})"))
            + (requestNames.Count > 4 ? $", +{requestNames.Count - 4}" : string.Empty);
    }

    private static void PrintPackage(PackageSummary package)
    {
        var version = string.IsNullOrWhiteSpace(package.AppVersion) ? "-" : package.AppVersion;
        var manifest = package.ManifestError is null
            ? package.ManifestEntries.ToString(CultureInfo.InvariantCulture)
            : $"ERR:{package.ManifestError}";
        var requestNames = package.RequestNames;

        Console.WriteLine(
            $"{package.LastWriteTime:yyyy-MM-dd HH:mm:ss}  " +
            $"{package.PackageId,-32}  " +
            $"{package.ReplayCount,7} " +
            $"{package.UniqueReplayHashCount,12} " +
            $"{manifest,8} " +
            $"{TrimForConsole(requestNames, 32),-32} " +
            $"{version}");

        if (package.Error is not null)
        {
            Console.WriteLine($"  ERROR: {package.Error}");
        }
    }

    private static void PrintSummary(ImportSummary summary)
    {
        var expectedSeenByImportService = SuspectImportExpectations.Imported
            + SuspectImportExpectations.Duplicates
            + SuspectImportExpectations.Replaced;
        var replayHashDuplicates = summary.TotalReplayDtos - summary.ReplayHashCounts.Count;
        var candidateHashDuplicates = summary.TotalReplayDtos - summary.CandidateHashCounts.Count;

        Console.WriteLine();
        Console.WriteLine("Summary");
        Console.WriteLine($"Packages: {summary.Packages}");
        Console.WriteLine($"Unreadable packages: {summary.UnreadablePackages}");
        Console.WriteLine($"ReplayDtos in requests: {summary.TotalReplayDtos}");
        Console.WriteLine($"Manifest entries: {summary.ManifestEntries}");
        Console.WriteLine($"Unique replay hashes: {summary.ReplayHashCounts.Count}");
        Console.WriteLine($"Duplicate replay-hash rows: {replayHashDuplicates}");
        Console.WriteLine($"Unique candidate hashes: {summary.CandidateHashCounts.Count}");
        Console.WriteLine($"Duplicate candidate-hash rows: {candidateHashDuplicates}");
        Console.WriteLine($"Expected log packages: {SuspectImportExpectations.PackageCount}");
        Console.WriteLine($"Expected log imported/dups/replaced: {SuspectImportExpectations.Imported}/{SuspectImportExpectations.Duplicates}/{SuspectImportExpectations.Replaced}");
        Console.WriteLine($"Expected log rows seen by import service: {expectedSeenByImportService}");

        PrintTopDuplicates("Top replay-hash duplicates", summary.ReplayHashCounts);
        PrintTopDuplicates("Top candidate-hash duplicates", summary.CandidateHashCounts);

        if (summary.Packages != SuspectImportExpectations.PackageCount)
        {
            Warn($"package count mismatch: found {summary.Packages}, expected {SuspectImportExpectations.PackageCount}");
        }

        if (summary.TotalReplayDtos != expectedSeenByImportService)
        {
            Warn(
                "request replay count does not equal imported + duplicates + replaced; " +
                $"request={summary.TotalReplayDtos}, log={expectedSeenByImportService}, delta={summary.TotalReplayDtos - expectedSeenByImportService}. " +
                "Use duplicate-hash rows above as the first explanation point; invalid or skipped replays may account for the rest.");
        }

        if (summary.UnreadablePackages > 0)
        {
            Warn($"{summary.UnreadablePackages} package(s) could not be fully read.");
        }
    }

    private static void PrintTopDuplicates(string title, Dictionary<string, int> counts)
    {
        var duplicates = counts
            .Where(kvp => kvp.Value > 1)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        Console.WriteLine(title + ":");
        if (duplicates.Count == 0)
        {
            Console.WriteLine("  none");
            return;
        }

        foreach (var (hash, count) in duplicates)
        {
            Console.WriteLine($"  {hash} x{count}");
        }
    }

    private static void Warn(string message)
    {
        Console.WriteLine($"WARNING: {message}");
    }

    private static List<HashDuplicateSummary> GetDuplicateSummaries(Dictionary<string, int> counts)
    {
        return counts
            .Where(kvp => kvp.Value > 1)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Take(10)
            .Select(kvp => new HashDuplicateSummary(kvp.Key, kvp.Value))
            .ToList();
    }

    private static void AddCount(Dictionary<string, int> counts, string hash)
    {
        if (!counts.TryAdd(hash, 1))
        {
            counts[hash]++;
        }
    }

    private static string TrimForConsole(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    private sealed record InspectOptions(
        string ReplayBlobRoot,
        DateTime ImportStartUtc,
        DateTime ImportEndUtc,
        TimeSpan? OffsetOverride,
        string? ExtractPackageId,
        string? OutputDirectory,
        List<string> ScorePackageIds,
        List<string> ScorePaths,
        bool ScoreWindow,
        bool ShowHelp)
    {
        public bool IsScoreMode => ScoreWindow || ScorePackageIds.Count > 0 || ScorePaths.Count > 0;
        public bool NeedsReplayBlobRoot => ExtractPackageId is not null || ScoreWindow || ScorePackageIds.Count > 0 || !IsScoreMode;

        public static InspectOptions Parse(string[] args)
        {
            if (args.Any(arg => arg is "-h" or "--help" or "/?"))
            {
                return new(DefaultReplayBlobRoot, GetDefaultStart(), GetDefaultEnd(), null, null, null, [], [], ScoreWindow: false, ShowHelp: true);
            }

            var positionalArgs = new List<string>();
            string? extractPackageId = null;
            string? outputDirectory = null;
            List<string> scorePackageIds = [];
            List<string> scorePaths = [];
            var scoreWindow = false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is "--extract-package" or "--package")
                {
                    extractPackageId = ReadOptionValue(args, ref i, arg);
                    continue;
                }

                if (arg is "--output" or "--out" or "-o")
                {
                    outputDirectory = ReadOptionValue(args, ref i, arg);
                    continue;
                }

                if (arg is "--score-package")
                {
                    scorePackageIds.Add(ReadOptionValue(args, ref i, arg));
                    continue;
                }

                if (arg is "--score-path")
                {
                    scorePaths.Add(ReadOptionValue(args, ref i, arg));
                    continue;
                }

                if (arg is "--score-window")
                {
                    scoreWindow = true;
                    continue;
                }

                positionalArgs.Add(arg);
            }

            var replayBlobRoot = positionalArgs.Count >= 1 && !string.IsNullOrWhiteSpace(positionalArgs[0])
                ? positionalArgs[0]
                : DefaultReplayBlobRoot;

            var importStartUtc = positionalArgs.Count >= 2
                ? ParseUtc(positionalArgs[1], nameof(ImportStartUtc))
                : GetDefaultStart();

            var importEndUtc = positionalArgs.Count >= 3
                ? ParseUtc(positionalArgs[2], nameof(ImportEndUtc))
                : GetDefaultEnd();

            TimeSpan? offset = positionalArgs.Count >= 4
                ? ParseOffset(positionalArgs[3])
                : null;

            if (importEndUtc < importStartUtc)
            {
                throw new ArgumentException("Import end must be greater than or equal to import start.");
            }

            if (extractPackageId is not null && (scoreWindow || scorePackageIds.Count > 0 || scorePaths.Count > 0))
            {
                throw new ArgumentException("--extract-package cannot be combined with score modes.");
            }

            return new(
                replayBlobRoot,
                importStartUtc,
                importEndUtc,
                offset,
                extractPackageId,
                outputDirectory,
                scorePackageIds,
                scorePaths,
                scoreWindow,
                ShowHelp: false);
        }

        private static string ReadOptionValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
            {
                throw new ArgumentException($"{optionName} requires a value.");
            }

            index++;
            return args[index];
        }

        private static DateTime GetDefaultStart() => DefaultImportStartUtc.Subtract(DefaultPadding);

        private static DateTime GetDefaultEnd() => DefaultImportEndUtc.Add(DefaultPadding);

        private static DateTime ParseUtc(string value, string argumentName)
        {
            if (!DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                throw new ArgumentException($"Could not parse {argumentName}: {value}");
            }

            return parsed.UtcDateTime;
        }

        private static TimeSpan ParseOffset(string value)
        {
            var sign = 1;
            if (value.StartsWith('+'))
            {
                value = value[1..];
            }
            else if (value.StartsWith('-'))
            {
                sign = -1;
                value = value[1..];
            }

            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var offset))
            {
                return sign * offset;
            }

            throw new ArgumentException($"Could not parse offset: {value}");
        }
    }

    private sealed record LogExpectations(
        int PackageCount,
        int Imported,
        int Duplicates,
        int Replaced);

    private sealed record ExtractedPackageSummary(
        string PackageId,
        string SourcePackageDirectory,
        string SourceRequestPath,
        string OutputDirectory,
        string AppVersion,
        List<RequestNames> RequestNames,
        int ReplayCount,
        int UniqueReplayHashes,
        int DuplicateReplayHashRows,
        int UniqueCandidateHashes,
        int DuplicateCandidateHashRows,
        int ManifestEntries,
        int UniqueManifestHashes,
        List<HashDuplicateSummary> TopReplayHashDuplicates,
        List<HashDuplicateSummary> TopCandidateHashDuplicates);

    private sealed record HashDuplicateSummary(string Hash, int Count);

    private sealed record ScoredReplay(
        ReplayDto Replay,
        string PackageId,
        string SourceRoot,
        string SourceName,
        string ReplayHash,
        string CandidateHash,
        string ContentFingerprint,
        string TimelineFingerprint,
        string CommanderLayout,
        string MessageVector,
        string Players);

    private sealed record SuspiciousCluster(
        int Score,
        List<string> Reasons,
        List<ScoredReplay> Replays,
        string ClusterKey);

    private sealed class ImportSummary
    {
        public int Packages { get; private set; }
        public int UnreadablePackages { get; private set; }
        public int TotalReplayDtos { get; private set; }
        public int ManifestEntries { get; private set; }
        public Dictionary<string, int> ReplayHashCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> CandidateHashCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Add(PackageSummary package)
        {
            Packages++;
            if (package.Error is not null)
            {
                UnreadablePackages++;
            }

            TotalReplayDtos += package.ReplayCount;
            ManifestEntries += package.ManifestEntries;
            AddCounts(ReplayHashCounts, package.ReplayHashCounts);
            AddCounts(CandidateHashCounts, package.CandidateHashCounts);
        }

        private static void AddCounts(Dictionary<string, int> target, Dictionary<string, int> source)
        {
            foreach (var (hash, count) in source)
            {
                if (!target.TryAdd(hash, count))
                {
                    target[hash] += count;
                }
            }
        }
    }

    private sealed class PackageSummary(string packageId, DateTime lastWriteTime)
    {
        private readonly HashSet<string> manifestHashes = new(StringComparer.OrdinalIgnoreCase);

        public string PackageId { get; } = packageId;
        public DateTime LastWriteTime { get; } = lastWriteTime;
        public string AppVersion { get; set; } = string.Empty;
        public string RequestNames { get; set; } = "-";
        public int ReplayCount { get; private set; }
        public int ManifestEntries { get; private set; }
        public string? Error { get; private set; }
        public string? ManifestError { get; set; }
        public Dictionary<string, int> ReplayHashCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> CandidateHashCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int UniqueReplayHashCount => ReplayHashCounts.Count;

        public void RecordReplayHash(string replayHash)
        {
            ReplayCount++;
            AddCount(ReplayHashCounts, replayHash);
        }

        public void RecordCandidateHash(string candidateHash)
        {
            AddCount(CandidateHashCounts, candidateHash);
        }

        public void RecordManifestHash(string replayHash)
        {
            ManifestEntries++;
            manifestHashes.Add(replayHash);
        }

        public PackageSummary WithError(string error)
        {
            Error = error;
            return this;
        }

        private static void AddCount(Dictionary<string, int> counts, string hash)
        {
            Program.AddCount(counts, hash);
        }
    }
}
