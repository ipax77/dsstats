using dsstats.maui.Services.Models;

namespace dsstats.maui.Services;

public sealed class ImportState
{
    public ImportStatus Status { get; private set; } = ImportStatus.Idle;
    private CancellationTokenSource? _cts;
    public ImportProgress Progress { get; private set; }
        = new(0, 0, 0, 0, 0, UploadStatus.None, TimeSpan.Zero, ImportStatus.Idle);

    public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

    public DecodeStatus? DecodeStatus { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action? OnChange;
    public event Action? OnFinished;

    public void UpdateProgress(ImportProgress progress)
    {
        Progress = progress;
        SetStatus(progress.ImportStatus);
        Notify();
    }

    public void SetDecodeStatus(DecodeStatus status)
    {
        DecodeStatus = status;
        Notify();
    }

    public void SetRunning(bool running)
    {
        IsRunning = running;
        Notify();
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        SetRunning(true);
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void Complete()
    {
        _cts?.Dispose();
        _cts = null;
        SetRunning(false);
    }

    public void SetStatus(ImportStatus status)
    {
        Status = status;
        if (Status == ImportStatus.Completed)
        {
            OnFinished?.Invoke();
        }
    }

    private void Notify()
    {
        OnChange?.Invoke();
    }
}

