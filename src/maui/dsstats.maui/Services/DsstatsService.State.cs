using dsstats.maui.Services.Models;
using System.Collections.Concurrent;

namespace dsstats.maui.Services;

public sealed partial class DsstatsService
{
    // SQLite single-writer enforcement
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    public SemaphoreSlim DbSemaphore => _dbSemaphore;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    // Progress counters
    private DateTime _start;
    private int _decoded;
    private int _imported;
    private int _errors;
    private int _total;
    private UploadStatus _uploadStatus;
    private ConcurrentBag<ReplayError> _replayErrors = [];

    #region Progress Reporting

    private async Task ReportProgressAsync(
        IProgress<ImportProgress> progress,
        int discovered,
        CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                progress.Report(new ImportProgress(
                    Volatile.Read(ref _total),
                    discovered,
                    Volatile.Read(ref _decoded),
                    Volatile.Read(ref _imported),
                    Volatile.Read(ref _errors),
                    _uploadStatus,
                    DateTime.UtcNow - _start,
                    ImportStatus.Running
                ));
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    private void ResetCounters()
    {
        _start = DateTime.UtcNow;
        _total = 0;
        _decoded = 0;
        _imported = 0;
        _errors = 0;
        _uploadStatus = UploadStatus.None;
        _replayErrors.Clear();
    }

    #endregion
}
