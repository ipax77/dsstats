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

    public static readonly string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker");

    private static readonly string configFile = Path.Combine(appFolder, "workerconfig.json");
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
    internal readonly Version CurrentVersion = new(3, 0, 5);

    public async Task StartImportAsync(CancellationToken token)
    {
        var config = await GetConfig();
        try
        {
            var toDoReplayPaths = await GetToDoReplayPaths(config, token);
            if (toDoReplayPaths.Count == 0)
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

            var decodeTask = DecodeToChannelAsync(config, toDoReplayPaths, channel.Writer, token);
            var importTask = ImportFromChannelAsync(channel.Reader, token);

            await Task.WhenAll(decodeTask, importTask);

            await Upload(config, token);
            sw.Stop();
            logger.LogWarning("{count} replays decoded in {time}.", toDoReplayPaths.Count, sw.Elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture));
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
            await Parallel.ForEachAsync(
                replayPaths,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = GetDecodeDegreeOfParallelism(config),
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

    private async Task ImportFromChannelAsync(
        ChannelReader<ReplayImportDto> reader,
        CancellationToken ct)
    {
        var batch = new List<ReplayImportDto>(ImportBatchSize);

        try
        {
            await foreach (var replayImport in reader.ReadAllAsync(ct))
            {
                batch.Add(replayImport);

                if (batch.Count >= ImportBatchSize)
                {
                    await ImportBatchAsync(batch, ct);
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
            }
        }
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

    private async Task<IReadOnlyCollection<string>> GetToDoReplayPaths(AppOptions config, CancellationToken ct)
    {
        var existingReplayPaths = await GetExistingReplayPaths(ct);
        var replayPaths = GetHdReplayPaths(config);
        replayPaths.ExceptWith(existingReplayPaths);
        replayPaths.ExceptWith(config.IgnoreReplays);
        return replayPaths.Take(dsstatsConfig.Value.BatchSize).ToList();
    }

    private async Task<HashSet<string>> GetExistingReplayPaths(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        return await context.Replays
            .Where(x => x.FileName != null)
            .Select(x => x.FileName!)
            .ToHashSetAsync(ct);
    }

    private static HashSet<string> GetHdReplayPaths(AppOptions config)
    {
        var folders = GetReplayFolders(config);
        HashSet<string> replayPaths = [];

        List<FileInfo> fileInfos = [];
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }
            var dir = new DirectoryInfo(folder);
            fileInfos.AddRange(dir.GetFiles(
                    $"{config.ReplayStartName}*.SC2Replay",
                    SearchOption.AllDirectories)
                );
        }

        return fileInfos
                .OrderByDescending(o => o.CreationTimeUtc).Select(s => s.FullName)
                .ToHashSet();
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

    private sealed record DecodeReplayResult(ReplayImportDto? Import, string? Error);
}
