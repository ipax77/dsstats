
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
    public async Task DecodeFromDirectory(int regionId, string? dirKey = null, int limit = 100)
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

            if (regionId == 0 && dirKey != null)
            {
                // Extract last character from dirKey and parse as integer
                if (dirKey.Length > 0 && char.IsDigit(dirKey[^1]))
                {
                    regionId = int.Parse(dirKey[^1].ToString());
                }
            }

            var fileInfos = await dbService.PickDirectoryInit(regionId, config.ReplayStartName, dirKey, limit);

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
                            new ReadItem(file.Path, ms.ToArray()),
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
                            new DecodedItem(item.Path, false, ex.Message, null, null),
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

                        await dbService.UpsertReplayAsync(item.Hash!, item.Replay);

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

                        OnDecodeStateChanged(new DecodeInfoEventArgs
                        {
                            Done = decoded,
                            Total = fileInfos.Count,
                            Error = Volatile.Read(ref failedCount),
                            Elapsed = sw.Elapsed,
                            Eta = TimeSpan.FromTicks(
                                sw.Elapsed.Ticks * (fileInfos.Count - processed) / Math.Max(processed, 1)
                            ),
                            Saving = false,
                            Finished = false,
                            Info = $"Decoded: {decoded}, Failed: {failedCount}"
                        });

                        if (processed >= fileInfos.Count)
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
        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            Done = replaysDecoded,
            Total = replaysDecoded,
            Error = failedCount,
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
}

record ReadItem(string Path, byte[] Data);
record DecodedItem(string Path, bool Success, string? Error, string? Hash, ReplayDto? Replay);