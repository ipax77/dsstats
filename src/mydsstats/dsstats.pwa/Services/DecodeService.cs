using dsstats.indexedDb.Services;
using dsstats.parser;
using dsstats.pwa.Clients;
using dsstats.shared;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using static dsstats.indexedDb.Services.IndexedDbService;

namespace dsstats.pwa.Services;

public partial class DecodeService : IDisposable
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DecodeService> logger;
    private readonly PwaConfigService pwaConfigService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly CancellationTokenSource cts = new();
    private CancellationTokenSource? decodeCts;
    private readonly SemaphoreSlim ss = new(1, 1);
    public bool Decoding { get; private set; }
    public ReplayDto? LatestReplay { get; private set; }
    public string? LatestReplayHash { get; private set; }
    public static readonly Version Version = new(1, 3);
    private int _currentWorkerCount = -1;

    public DecodeService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory,
                         ILogger<DecodeService> logger, PwaConfigService pwaConfigService)
    {
        this.scopeFactory = scopeFactory;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        this.pwaConfigService = pwaConfigService;
    }
    int replaysDecoded = 0;
    private DecodeClient? _decodeClient;

    // Used only by DecodeFromStream (single-file upload path, no worker needed).
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

    [SupportedOSPlatform("browser")]
    private async Task EnsureWorkersAsync(int cpuCores)
    {
        if (_decodeClient != null && _currentWorkerCount == cpuCores)
            return;

        if (_decodeClient != null)
        {
            await _decodeClient.DisposeAsync(); // terminate old workers
            _decodeClient = null;
        }

        _decodeClient = new DecodeClient();
        await _decodeClient.InitAsync(cpuCores);

        _currentWorkerCount = cpuCores;
    }
    
    [SupportedOSPlatform("browser")]
    private async Task TeardownWorkersAsync()
    {
        if (_decodeClient is null) return;
        await _decodeClient.DisposeAsync();
        _decodeClient = null;
    }

    public event EventHandler<DecodeInfoEventArgs>? DecodeStateChanged;
    public event EventHandler? PromptForUpload;
    private int _decodeCompletionsWithoutUpload = 0;

    private void OnDecodeStateChanged(DecodeInfoEventArgs e)
    {
        EventHandler<DecodeInfoEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    private void OnPromptForUpload()
    {
        PromptForUpload?.Invoke(this, EventArgs.Empty);
    }

    public async Task<ReplayDto?> DecodeFromStream(Stream stream, string originalFileName)
    {
        await ss.WaitAsync();
        Decoding = true;
        decodeCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        Stopwatch sw = Stopwatch.StartNew();
        bool success = false;
        string message = string.Empty;
        ReplayDto? replayDto = null;

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();

        try
        {
            var sc2Replay = await decoder.DecodeAsync(stream, decoderOptions, decodeCts.Token);
            if (sc2Replay is null)
            {
                logger.LogWarning("Failed to decode replay from stream.");
                message = "Failed to decode replay.";
                ArgumentNullException.ThrowIfNull(sc2Replay, message);
            }
            var replay = DsstatsParser.ParseReplay(sc2Replay, compat: true);
            var hash = replay.ComputeHash();
            await dbService.UpsertReplayAsync(hash, replay);
            logger.LogInformation("Decoded and saved replay from stream: {FileName}", originalFileName);
            success = true;
            replayDto = replay;
            LatestReplay = replayDto;
            LatestReplayHash = hash;
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
            Done = 1,
            Successful = success ? 1 : 0,
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
        else
        {
            _decodeCompletionsWithoutUpload++;
            if (_decodeCompletionsWithoutUpload == 1 || _decodeCompletionsWithoutUpload % 10 == 0)
                OnPromptForUpload();
        }

        return replayDto;
    }

    [SupportedOSPlatform("browser")]
    public async Task DecodeFromDirectoryObsolete(int regionId, string? dirKey = null, int limit = 100)
    {
        await ss.WaitAsync();

        if (regionId == 0 && dirKey != null)
        {
            // Extract last character from dirKey and parse as integer
            if (dirKey.Length > 0 && char.IsDigit(dirKey[^1]))
            {
                regionId = int.Parse(dirKey[^1].ToString());
            }
        }

        Decoding = true;
        decodeCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        Stopwatch sw = Stopwatch.StartNew();
        ConcurrentBag<string> failedReplays = [];
        ConcurrentBag<string> decodedReplays = [];
        int totalFiles = 0;
        var config = await pwaConfigService.GetConfig();
        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();

        try
        {
            replaysDecoded = 0;
            var existingPaths = await dbService.GetExistingPaths();
            logger.LogInformation("Found {Count} existing replay paths in database.", existingPaths.Count);


            var fileInfos = await dbService.PickDirectoryInit(config.ReplayStartName, dirKey, limit);
            totalFiles = fileInfos.Count;

            logger.LogInformation("Starting decoding of {FileCount} replays...", fileInfos.Count);
            OnDecodeStateChanged(new DecodeInfoEventArgs
            {
                Done = 0,
                Total = totalFiles,
                Error = 0,
                Elapsed = sw.Elapsed,
                Eta = TimeSpan.Zero,
                Saving = false,
                Finished = false,
                Info = $"Starting decode of {totalFiles} replays..."
            });
            await EnsureWorkersAsync(config.CPUCores);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = config.CPUCores,
                CancellationToken = decodeCts.Token
            };

            var lastUpdate = Stopwatch.StartNew();
            var updateInterval = TimeSpan.FromMilliseconds(500);

            await Parallel.ForEachAsync(fileInfos, options, async (fileInfo, ct) =>
            {
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

                var current = Interlocked.Increment(ref replaysDecoded);

                if (lastUpdate.Elapsed >= updateInterval)
                {
                    lock (lastUpdate)
                    {
                        if (lastUpdate.Elapsed >= updateInterval)
                        {
                            lastUpdate.Restart();

                            OnDecodeStateChanged(new DecodeInfoEventArgs
                            {
                                Done = current,
                                Successful = current - failedReplays.Count,
                                Total = totalFiles,
                                Error = failedReplays.Count,
                                Elapsed = sw.Elapsed,
                                Eta = TimeSpan.FromTicks(
                                    sw.Elapsed.Ticks * (totalFiles - current) / Math.Max(current, 1)
                                ),
                                Saving = false,
                                Finished = false,
                                Info = $"Decoded: {current}, Failed: {failedReplays.Count}"
                            });
                        }
                    }
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
            if (decodeCts.IsCancellationRequested)
                await TeardownWorkersAsync();
            ss.Release();
        }
        sw.Stop();
        logger.LogInformation("Decoding completed. Decoded {DecodedCount} replays in {Elapsed} min.",
         replaysDecoded, sw.Elapsed.TotalMinutes.ToString("N2"));
        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = replaysDecoded + failedReplays.Count,
            Successful = replaysDecoded,
            Total = totalFiles,
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
        else
        {
            _decodeCompletionsWithoutUpload++;
            if (_decodeCompletionsWithoutUpload == 1 || _decodeCompletionsWithoutUpload % 10 == 0)
                OnPromptForUpload();
        }
    }

    [SupportedOSPlatform("browser")]
    public async Task TriggerUploadAsync()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        await Upload10(dbService);
    }

    [SupportedOSPlatform("browser")]
    public async Task<string?> AddDirectoryHandle()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        var config = await pwaConfigService.GetConfig();
        return await dbService.PickDirectoryHandle(config.ReplayStartName);
    }

    [SupportedOSPlatform("browser")]
    private async Task<DecodeResult> TryDecodeReplay(string path, IndexedDbService dbService, CancellationToken token)
    {
        try
        {
            // File read: JSInterop — must stay on main thread
            var streamRef = await dbService.GetFileContent(path);
            using var stream = await streamRef.OpenReadStreamAsync(maxAllowedSize: 5_000_000, token);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms, token);

            token.ThrowIfCancellationRequested();

            // CPU-heavy decode: dispatched to Web Worker
            var (success, error, hash, replay) = await _decodeClient!.DecodeAsync(ms.ToArray(), token);
            if (!success || replay is null)
                return new DecodeResult { Success = false, Message = error ?? "Worker decode error", Path = path };

            replay.FileName = path;

            // IndexedDB write: JSInterop — must stay on main thread
            await dbService.UpsertReplayAsync(hash!, replay);
            Interlocked.Increment(ref replaysDecoded);
            LatestReplay = replay;
            LatestReplayHash = hash;
            return new DecodeResult { Success = true, Message = "Replay decoded successfully.", Replay = replay, Path = path };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DecodeResult { Success = false, Message = ex.Message, Path = path };
        }
    }

    [SupportedOSPlatform("browser")]
    public async Task DecodeAllHandles(int limit = 0)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        var entries = await dbService.GetAllDirectoryHandleEntries();
        var aggregateState = new DecodeAggregateState();

        // Request all folder permissions up-front while the user gesture is still active.
        // Sequential calls chain off each other's activation (user clicks "Allow" → new activation).
        // This is best-effort; even if a dialog is skipped, DecodeFromDirectory handles it gracefully (0 files).
        await dbService.VerifyAllDirectoryPermissions(entries.Select(e => e.Key).ToList());

        foreach (var entry in entries)
        {
            if (cts.IsCancellationRequested) break;
            await DecodeFromDirectory(entry.Key, limit, aggregateState);
        }
    }

    public void Cancel()
    {
        decodeCts?.Cancel();
    }

    internal sealed class DecodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ReplayDto? Replay { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
        decodeCts?.Dispose();
    }
}

public class DecodeInfoEventArgs : EventArgs
{
    public int Done { get; set; }
    public int Successful { get; set; }
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
