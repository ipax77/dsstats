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
    private async Task DecodeAndImportAsync(
        DecodeStatus decodeStatus,
    IProgress<ImportProgress> progress,
    CancellationToken cancellationToken)
    {
        // Fast-fail if already running
        if (!await _runGate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Import already in progress.");

        try
        {
            var config = await GetConfig();
            await DecodeAndImportCoreAsync(config, decodeStatus, progress, cancellationToken);
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
            cancellationToken);
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
            new BoundedChannelOptions(500)
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
            await Task.WhenAll(decodeTask, importTask);
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
                await progressTask;
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
                ImportStatus.Completed,
                "Import finished"));

            await Toast.Make("Replay decoding finished.").Show(ct);
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
            await foreach (var result in _replayDecoder
                .DecodeParallelWithErrorReport(
                    replayPaths,
                    config.CPUCores,
                    _decoderOptions,
                    ct))
            {
                if (ct.IsCancellationRequested)
                    break;

                if (result.Sc2Replay is null)
                {
                    Interlocked.Increment(ref _errors);
                    _replayErrors.Add(new(result.Exception ?? "failed decoding", result.ReplayPath));
                    continue;
                }

                var dto = DsstatsParser.ParseReplay(result.Sc2Replay);
                if (dto is null)
                {
                    Interlocked.Increment(ref _errors);
                    _replayErrors.Add(new("failed parsing", result.ReplayPath));
                    continue;
                }

                dto.FileName = result.ReplayPath;
                dto.SetUploader(config.GetToonIdDtos());

                await writer.WriteAsync(dto, ct);
                Interlocked.Increment(ref _decoded);
            }
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

    #endregion

    #region Import Stage (Consumer)

    private async Task ImportFromChannelAsync(
        ChannelReader<ReplayDto> reader,
        CancellationToken ct)
    {
        var batch = new List<ReplayDto>(100);

        try
        {
            await foreach (var replay in reader.ReadAllAsync(ct))
            {
                batch.Add(replay);

                if (batch.Count >= 100)
                {
                    await ImportBatchAsync(batch, ct);
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
                await ImportBatchAsync(batch, CancellationToken.None);
                Interlocked.Add(ref _imported, batch.Count);
            }
        }
    }

    private async Task ImportBatchAsync(
        List<ReplayDto> batch,
        CancellationToken ct)
    {
        await _dbSemaphore.WaitAsync(ct);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.InsertReplays(batch);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    #endregion
}
