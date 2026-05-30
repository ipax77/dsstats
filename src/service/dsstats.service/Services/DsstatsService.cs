using dsstats.db;
using dsstats.dbServices;
using dsstats.parser;
using dsstats.service.Models;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using s2protocol.NET;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Threading.Channels;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService(IServiceScopeFactory scopeFactory,
                                    IHttpClientFactory httpClientFactory,
                                    IOptions<DsstatsConfig> dsstatsConfig,
                                    ILogger<DsstatsService> logger) : IDisposable
{
    private const int ImportBatchSize = 25;
    private const int DecodeBacklogCapacity = 4;
    private const int MaxDecodeParallelism = 4;

    public static readonly string appFolder = DsstatsServicePaths.AppFolder;

    private static readonly string configFile = DsstatsServicePaths.ConfigFile;
    private readonly ReplayDecoder _replayDecoder = new();
    private readonly ReplayDecoderOptions _decoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    private int _startupLogged;
    internal readonly Version CurrentVersion = new(3, 0, 6);

    public async Task StartImportAsync(CancellationToken token)
    {
        var config = await GetConfig();
        LogStartupStateOnce(config);
        if (!config.AutoDecode)
        {
            logger.LogWarning("Auto decode is disabled in {ConfigFile}.", configFile);
            return;
        }

        try
        {
            var discovery = await GetToDoReplayPaths(config, token);
            if (discovery.ReplayPaths.Count == 0)
            {
                logger.LogWarning("no replay to decode found.");
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            var channel = Channel.CreateBounded<ReplayImportDto>(
            new BoundedChannelOptions(DecodeBacklogCapacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            var decodeTask = DecodeToChannelAsync(config, discovery.ReplayPaths, channel.Writer, token);
            var importTask = ImportFromChannelAsync(channel.Reader, token);

            await Task.WhenAll(decodeTask, importTask);
            var importedCount = await importTask;

            var uploadedCount = await Upload(config, token);
            sw.Stop();
            logger.LogWarning(
                "Decoded {DecodedCount} replay(s), imported {ImportedCount}, uploaded {UploadedCount} in {Elapsed}.",
                discovery.ReplayPaths.Count,
                importedCount,
                uploadedCount,
                sw.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Failed decoding replays: {error}", ex.Message);
        }
    }

    private async Task DecodeToChannelAsync(
        AppOptions config,
        IReadOnlyCollection<string> replayPaths,
        ChannelWriter<ReplayImportDto> writer,
        CancellationToken ct)
    {
        try
        {
            var uploaders = GetToonIds(config);
            var decodeDegree = GetDecodeDegreeOfParallelism(config);
            await Parallel.ForEachAsync(
                replayPaths,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = decodeDegree,
                    CancellationToken = ct
                },
                async (replayPath, token) =>
                {
                    var result = await DecodeReplayImportAsync(replayPath, uploaders, token);
                    if (result.Import is null)
                    {
                        logger.LogWarning("Failed decoding replay {replay}: {error}", replayPath, result.Error);
                        return;
                    }

                    await writer.WriteAsync(result.Import, token);
                });
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task<DecodeReplayResult> DecodeReplayImportAsync(
        string replayPath,
        List<ToonIdDto> uploaders,
        CancellationToken ct)
    {
        try
        {
            var sc2Replay = await _replayDecoder.DecodeAsync(replayPath, _decoderOptions, ct);
            if (sc2Replay is null)
            {
                return new(null, "failed decoding");
            }

            var replayImport = DsstatsParser.ParseReplayImport(
                sc2Replay,
                spawnPlaybackEncoder: sidecar => SpawnPlaybackSidecarCodec.EncodeWithMetadata(
                    sidecar,
                    CompressionLevel.Fastest));

            replayImport.Replay.FileName = replayPath;
            replayImport.Replay.SetUploader(uploaders);
            return new(replayImport, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    private async Task<int> ImportFromChannelAsync(
        ChannelReader<ReplayImportDto> reader,
        CancellationToken ct)
    {
        var batch = new List<ReplayImportDto>(ImportBatchSize);
        var importedCount = 0;

        try
        {
            await foreach (var replayImport in reader.ReadAllAsync(ct))
            {
                batch.Add(replayImport);

                if (batch.Count >= ImportBatchSize)
                {
                    await ImportBatchAsync(batch, ct);
                    importedCount += batch.Count;
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // fall through to final flush
        }
        finally
        {
            if (batch.Count > 0)
            {
                await ImportBatchAsync(batch, CancellationToken.None);
                importedCount += batch.Count;
            }
        }

        return importedCount;
    }

    private async Task ImportBatchAsync(
        List<ReplayImportDto> batch,
        CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.InsertReplayImports(batch);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    internal async Task<ReplayDiscoveryResult> GetToDoReplayPaths(AppOptions config, CancellationToken ct)
    {
        return await DiscoverReplayPaths(
            config,
            dsstatsConfig.Value.BatchSize,
            GetExistingReplayPaths,
            ct);
    }

    private async Task<HashSet<string>> GetExistingReplayPaths(IReadOnlyCollection<string> replayPaths, CancellationToken ct)
    {
        if (replayPaths.Count == 0)
        {
            return [];
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var existingReplayPaths = await context.Replays
            .AsNoTracking()
            .Where(x => x.FileName != null && replayPaths.Contains(x.FileName))
            .Select(x => x.FileName!)
            .ToListAsync(ct);

        return existingReplayPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal static async Task<ReplayDiscoveryResult> DiscoverReplayPaths(
        AppOptions config,
        int batchSize,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<HashSet<string>>> getExistingReplayPaths,
        CancellationToken ct)
    {
        var folders = GetReplayFolders(config);
        var ignoreReplays = config.IgnoreReplays.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new ReplayCandidateQueue(Math.Max(1, batchSize));
        var pending = new List<ReplayFileCandidate>(Math.Max(ImportBatchSize, Math.Min(250, batchSize * 2)));
        var scannedCount = 0;
        var ignoredCount = 0;
        var existingCount = 0;

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var candidate in EnumerateReplayFileCandidates(folder, config.ReplayStartName))
            {
                ct.ThrowIfCancellationRequested();
                scannedCount++;
                if (ignoreReplays.Contains(candidate.Path))
                {
                    ignoredCount++;
                    continue;
                }

                pending.Add(candidate);
                if (pending.Count >= 250)
                {
                    existingCount += await AddPendingReplayCandidates(pending, candidates, getExistingReplayPaths, ct);
                    pending.Clear();
                }
            }
        }

        if (pending.Count > 0)
        {
            existingCount += await AddPendingReplayCandidates(pending, candidates, getExistingReplayPaths, ct);
            pending.Clear();
        }

        return new(
            candidates.GetReplayPathsNewestFirst(),
            folders.Count,
            scannedCount,
            ignoredCount,
            existingCount);
    }

    private static IEnumerable<ReplayFileCandidate> EnumerateReplayFileCandidates(string folder, string replayStartName)
    {
        foreach (var file in EnumerateReplayFilesSafe(folder, $"{replayStartName}*.SC2Replay"))
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new(file);
            }
            catch (Exception)
            {
                continue;
            }

            yield return new(fileInfo.FullName, fileInfo.CreationTimeUtc);
        }
    }

    private static IEnumerable<string> EnumerateReplayFilesSafe(string folder, string pattern)
    {
        var pendingFolders = new Stack<string>();
        pendingFolders.Push(folder);

        while (pendingFolders.Count > 0)
        {
            var currentFolder = pendingFolders.Pop();
            string[] files;
            try
            {
                files = Directory.GetFiles(currentFolder, pattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            string[] childFolders;
            try
            {
                childFolders = Directory.GetDirectories(currentFolder);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var childFolder in childFolders)
            {
                pendingFolders.Push(childFolder);
            }
        }
    }

    private static async Task<int> AddPendingReplayCandidates(
        List<ReplayFileCandidate> pending,
        ReplayCandidateQueue candidates,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<HashSet<string>>> getExistingReplayPaths,
        CancellationToken ct)
    {
        var pendingPaths = pending.Select(candidate => candidate.Path).ToList();
        var existingReplayPaths = await getExistingReplayPaths(pendingPaths, ct);
        foreach (var candidate in pending)
        {
            if (!existingReplayPaths.Contains(candidate.Path))
            {
                candidates.Enqueue(candidate);
            }
        }

        return existingReplayPaths.Count;
    }

    private static int GetDecodeDegreeOfParallelism(AppOptions config)
    {
        var configuredCores = config.CPUCores <= 0
            ? Environment.ProcessorCount
            : config.CPUCores;

        var coreLimit = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, MaxDecodeParallelism));
        return Math.Max(1, Math.Min(configuredCores, coreLimit));
    }

    public void Dispose()
    {
        _replayDecoder.Dispose();
        _dbSemaphore.Dispose();
        configSemaphore.Dispose();
    }

    private void LogStartupStateOnce(AppOptions config)
    {
        if (Interlocked.Exchange(ref _startupLogged, 1) != 0)
        {
            return;
        }

        logger.LogWarning(
            "Dsstats service {Version} ready. Profiles: {ProfileCount}.",
            CurrentVersion.ToString(),
            config.Sc2Profiles.Count);
    }

    private sealed record DecodeReplayResult(ReplayImportDto? Import, string? Error);

    internal sealed record ReplayDiscoveryResult(
        IReadOnlyList<string> ReplayPaths,
        int ReplayFolderCount,
        int ScannedReplayCount,
        int IgnoredReplayCount,
        int ExistingReplayCount);

    private sealed record ReplayFileCandidate(string Path, DateTime CreationTimeUtc);

    private sealed class ReplayCandidateQueue(int capacity)
    {
        private readonly PriorityQueue<ReplayFileCandidate, DateTime> queue = new();

        public void Enqueue(ReplayFileCandidate candidate)
        {
            if (queue.Count < capacity)
            {
                queue.Enqueue(candidate, candidate.CreationTimeUtc);
                return;
            }

            if (queue.TryPeek(out var oldestCandidate, out _)
                && candidate.CreationTimeUtc > oldestCandidate.CreationTimeUtc)
            {
                queue.Dequeue();
                queue.Enqueue(candidate, candidate.CreationTimeUtc);
            }
        }

        public List<string> GetReplayPathsNewestFirst()
        {
            return queue.UnorderedItems
                .Select(item => item.Element)
                .OrderByDescending(candidate => candidate.CreationTimeUtc)
                .Select(candidate => candidate.Path)
                .ToList();
        }
    }
}
