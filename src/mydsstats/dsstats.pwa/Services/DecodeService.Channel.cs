
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;
using dsstats.indexedDb.Services;
using dsstats.shared;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    private readonly Lock lastReplayLock = new();
    private static readonly TimeSpan DecodeIdleThreshold = TimeSpan.FromSeconds(30);

    [SupportedOSPlatform("browser")]
    public async Task DecodeFromDirectory(string? dirKey = null, int limit = 100)
        => await DecodeFromDirectory(dirKey, limit, null);

    [SupportedOSPlatform("browser")]
    private async Task DecodeFromDirectory(string? dirKey, int limit, DecodeAggregateState? aggregateState)
    {
        await ss.WaitAsync();

        Decoding = true;
        decodeCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

        var config = await pwaConfigService.GetConfig();
        var workerCount = PwaConfig.NormalizeCpuCores(config.CPUCores);
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        replaysDecoded = 0;
        int failedCount = 0;
        int processedCount = 0;
        int totalFiles = 0;
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

            var fileInfos = await dbService.PickDirectoryInit(config.ReplayStartName, dirKey, limit);
            totalFiles = fileInfos.Count;
            if (aggregateState is not null)
            {
                aggregateState.Total += totalFiles;
            }

            var readerTask = Task.Run(async () =>
            {
                try
                {
                    foreach (var file in fileInfos)
                    {
                        decodeCts.Token.ThrowIfCancellationRequested();

                        var streamRef = await dbService.GetFileContent(file.Path);
                        using var stream = await streamRef.OpenReadStreamAsync(5_000_000, decodeCts.Token);
                        var data = await ReadReplayBytesAsync(stream, file.Size, decodeCts.Token);

                        await readChannel.Writer.WriteAsync(
                            new ReadItem(file.Path, file.Size, file.LastModified, data),
                            decodeCts.Token
                        );
                    }
                }
                finally
                {
                    readChannel.Writer.Complete();
                }
            });

            var decodeWorkers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
            {
                await foreach (var item in readChannel.Reader.ReadAllAsync(decodeCts.Token))
                {
                    try
                    {
                        var (success, error, hash, replay, spawnPlayback) =
                            await _decodeClient!.DecodeAsync(item.Data, decodeCts.Token);

                        await TryWriteDecodedItemAsync(
                            decodeChannel,
                            decodeCts.Token,
                            new DecodedItem(
                                item.Path,
                                item.Size,
                                item.LastModified,
                                success && replay != null,
                                error,
                                hash,
                                replay,
                                spawnPlayback
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
                            decodeCts.Token,
                            new DecodedItem(item.Path, item.Size, item.LastModified, false, ex.Message, null, null, null)
                        );
                    }
                }
            }));

            var decodeCompletion = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(decodeWorkers);
                }
                finally
                {
                    decodeChannel.Writer.TryComplete();
                }
            });



            var writerTask = Task.Run(async () =>
            {
                await foreach (var item in decodeChannel.Reader.ReadAllAsync(decodeCts.Token))
                {
                    if (item.Success && item.Replay != null)
                    {
                        item.Replay.FileName = item.Path;

                        await dbService.UpsertReplayAsync(item.Hash!, item.Replay, item.Size, item.LastModified, item.SpawnPlayback);

                        Interlocked.Increment(ref replaysDecoded);

                        SetLatestReplay(item);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                        logger.LogWarning("failed decoding replay: {error}", item.Error);
                    }

                    var processed = Interlocked.Increment(ref processedCount);
                    RecordBrowserPause(progressClock);
                    progressClock.RecordProgress(processed);
                }
            });

            Task? progressTask = null;
            try
            {
                progressTask = Task.Run(async () =>
                {
                    var wasIdle = false;
                    while (!decodeCts.Token.IsCancellationRequested)
                    {
                        var processed = Volatile.Read(ref processedCount);
                        var decoded = Volatile.Read(ref replaysDecoded);

                        var interval = wasIdle
                            ? TimeSpan.FromSeconds(2)
                            : processed < 20
                            ? TimeSpan.FromMilliseconds(200)
                            : TimeSpan.FromMilliseconds(700);

                        await Task.Delay(interval, decodeCts.Token);
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
                            Info = $"Decoded: {successful}, Failed: {errors}"
                        });

                        if (processed >= totalFiles)
                            break;
                    }
                });
            }
            catch (OperationCanceledException) { }

            await readerTask;
            await decodeCompletion;
            await writerTask;
            if (progressTask is not null)
                await progressTask;
        }
        catch (OperationCanceledException) { }
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

        var finalDone = (aggregateState?.Processed ?? 0) + processedCount;
        var finalSuccessful = (aggregateState?.Successful ?? 0) + replaysDecoded;
        var finalErrors = (aggregateState?.Error ?? 0) + failedCount;
        var finalTotal = aggregateState?.Total ?? totalFiles;
        finalTotal = Math.Max(finalTotal, finalDone);
        RecordBrowserPause(progressClock);
        var finalProgress = progressClock.GetSnapshot(totalFiles);

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
            Info = $"Decoded: {finalSuccessful}"
        });

        if (aggregateState is not null)
        {
            aggregateState.Processed = finalDone;
            aggregateState.Successful = finalSuccessful;
            aggregateState.Error = finalErrors;
        }

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

    private static async Task<byte[]> ReadReplayBytesAsync(Stream stream, long fileSize, CancellationToken token)
    {
        if (fileSize is > 0 and <= 5_000_000)
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
    SpawnPlaybackEncodedSidecar? SpawnPlayback);
