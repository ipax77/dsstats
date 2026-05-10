using dsstats.maui.Services.Models;
using s2protocol.NET;

namespace dsstats.maui.Services;

public sealed partial class DsstatsService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory) : IDisposable
{
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

    #region Public API

    public List<ReplayError> GetReplayErrors()
    {
        return _replayErrors.ToList();
    }

    public async Task StartImportAsync(
        ImportState importState, DecodeStatus? decodeStatus = null)
    {
        if (importState.IsRunning)
            throw new InvalidOperationException("Import already running.");

        importState.SetRunning(true);
        importState.Start();
        importState.UpdateProgress(
            new ImportProgress(0, 0, 0, 0, 0, UploadStatus.None, TimeSpan.Zero, ImportStatus.Running));

        var progress = new Progress<ImportProgress>(importState.UpdateProgress);

        var importToken = importState.Token;

        try
        {
            await Task.Run(async () =>
            {
                var status = decodeStatus ?? await GetDecodeStatus(importToken).ConfigureAwait(false);
                await DecodeAndImportAsync(status, progress, importToken).ConfigureAwait(false);
                await StartUpload(importState, false, importToken).ConfigureAwait(false);
            }, importToken).ConfigureAwait(false);
        }
        finally
        {
            var final = importState.Progress;

            var summary =
                $"{final.Decoded:N0} replays decoded " +
                $"in {FormatDuration(final.Elapsed)}";

            importState.UpdateProgress(final with
            {
                Message = summary
            });
            importState.SetRunning(false);
            importState.Complete();
            var status = await GetDecodeStatus(default, true).ConfigureAwait(false);
            importState.SetDecodeStatus(status);
        }
    }

    private static string FormatDuration(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return elapsed.ToString(@"h\:mm\:ss");

        if (elapsed.TotalMinutes >= 1)
            return elapsed.ToString(@"m\:ss");

        return $"{elapsed.TotalSeconds:N0}s";
    }

    #endregion

    public void Dispose()
    {
        WatchService?.Dispose();
    }
}
