using dsstats.indexedDb.Services;
using dsstats.parser;
using dsstats.shared;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using static dsstats.indexedDb.Services.IndexedDbService;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DecodeService> logger;
    private readonly PwaConfigService pwaConfigService;
    private readonly IHttpClientFactory httpClientFactory;
    private CancellationTokenSource cts = new();
    private readonly SemaphoreSlim ss = new(1, 1);
    public bool Decoding { get; private set; }

    public DecodeService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory,
                         ILogger<DecodeService> logger, PwaConfigService pwaConfigService)
    {
        this.scopeFactory = scopeFactory;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        this.pwaConfigService = pwaConfigService;
    }
    int replaysDecoded = 0;
    private readonly ReplayDecoder decoder = new ReplayDecoder();
    private readonly ReplayDecoderOptions decoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = true,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    public event EventHandler<DecodeInfoEventArgs>? DecodeStateChanged;
    private void OnDecodeStateChanged(DecodeInfoEventArgs e)
    {
        EventHandler<DecodeInfoEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    public async Task<ReplayDto?> DecodeFromStream(Stream stream, string originalFileName)
    {
        await ss.WaitAsync();
        Decoding = true;
        cts = new();
        Stopwatch sw = Stopwatch.StartNew();
        bool success = false;
        string message = string.Empty;
        ReplayDto? replayDto = null;

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();

        try
        {
            var sc2Replay = await decoder.DecodeAsync(stream, decoderOptions, cts.Token);
            if (sc2Replay is null)
            {
                logger.LogWarning("Failed to decode replay from stream.");
                message = "Failed to decode replay.";
                ArgumentNullException.ThrowIfNull(sc2Replay, message);
            }
            var replay = DsstatsParser.ParseReplay(sc2Replay, compat: true);
            await dbService.UpsertReplayAsync(replay.ComputeHash(), replay);
            logger.LogInformation("Decoded and saved replay from stream: {FileName}", originalFileName);
            success = true;
            replayDto = replay;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to decode replay from stream: {Message}", ex.Message);
            message = ex.Message;
        }
        finally
        {
            Decoding = false;
            ss.Release();
        }
        sw.Stop();
        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = success ? 1 : 0,
            Total = 1,
            Error = success ? 0 : 1,
            Elapsed = sw.Elapsed,
            Eta = TimeSpan.Zero,
            Saving = false,
            Finished = true,
            Info = success ? "Replay decoded successfully." : $"Failed to decode replay: {message}"
        });

        var config = await pwaConfigService.GetConfig();
        if (config.UploadCredential)
        {
            await Upload10(dbService);
        }

        return replayDto;
    }

    public async Task DecodeFromDirectory(int regionId, string? dirKey = null)
    {
        await ss.WaitAsync();
        Decoding = true;
        cts = new();
        Stopwatch sw = Stopwatch.StartNew();
        ConcurrentBag<string> failedReplays = [];
        ConcurrentBag<string> decodedReplays = [];
        var config = await pwaConfigService.GetConfig();
        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();

        try
        {
            replaysDecoded = 0;
            var existingPaths = await dbService.GetExistingPaths();
            logger.LogInformation("Found {Count} existing replay paths in database.", existingPaths.Count);


            var fileInfos = await dbService.PickDirectoryInit(regionId, config.ReplayStartName, dirKey);

            logger.LogInformation("Starting decoding of {FileCount} replays...", fileInfos.Count);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = config.CPUCores,
                CancellationToken = cts.Token
            };

            await Parallel.ForEachAsync(fileInfos, options, async (fileInfo, ct) =>
            {
                if (existingPaths.Contains(fileInfo.Path))
                {
                    return;
                }
                var result = await TryDecodeReplay(fileInfo.Path, dbService, ct);
                if (result.Success)
                {
                    decodedReplays.Add(fileInfo.Path);
                    logger.LogInformation("Decoded replay: {Path}", fileInfo.Path);
                }
                else
                {
                    failedReplays.Add(fileInfo.Path);
                    logger.LogWarning("Failed to decode replay: {Path}, Reason: {Reason}", fileInfo.Path, result.Message);
                }
                if (replaysDecoded % 5 == 0)
                {
                    OnDecodeStateChanged(new DecodeInfoEventArgs
                    {
                        Done = replaysDecoded,
                        Total = fileInfos.Count,
                        Error = failedReplays.Count,
                        Elapsed = sw.Elapsed,
                        Eta = TimeSpan.FromTicks(sw.Elapsed.Ticks * (fileInfos.Count - replaysDecoded) / Math.Max(replaysDecoded, 1)),
                        Saving = false,
                        Finished = false,
                        Info = $"Decoded: {replaysDecoded}, Failed: {failedReplays.Count}"
                    });
                }
            });
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning("Decoding operation was canceled: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to decode replays from directory: {Message}", ex.Message);
        }
        finally
        {
            Decoding = false;
            ss.Release();
        }
        sw.Stop();
        logger.LogInformation("Decoding completed. Decoded {DecodedCount} replays in {Elapsed} min.",
         replaysDecoded, sw.Elapsed.TotalMinutes.ToString("N2"));
        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = replaysDecoded,
            Total = replaysDecoded,
            Error = failedReplays.Count,
            Elapsed = sw.Elapsed,
            Eta = TimeSpan.Zero,
            Saving = false,
            Finished = true,
            Info = $"Decoded: {replaysDecoded}"
        });

        if (config.UploadCredential)
        {
            await Upload10(dbService);
        }
    }

    private async Task<DecodeResult> TryDecodeReplay(string path, IndexedDbService dbService, CancellationToken token)
    {
        try
        {
            var streamRef = await dbService.GetFileContent(path);
            using var stream = await streamRef.OpenReadStreamAsync(maxAllowedSize: 5_000_000, token); // 5MB
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms, token);
            ms.Position = 0;

            var sc2Replay = await decoder.DecodeAsync(ms, decoderOptions, token);
            if (sc2Replay is null)
            {
                return new DecodeResult
                {
                    Success = false,
                    Message = "Failed to decode replay.",
                    Path = path
                };
            }
            var replay = DsstatsParser.ParseReplay(sc2Replay, compat: true);
            replay.FileName = path;
            await dbService.UpsertReplayAsync(replay.ComputeHash(), replay);
            Interlocked.Increment(ref replaysDecoded);
            return new DecodeResult
            {
                Success = true,
                Message = "Replay decoded successfully.",
                Replay = replay,
                Path = path
            };
        }
        catch (Exception ex)
        {
            return new DecodeResult
            {
                Success = false,
                Message = ex.Message,
                Path = path
            };
        }
    }

    public async Task DecodeAllFromDirectory(int regionId, string? dirKey = null)
    {
        await ss.WaitAsync();
        Decoding = true;
        cts = new();
        Stopwatch sw = Stopwatch.StartNew();
        ConcurrentBag<string> failedReplays = [];
        var config = await pwaConfigService.GetConfig();

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();

        try
        {
            replaysDecoded = 0;
            var existingPaths = await dbService.GetExistingPaths();

            int batchIndex = 0;
            while (true)
            {
                // Ask frontend for the next batch of up to 100
                var fileInfos = await dbService.PickDirectoryInit(regionId, config.ReplayStartName, dirKey);

                if (fileInfos.Count == 0)
                    break; // no more new replays

                logger.LogInformation("Decoding batch {Batch} with {Count} replays", ++batchIndex, fileInfos.Count);

                await DecodeBatch(fileInfos, dbService, failedReplays, sw, config.CPUCores);

                // Extend the "already decoded" list so we don’t get duplicates
                foreach (var fi in fileInfos)
                    existingPaths.Add(fi.Path);
            }
        }
        finally
        {
            Decoding = false;
            ss.Release();
        }

        sw.Stop();
        logger.LogInformation("Finished decoding {Count} replays", replaysDecoded);
    }

    private async Task DecodeBatch(IEnumerable<FileInfoRecord> files,
                               IndexedDbService dbService,
                               ConcurrentBag<string> failedReplays,
                               Stopwatch sw,
                               int maxParallelism)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
            CancellationToken = cts.Token
        };

        await Parallel.ForEachAsync(files, options, async (fileInfo, ct) =>
        {
            var result = await TryDecodeReplay(fileInfo.Path, dbService, ct);
            if (!result.Success)
                failedReplays.Add(fileInfo.Path);

            if (replaysDecoded % 5 == 0)
            {
                OnDecodeStateChanged(new DecodeInfoEventArgs
                {
                    Done = replaysDecoded,
                    Total = -1,
                    Error = failedReplays.Count,
                    Elapsed = sw.Elapsed,
                    Eta = TimeSpan.Zero,
                    Saving = false,
                    Finished = false,
                    Info = $"Decoded: {replaysDecoded}, Failed: {failedReplays.Count}"
                });
            }
        });
    }


    public void Cancel()
    {
        cts.Cancel();
    }

    internal sealed class DecodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ReplayDto? Replay { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}

public class DecodeInfoEventArgs : EventArgs
{
    public int Done { get; set; }
    public int Total { get; set; }
    public int Error { get; set; }
    public TimeSpan Eta { get; set; }
    public TimeSpan Elapsed { get; set; }
    public bool Saving { get; set; }
    public bool Finished { get; set; }
    public string? Info { get; set; }
    public UploadStatus UploadStatus { get; set; }
}

public enum UploadStatus
{
    None = 0,
    Uploading = 1,
    UploadError = 2,
    UploadSuccess = 3,
    Forbidden = 4
}