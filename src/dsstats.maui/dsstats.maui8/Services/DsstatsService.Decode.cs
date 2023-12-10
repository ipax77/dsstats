using dsstats.shared.Interfaces;
using System.Collections.Concurrent;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    CancellationTokenSource? ctsDecode = null;

    private DateTime decodeStart;
    private int doneDecoding = 0;
    private int todoDecoding = 0;
    private int threads = 2;
    private int etaWindow = 10;
    private Timer? notifyTimer = null;
    private ConcurrentQueue<double> etaMovingAverageValues = new();

    public bool Decoding => ctsDecode != null && !ctsDecode.IsCancellationRequested;

    public event EventHandler<DecodeInfoEventArgs>? DecodeStateChanged;
    private void OnDecodeStateChanged(DecodeInfoEventArgs e)
    {
        EventHandler<DecodeInfoEventArgs>? handler = DecodeStateChanged;
        handler?.Invoke(this, e);
    }

    public async void DecodeNewReplays()
    {
        if (ctsDecode != null && !ctsDecode.IsCancellationRequested)
        {
            OnDecodeStateChanged(new() { Info = Loc["Already decoding."] });
            return;
        }

        var newReplayPaths = await ScanForNewReplays(true);

        if (newReplayPaths.Count == 0)
        {
            OnDecodeStateChanged(new() { Info = Loc["No new replays found."] });
            return;
        }

        SetupDecodeJob(newReplayPaths.Count);

        using var scope = scopeFactory.CreateAsyncScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
        
        threads = Math.Min(Environment.ProcessorCount, configService.AppOptions.CPUCores);
        threads = Math.Max(1, threads);
        etaWindow = Math.Max(10, threads + 5);

        _ = StartDecodeJob(newReplayPaths).ConfigureAwait(false);
    }

    private async Task StartDecodeJob(List<string> replays)
    {
        if (ctsDecode is not null)
        {
            bool singleSave = replays.Count < 10;
            await Task.Run(async () =>
            {
                await Decode(replays, ctsDecode.Token, singleSave);
                notifyTimer?.Dispose();
                notifyTimer = null;
                if (!singleSave)
                {
                    OnDecodeStateChanged(new() { Saving = true });
                    await ImportReplays(true);
                }
                await UploadReplays();
                await ProduceRatings();
            });
            CleanUpDecodeJob();
            _ = ScanForNewReplays();

            var decodeInfoEnd = GetDecodeInfo(true);
            decodeInfoEnd.Finished = true;
            OnDecodeStateChanged(decodeInfoEnd);
        }
    }

    private void TimerElapsed(object? state)
    {
        var decodeInfo = GetDecodeInfo();
        OnDecodeStateChanged(decodeInfo);
    }

    public void CancelDecoding()
    {
        if (ctsDecode is not null)
        {
            ctsDecode.Cancel();
            CleanUpDecodeJob();
            OnDecodeStateChanged(new() { Info = Loc["Decoding canceled."] });
        }
    }

    private DecodeInfoEventArgs GetDecodeInfo(bool finished = false)
    {
        TimeSpan elapsed = DateTime.UtcNow - decodeStart;

        if (finished)
        {
            return new DecodeInfoEventArgs
            {
                Done = ctsDecode == null || ctsDecode.IsCancellationRequested ? doneDecoding : todoDecoding,
                Total = todoDecoding,
                Elapsed = elapsed,
                UploadStatus = uploadStatus,
                Eta = TimeSpan.Zero
            };
        }

        double itemsPerSecond = elapsed.TotalSeconds < 10 || elapsed.TotalSeconds == 0 ? 
            3.0 / threads
            : doneDecoding / elapsed.TotalSeconds;
        double remainingTimeInSeconds = itemsPerSecond == 0 ? 0 : (todoDecoding - doneDecoding) / itemsPerSecond;
        etaMovingAverageValues.Enqueue(remainingTimeInSeconds);

        double smoothedEtaInSeconds = etaMovingAverageValues.Average();
        while (etaMovingAverageValues.Count > etaWindow)
        {
            etaMovingAverageValues.TryDequeue(out _);
        }

        return new()
        {
            Done = finished ? todoDecoding : doneDecoding,
            Total = todoDecoding,
            Elapsed = elapsed,
            UploadStatus = uploadStatus,
            Eta = TimeSpan.FromSeconds(smoothedEtaInSeconds)
        };
    }

    private async Task ProduceRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        var decodeInfoStart = GetDecodeInfo(true);
        decodeInfoStart.Calculating = true;
        OnDecodeStateChanged(decodeInfoStart);

        await ratingService.ProduceRatings(shared.RatingCalcType.Dsstats);
    }

    private void SetupDecodeJob(int newReplaysCount)
    {
        ctsDecode = new();
        decodeStart = DateTime.UtcNow;
        doneDecoding = 0;
        todoDecoding = newReplaysCount;
        notifyTimer = new Timer(TimerElapsed, null, 0, 1000);

        var decodeInfo = GetDecodeInfo();
        decodeInfo.Info = Loc["Start decoding."];
        OnDecodeStateChanged(decodeInfo);
    }

    private void CleanUpDecodeJob()
    {
        ctsDecode?.Dispose();
        ctsDecode = null;

        etaMovingAverageValues.Clear();
        etaWindow = 10;
    }
}

public class BelowNormalPriorityTaskScheduler : TaskScheduler
{
    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return new List<Task>(); // Return an empty list since we won't track tasks.
    }

    protected override void QueueTask(Task task)
    {
        // Set the priority to BelowNormal before scheduling the task.
        Thread newThread = new Thread(() => TryExecuteTask(task)) { Priority = ThreadPriority.BelowNormal };
        newThread.Start();
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false; // We don't support inlining tasks.
    }
}

public class DecodeInfoEventArgs : EventArgs
{
    public int Done { get; set; }
    public int Total { get; set; }
    public TimeSpan Eta { get; set; }
    public TimeSpan Elapsed { get; set; }
    public bool Saving { get; set; }
    public bool Calculating { get; set; }
    public UploadStatus UploadStatus { get; set; }
    public bool Finished { get; set; }
    public string? Info { get; set; }
    public DecodeError? DecodeError { get; set; }
}

public record DecodeError
{
    public string ReplayPath { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}

public enum UploadStatus
{
    None = 0,
    Uploading = 1,
    UploadError = 2,
    UploadSuccess = 3,
    Forbidden = 4
}