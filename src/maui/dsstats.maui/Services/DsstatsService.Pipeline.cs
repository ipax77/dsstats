using CommunityToolkit.Maui.Alerts;
using dsstats.db;
using dsstats.dbServices;
using dsstats.maui.Services.Models;
using dsstats.parser;
using dsstats.shared;
using System.Threading.Channels;

namespace dsstats.maui.Services;

public sealed partial class DsstatsService
{
    private const int ImportBatchSize = 25;
    private const int DecodeBacklogCapacity = 4;
    private const int MaxDecodeParallelism = 4;

    private async Task DecodeAndImportAsync(
        DecodeStatus decodeStatus,
    IProgress<ImportProgress> progress,
    CancellationToken cancellationToken)
    {
        // Fast-fail if already running
        if (!await _runGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Import already in progress.");

        try
        {
            var config = await GetConfig().ConfigureAwait(false);
            await DecodeAndImportCoreAsync(config, decodeStatus, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _runGate.Release();
        }
    }

    private async Task DecodeAndImportCoreAsync(
        MauiConfig config,
        DecodeStatus decodeStatus,
        IProgress<ImportProgress> progress,
        CancellationToken cancellationToken)
    {
        ResetCounters();

        _total = decodeStatus.ToDoReplayPaths.Count;
        if (decodeStatus.ToDoReplayPaths.Count == 0)
            return;

        await RunPipelineAsync(
            config,
            decodeStatus.ToDoReplayPaths,
            progress,
            cancellationToken).ConfigureAwait(false);
    }

    #region Pipeline Orchestration

    private async Task RunPipelineAsync(
        MauiConfig config,
        List<string> replayPaths,
        IProgress<ImportProgress> progress,
        CancellationToken ct)
    {
        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var channel = Channel.CreateBounded<ReplayDto>(
            new BoundedChannelOptions(DecodeBacklogCapacity)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        var discovered = replayPaths.Count;

        var progressTask = ReportProgressAsync(progress, discovered, progressCts.Token);
        var decodeTask = DecodeToChannelAsync(
            config,
            replayPaths,
            channel.Writer,
            ct);

        var importTask = ImportFromChannelAsync(
            channel.Reader,
            ct);

        try
        {
            await Task.WhenAll(decodeTask, importTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation / app close
        }
        finally
        {
            progressCts.Cancel();
            channel.Writer.TryComplete();
            try
            {
                await progressTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            progress.Report(new ImportProgress(
                _total,
                discovered,
                _decoded,
                _imported,
                _errors,
                _uploadStatus,
                DateTime.UtcNow - _start,
                ImportStatus.Running,
                "Import finished"));

            await MainThread
                .InvokeOnMainThreadAsync(() => Toast.Make("Replay decoding finished.").Show(ct))
                .ConfigureAwait(false);
        }
    }

    #endregion

    #region Decode Stage (Producer)

    private async Task DecodeToChannelAsync(
        MauiConfig config,
        List<string> replayPaths,
        ChannelWriter<ReplayDto> writer,
        CancellationToken ct)
    {
        try
        {
            var uploaders = config.GetToonIdDtos();
            await Parallel.ForEachAsync(
                replayPaths,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = GetDecodeDegreeOfParallelism(config),
                    CancellationToken = ct
                },
                async (replayPath, token) =>
                {
                    var result = await DecodeReplayDtoAsync(replayPath, uploaders, token).ConfigureAwait(false);
                    if (result.Replay is null)
                    {
                        Interlocked.Increment(ref _errors);
                        _replayErrors.Add(new(result.Error ?? "failed decoding", replayPath));
                        return;
                    }

                    await writer.WriteAsync(result.Replay, token).ConfigureAwait(false);
                    Interlocked.Increment(ref _decoded);
                }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _errors);
            _replayErrors.Add(new(ex.Message, "process"));
            throw;
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task<DecodeReplayResult> DecodeReplayDtoAsync(
        string replayPath,
        List<ToonIdDto> uploaders,
        CancellationToken ct)
    {
        try
        {
            var sc2Replay = await _replayDecoder.DecodeAsync(replayPath, _decoderOptions, ct).ConfigureAwait(false);
            if (sc2Replay is null)
            {
                return new(null, "failed decoding");
            }

            var dto = DsstatsParser.ParseReplay(sc2Replay);
            if (dto is null)
            {
                return new(null, "failed parsing");
            }

            dto.FileName = replayPath;
            dto.SetUploader(uploaders);
            return new(dto, null);
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

    #endregion

    #region Import Stage (Consumer)

    private async Task ImportFromChannelAsync(
        ChannelReader<ReplayDto> reader,
        CancellationToken ct)
    {
        var batch = new List<ReplayDto>(ImportBatchSize);

        try
        {
            await foreach (var replay in reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                batch.Add(replay);

                if (batch.Count >= ImportBatchSize)
                {
                    await ImportBatchAsync(batch, ct).ConfigureAwait(false);
                    Interlocked.Add(ref _imported, batch.Count);
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
                await ImportBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
                Interlocked.Add(ref _imported, batch.Count);
            }
        }
    }

    private async Task ImportBatchAsync(
        List<ReplayDto> batch,
        CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.InsertReplays(batch).ConfigureAwait(false);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private static int GetDecodeDegreeOfParallelism(MauiConfig config)
    {
        var configuredCores = config.CPUCores <= 0
            ? Environment.ProcessorCount
            : config.CPUCores;

        var coreLimit = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, MaxDecodeParallelism));
        return Math.Max(1, Math.Min(configuredCores, coreLimit));
    }

    #endregion

    private sealed record DecodeReplayResult(ReplayDto? Replay, string? Error);
}
