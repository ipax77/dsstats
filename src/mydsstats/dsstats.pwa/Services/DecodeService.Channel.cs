
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;
using System.Collections.Concurrent;
using dsstats.indexedDb.Services;
using dsstats.shared;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    public const long MaxReplayFileSize = 5L * 1024 * 1024;

    private readonly Lock lastReplayLock = new();
    private static readonly TimeSpan DecodeIdleThreshold = TimeSpan.FromSeconds(30);
    private const int MaxErrorSamples = 3;

    [SupportedOSPlatform("browser")]
    public async Task DecodeFromDirectory(string? dirKey = null, int limit = 100)
        => await DecodeFromDirectory(dirKey, limit, null);

    [SupportedOSPlatform("browser")]
    private async Task DecodeFromDirectory(string? dirKey, int limit, DecodeAggregateState? aggregateState)
    {
        await ss.WaitAsync();

        Decoding = true;
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        decodeCts = runCts;
        var token = runCts.Token;

        var config = await pwaConfigService.GetConfig();
        var workerCount = PwaConfig.NormalizeCpuCores(config.CPUCores);
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        replaysDecoded = 0;
        int failedCount = 0;
        int processedCount = 0;
        int totalFiles = 0;
        bool cancelled = false;
        bool pipelineFailed = false;
        string? pipelineError = null;
        var errorSamples = new ConcurrentQueue<string>();
        var ignoredReplayPaths = new HashSet<string>(StringComparer.Ordinal);
        var progressClock = new DecodeProgressClock(sw, DecodeIdleThreshold);

        try
        {
            await EnsureWorkersAsync(workerCount);
            _decodeClient!.ConsumeBrowserPause();

            var readChannel = Channel.CreateBounded<ReadItem>(new BoundedChannelOptions(workerCount)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var decodeChannel = Channel.CreateBounded<DecodedItem>(new BoundedChannelOptions(workerCount)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var fileInfos = await dbService.PickDirectoryInit(
                config.ReplayStartName,
                dirKey,
                limit,
                config.IgnoreReplays);
            totalFiles = fileInfos.Count;
            if (aggregateState is not null)
            {
                aggregateState.Total += totalFiles;
            }

            async Task ReadFilesAsync()
            {
                try
                {
                    foreach (var file in fileInfos)
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            if (file.Size > MaxReplayFileSize)
                            {
                                throw new InvalidDataException(
                                    $"Replay is {FormatFileSize(file.Size)}, exceeding the supported limit of {FormatFileSize(MaxReplayFileSize)}.");
                            }

                            await using var streamRef = await dbService.GetFileContent(file.Path);
                            await using var stream = await streamRef.OpenReadStreamAsync(MaxReplayFileSize, token);
                            var data = await ReadReplayBytesAsync(stream, file.Size, token);

                            await readChannel.Writer.WriteAsync(
                                new ReadItem(file.Path, file.Size, file.LastModified, data),
                                token);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed reading replay {Path}; continuing with the remaining files.", file.Path);
                            await decodeChannel.Writer.WriteAsync(
                                DecodedItem.Failed(file.Path, file.Size, file.LastModified, $"Read failed: {ex.Message}", shouldIgnore: true),
                                token);
                        }
                    }
                }
                finally
                {
                    readChannel.Writer.TryComplete();
                }
            }

            async Task DecodeFilesAsync()
            {
                await foreach (var item in readChannel.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        var (success, error, hash, replay, spawnPlayback, spawnPlaybackError) =
                            await _decodeClient!.DecodeAsync(item.Data, token);

                        await TryWriteDecodedItemAsync(
                            decodeChannel,
                            token,
                            new DecodedItem(
                                item.Path,
                                item.Size,
                                item.LastModified,
                                success && replay != null,
                                error,
                                hash,
                                replay,
                                spawnPlayback,
                                spawnPlaybackError,
                                ShouldIgnore: !success || replay is null
                            )
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await TryWriteDecodedItemAsync(
                            decodeChannel,
                            token,
                            DecodedItem.Failed(item.Path, item.Size, item.LastModified, $"Decode failed: {ex.Message}", shouldIgnore: true)
                        );
                    }
                }
            }

            async Task CompleteDecodeChannelAsync(Task[] decodeTasks)
            {
                try
                {
                    await Task.WhenAll(decodeTasks);
                }
                finally
                {
                    decodeChannel.Writer.TryComplete();
                }
            }



            async Task SaveDecodedFilesAsync()
            {
                await foreach (var item in decodeChannel.Reader.ReadAllAsync(token))
                {
                    try
                    {
                        if (item.Success && item.Replay != null)
                        {
                            item.Replay.FileName = item.Path;

                            if (item.SpawnPlayback is null)
                            {
                                logger.LogWarning(
                                    "Replay {Path} decoded without spawn playback sidecar. Reason: {Reason}",
                                    item.Path,
                                    item.SpawnPlaybackError ?? "worker returned no sidecar payload");
                            }
                            else
                            {
                                logger.LogDebug(
                                    "Replay {Path} decoded with spawn playback sidecar. Hash: {Hash}, CompressedLength: {CompressedLength}, UncompressedLength: {UncompressedLength}, UnitCount: {UnitCount}",
                                    item.Path,
                                    item.Hash,
                                    item.SpawnPlayback.CompressedLength,
                                    item.SpawnPlayback.UncompressedLength,
                                    item.SpawnPlayback.UnitCount);
                            }

                            await dbService.UpsertReplayAsync(item.Hash!, item.Replay, item.Size, item.LastModified, item.SpawnPlayback);
                            Interlocked.Increment(ref replaysDecoded);
                            SetLatestReplay(item);
                        }
                        else
                        {
                            RecordReplayFailure(item, item.Error ?? "Unknown decode error");
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed saving replay {Path}; continuing with the remaining files.", item.Path);
                        RecordReplayFailure(item with { ShouldIgnore = false }, $"Save failed: {ex.Message}");
                    }
                    finally
                    {
                        var processed = Interlocked.Increment(ref processedCount);
                        RecordBrowserPause(progressClock);
                        progressClock.RecordProgress(processed);
                    }
                }
            }

            void RecordReplayFailure(DecodedItem item, string error)
            {
                Interlocked.Increment(ref failedCount);
                var detail = $"{GetReplayFileName(item.Path)}: {error}";
                if (errorSamples.Count < MaxErrorSamples)
                {
                    errorSamples.Enqueue(detail);
                }
                if (item.ShouldIgnore)
                {
                    ignoredReplayPaths.Add(item.Path);
                }
                logger.LogWarning("Failed processing replay {Path}: {Error}", item.Path, error);
            }

            async Task ReportProgressAsync()
            {
                try
                {
                    var wasIdle = false;
                    while (!token.IsCancellationRequested)
                    {
                        var processed = Volatile.Read(ref processedCount);
                        var decoded = Volatile.Read(ref replaysDecoded);

                        var interval = wasIdle
                            ? TimeSpan.FromSeconds(2)
                            : processed < 20
                            ? TimeSpan.FromMilliseconds(200)
                            : TimeSpan.FromMilliseconds(700);

                        await Task.Delay(interval, token);
                        RecordBrowserPause(progressClock);

                        var total = aggregateState?.Total ?? totalFiles;
                        var done = (aggregateState?.Processed ?? 0) + processed;
                        var successful = (aggregateState?.Successful ?? 0) + decoded;
                        var errors = (aggregateState?.Error ?? 0) + Volatile.Read(ref failedCount);
                        total = Math.Max(total, done);
                        var progress = progressClock.GetSnapshot(totalFiles);
                        wasIdle = progress.IsIdle;

                        OnDecodeStateChanged(new DecodeInfoEventArgs
                        {
                            Done = done,
                            Successful = successful,
                            Total = total,
                            Error = errors,
                            Elapsed = progress.ActiveElapsed,
                            IdleTime = progress.IdleTime,
                            TotalIdleTime = progress.TotalIdleTime,
                            IsIdle = progress.IsIdle,
                            Eta = CalculateEta(progress.ActiveElapsed, processed, totalFiles),
                            Saving = false,
                            Finished = false,
                            Info = BuildProgressInfo(successful, errors, errorSamples)
                        });

                        if (processed >= totalFiles)
                            break;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                }
            }

            async Task RunPipelineStageAsync(Func<Task> stage)
            {
                try
                {
                    await stage();
                }
                catch
                {
                    runCts.Cancel();
                    throw;
                }
            }

            var readerTask = RunPipelineStageAsync(ReadFilesAsync);
            var decodeWorkers = Enumerable.Range(0, workerCount)
                .Select(_ => RunPipelineStageAsync(DecodeFilesAsync))
                .ToArray();
            var decodeCompletion = CompleteDecodeChannelAsync(decodeWorkers);
            var writerTask = RunPipelineStageAsync(SaveDecodedFilesAsync);
            var progressTask = ReportProgressAsync();

            await Task.WhenAll(readerTask, decodeCompletion, writerTask);
            await progressTask;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            cancelled = true;
            logger.LogInformation(
                "Replay directory decode was cancelled after processing {ProcessedCount} of {TotalFiles} files.",
                processedCount,
                totalFiles);
        }
        catch (Exception ex)
        {
            pipelineFailed = true;
            pipelineError = ex.Message;
            runCts.Cancel();
            logger.LogError(
                ex,
                "Failed to decode replays from directory after processing {ProcessedCount} of {TotalFiles} files.",
                processedCount,
                totalFiles);
        }
        finally
        {
            Decoding = false;
            if (token.IsCancellationRequested || pipelineFailed)
                await TeardownWorkersAsync();
            if (ReferenceEquals(decodeCts, runCts))
                decodeCts = null;
            ss.Release();
        }
        sw.Stop();
        logger.LogInformation("Decoding completed. Decoded {DecodedCount} replays in {Elapsed} min.",
         replaysDecoded, sw.Elapsed.TotalMinutes.ToString("N2"));

        if (ignoredReplayPaths.Count > 0)
        {
            var knownIgnoredReplays = config.IgnoreReplays.ToHashSet(StringComparer.Ordinal);
            var changed = false;
            foreach (var ignoredReplayPath in ignoredReplayPaths)
            {
                changed |= knownIgnoredReplays.Add(ignoredReplayPath);
            }
            if (changed)
            {
                config.IgnoreReplays = knownIgnoredReplays.Order(StringComparer.Ordinal).ToList();
                await pwaConfigService.SaveConfig(config, showNotification: false);
            }
        }

        var finalDone = (aggregateState?.Processed ?? 0) + processedCount;
        var finalSuccessful = (aggregateState?.Successful ?? 0) + replaysDecoded;
        var finalErrors = (aggregateState?.Error ?? 0) + failedCount;
        var finalTotal = aggregateState?.Total ?? totalFiles;
        finalTotal = Math.Max(finalTotal, finalDone);
        RecordBrowserPause(progressClock);
        var finalProgress = progressClock.GetSnapshot(totalFiles);
        var finalInfo = BuildFinalInfo(
            finalSuccessful,
            finalErrors,
            finalDone,
            finalTotal,
            cancelled,
            pipelineFailed,
            pipelineError,
            errorSamples);

        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = finalDone,
            Successful = finalSuccessful,
            Total = finalTotal,
            Error = finalErrors,
            Elapsed = finalProgress.ActiveElapsed,
            IdleTime = TimeSpan.Zero,
            TotalIdleTime = finalProgress.TotalIdleTime,
            IsIdle = false,
            Eta = TimeSpan.Zero,
            Saving = false,
            Finished = true,
            Cancelled = cancelled,
            Failed = pipelineFailed,
            Info = finalInfo
        });

        if (aggregateState is not null)
        {
            aggregateState.Processed = finalDone;
            aggregateState.Successful = finalSuccessful;
            aggregateState.Error = finalErrors;
            aggregateState.Cancelled |= cancelled;
            aggregateState.Failed |= pipelineFailed;
            aggregateState.FailureInfo ??= pipelineError;
        }

        var completedPipeline = !cancelled && !pipelineFailed && processedCount >= totalFiles;
        if (completedPipeline && config.UploadCredential)
        {
            await Upload10(dbService);
        }
        else if (completedPipeline)
        {
            _decodeCompletionsWithoutUpload++;
            if (_decodeCompletionsWithoutUpload == 1 || _decodeCompletionsWithoutUpload % 10 == 0)
                OnPromptForUpload();
        }
    }

    private static async Task<byte[]> ReadReplayBytesAsync(Stream stream, long fileSize, CancellationToken token)
    {
        if (fileSize is > 0 and <= MaxReplayFileSize)
        {
            var buffer = new byte[(int)fileSize];
            var offset = 0;

            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset), token);
                if (read == 0)
                    break;

                offset += read;
            }

            if (offset != buffer.Length)
                Array.Resize(ref buffer, offset);

            return buffer;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, token);
        return ms.ToArray();
    }

    private static string BuildProgressInfo(int successful, int errors, ConcurrentQueue<string> errorSamples)
    {
        var info = $"Decoded: {successful}, Failed: {errors}";
        return errors > 0 && errorSamples.TryPeek(out var sample)
            ? $"{info}. First error: {sample}"
            : info;
    }

    private static string BuildFinalInfo(
        int successful,
        int errors,
        int done,
        int total,
        bool cancelled,
        bool pipelineFailed,
        string? pipelineError,
        ConcurrentQueue<string> errorSamples)
    {
        if (pipelineFailed)
        {
            return $"Decode stopped unexpectedly after {done} of {total} replays: {pipelineError ?? "Unknown pipeline error"}";
        }

        if (cancelled)
        {
            return $"Decode cancelled after {done} of {total} replays.";
        }

        var info = $"Decoded: {successful}, Failed: {errors}";
        return errors > 0 && errorSamples.TryPeek(out var sample)
            ? $"{info}. First error: {sample}"
            : info;
    }

    internal static string GetReplayFileName(string path)
    {
        var separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return separator >= 0 && separator < path.Length - 1 ? path[(separator + 1)..] : path;
    }

    private static string FormatFileSize(long bytes)
        => $"{bytes / (1024d * 1024d):N1} MB";

    private static TimeSpan CalculateEta(TimeSpan activeElapsed, int processed, int total)
    {
        if (processed <= 0 || processed >= total)
            return TimeSpan.Zero;

        return TimeSpan.FromTicks(
            activeElapsed.Ticks * (total - processed) / processed
        );
    }

    [SupportedOSPlatform("browser")]
    private void RecordBrowserPause(DecodeProgressClock progressClock)
    {
        var browserPause = _decodeClient?.ConsumeBrowserPause() ?? TimeSpan.Zero;
        if (browserPause > TimeSpan.Zero)
        {
            progressClock.RecordBrowserPause(browserPause);
        }
    }

    private void SetLatestReplay(DecodedItem item)
    {
        if (item.Replay is null) return;
        lock (lastReplayLock)
        {
            if (LatestReplay is null)
            {
                LatestReplay = item.Replay;
                LatestReplayHash = item.Hash;
            }
            else if (item.Replay.Gametime > LatestReplay.Gametime)
            {
                LatestReplay = item.Replay;
                LatestReplayHash = item.Hash;
            }
        }
    }

    private sealed class DecodeAggregateState
    {
        public int Processed { get; set; }
        public int Successful { get; set; }
        public int Error { get; set; }
        public int Total { get; set; }
        public bool Cancelled { get; set; }
        public bool Failed { get; set; }
        public string? FailureInfo { get; set; }
    }

    private sealed class DecodeProgressClock
    {
        private readonly Stopwatch stopwatch;
        private readonly TimeSpan idleThreshold;
        private readonly Lock gate = new();
        private int lastProcessed;
        private TimeSpan lastProgressAt;
        private TimeSpan accumulatedIdle;
        private bool isIdle;

        public DecodeProgressClock(Stopwatch stopwatch, TimeSpan idleThreshold)
        {
            this.stopwatch = stopwatch;
            this.idleThreshold = idleThreshold;
            lastProgressAt = stopwatch.Elapsed;
        }

        public void RecordProgress(int processed)
        {
            lock (gate)
            {
                if (processed == lastProcessed)
                {
                    return;
                }

                var now = stopwatch.Elapsed;
                var stalledFor = now - lastProgressAt;
                if (stalledFor >= idleThreshold)
                {
                    accumulatedIdle += stalledFor;
                }

                lastProcessed = processed;
                lastProgressAt = now;
                isIdle = false;
            }
        }

        public void RecordBrowserPause(TimeSpan pause)
        {
            if (pause <= TimeSpan.Zero)
            {
                return;
            }

            lock (gate)
            {
                accumulatedIdle += pause;
                var adjustedLastProgressAt = lastProgressAt + pause;
                lastProgressAt = adjustedLastProgressAt > stopwatch.Elapsed
                    ? stopwatch.Elapsed
                    : adjustedLastProgressAt;
            }
        }

        public DecodeProgressSnapshot GetSnapshot(int total)
        {
            lock (gate)
            {
                var now = stopwatch.Elapsed;
                var idleTime = TimeSpan.Zero;
                if (lastProcessed < total)
                {
                    var stalledFor = now - lastProgressAt;
                    if (stalledFor >= idleThreshold)
                    {
                        isIdle = true;
                        idleTime = stalledFor;
                    }
                    else
                    {
                        isIdle = false;
                    }
                }

                var totalIdleTime = accumulatedIdle + idleTime;
                var activeElapsed = now - totalIdleTime;
                if (activeElapsed < TimeSpan.Zero)
                {
                    activeElapsed = TimeSpan.Zero;
                }

                return new DecodeProgressSnapshot(activeElapsed, idleTime, totalIdleTime, isIdle);
            }
        }
    }

    private readonly record struct DecodeProgressSnapshot(
        TimeSpan ActiveElapsed,
        TimeSpan IdleTime,
        TimeSpan TotalIdleTime,
        bool IsIdle);

    private async Task TryWriteDecodedItemAsync(
        Channel<DecodedItem> decodeChannel,
        CancellationToken token,
        DecodedItem item)
    {
        try
        {
            await decodeChannel.Writer.WriteAsync(item, token);
        }
        catch (ChannelClosedException)
        {
            logger.LogDebug("Decode result channel was already closed for {Path}", item.Path);
        }
    }
}

record ReadItem(string Path, long Size, long LastModified, byte[] Data);
record DecodedItem(
    string Path,
    long Size,
    long LastModified,
    bool Success,
    string? Error,
    string? Hash,
    ReplayDto? Replay,
    SpawnPlaybackEncodedSidecar? SpawnPlayback,
    string? SpawnPlaybackError,
    bool ShouldIgnore = false)
{
    public static DecodedItem Failed(
        string path,
        long size,
        long lastModified,
        string error,
        bool shouldIgnore)
        => new(path, size, lastModified, false, error, null, null, null, null, shouldIgnore);
}
