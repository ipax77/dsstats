
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Channels;
using dsstats.indexedDb.Services;
using dsstats.shared;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    private readonly Lock lastReplayLock = new();

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
        _currentWorkerCount = config.CPUCores;
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = scopeFactory.CreateAsyncScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        replaysDecoded = 0;
        int failedCount = 0;
        int processedCount = 0;
        int totalFiles = 0;

        try
        {
            await EnsureWorkersAsync(config.CPUCores);

            var readChannel = Channel.CreateBounded<ReadItem>(new BoundedChannelOptions(8)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var decodeChannel = Channel.CreateBounded<DecodedItem>(new BoundedChannelOptions(config.CPUCores * 4)
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

                        var ms = new MemoryStream();
                        await stream.CopyToAsync(ms, decodeCts.Token);

                        await readChannel.Writer.WriteAsync(
                            new ReadItem(file.Path, file.Size, file.LastModified, ms.ToArray()),
                            decodeCts.Token
                        );
                    }
                }
                finally
                {
                    readChannel.Writer.Complete();
                }
            });

            var decodeWorkers = Enumerable.Range(0, config.CPUCores).Select(_ => Task.Run(async () =>
            {
                await foreach (var item in readChannel.Reader.ReadAllAsync(decodeCts.Token))
                {
                    try
                    {
                        var (success, error, hash, replay) =
                            await _decodeClient!.DecodeAsync(item.Data, decodeCts.Token);

                        await decodeChannel.Writer.WriteAsync(
                            new DecodedItem(
                                item.Path,
                                item.Size,
                                item.LastModified,
                                success && replay != null,
                                error,
                                hash,
                                replay
                            ),
                            decodeCts.Token
                        );
                    }
                    catch (Exception ex)
                    {
                        await decodeChannel.Writer.WriteAsync(
                            new DecodedItem(item.Path, item.Size, item.LastModified, false, ex.Message, null, null),
                            decodeCts.Token
                        );
                    }
                }
            }));

            var decodeCompletion = Task.WhenAll(decodeWorkers).ContinueWith(_ =>
            {
                decodeChannel.Writer.Complete();
            });



            var writerTask = Task.Run(async () =>
            {
                await foreach (var item in decodeChannel.Reader.ReadAllAsync(decodeCts.Token))
                {
                    if (item.Success && item.Replay != null)
                    {
                        item.Replay.FileName = item.Path;

                        await dbService.UpsertReplayAsync(item.Hash!, item.Replay, item.Size, item.LastModified);

                        Interlocked.Increment(ref replaysDecoded);

                        SetLatestReplay(item);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                        logger.LogWarning("failed decoding replay: {error}", item.Error);
                    }

                    Interlocked.Increment(ref processedCount);
                }
            });

            var progressCts = CancellationTokenSource.CreateLinkedTokenSource(decodeCts.Token);

            Task? progressTask = null;
            try
            {
                progressTask = Task.Run(async () =>
                {
                    while (!decodeCts.Token.IsCancellationRequested)
                    {
                        var processed = Volatile.Read(ref processedCount);
                        var decoded = Volatile.Read(ref replaysDecoded);

                        var interval = processed < 20
                            ? TimeSpan.FromMilliseconds(200)
                            : TimeSpan.FromMilliseconds(700);

                        await Task.Delay(interval, decodeCts.Token);

                        var total = aggregateState?.Total ?? totalFiles;
                        var done = (aggregateState?.Processed ?? 0) + processed;
                        var successful = (aggregateState?.Successful ?? 0) + decoded;
                        var errors = (aggregateState?.Error ?? 0) + Volatile.Read(ref failedCount);

                        OnDecodeStateChanged(new DecodeInfoEventArgs
                        {
                            Done = done,
                            Successful = successful,
                            Total = total,
                            Error = errors,
                            Elapsed = sw.Elapsed,
                            Eta = TimeSpan.FromTicks(
                                sw.Elapsed.Ticks * (totalFiles - processed) / Math.Max(processed, 1)
                            ),
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
            await Task.WhenAll(decodeWorkers);
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

        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = finalDone,
            Successful = finalSuccessful,
            Total = finalTotal,
            Error = finalErrors,
            Elapsed = sw.Elapsed,
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
}

record ReadItem(string Path, long Size, long LastModified, byte[] Data);
record DecodedItem(string Path, long Size, long LastModified, bool Success, string? Error, string? Hash, ReplayDto? Replay);
