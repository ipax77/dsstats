using System.Collections.Concurrent;

namespace dsstats.maui.Services;

public sealed partial class WatchService : IDisposable
{
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly ConcurrentDictionary<string, byte> detectedFiles = new();
    private CancellationTokenSource cts = new();

    public event EventHandler<ReplayDetectedEventArgs>? NewFileDetected;
    public bool IsWatching { get; private set; }

    private readonly IReadOnlyList<string> folders;
    private readonly string replayStartName;

    public WatchService(IEnumerable<string> folders, string replayStartName)
    {
        this.folders = folders.ToList();
        this.replayStartName = replayStartName;
    }

    public void WatchForNewReplays()
    {
        if (IsWatching) return;
        cts = new CancellationTokenSource();

        IsWatching = true;

        detectedFiles.Clear();

        foreach (var folder in folders)
        {
            var watcher = new FileSystemWatcher(folder)
            {
                Filter = $"{replayStartName}*.SC2Replay",
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += OnFileEvent;
            // watcher.Changed += OnFileEvent;

            watchers.Add(watcher);
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (!detectedFiles.TryAdd(e.FullPath, 0))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                if (await FileIsReadyAsync(e.FullPath, cts.Token))
                {
                    NewFileDetected?.Invoke(
                        this,
                        new ReplayDetectedEventArgs(e.FullPath));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
        });
    }

    public void StopWatching()
    {
        if (!IsWatching) return;
        IsWatching = false;

        cts.Cancel();

        foreach (var watcher in watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        watchers.Clear();
    }

    private static async Task<bool> FileIsReadyAsync(
        string path,
        CancellationToken token,
        int attempts = 30,
        int delayMs = 250)
    {
        for (int i = 0; i < attempts; i++)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                // Optional sanity check
                if (stream.Length > 0)
                    return true;
            }
            catch (IOException)
            {
                // File still locked by writer or system
            }
            catch (UnauthorizedAccessException)
            {
                // Writer or antivirus still holds restrictive handle
            }

            await Task.Delay(delayMs, token);
        }

        return false;
    }

    public void Dispose()
    {
        StopWatching();
        cts.Dispose();
    }
}


public sealed class ReplayDetectedEventArgs : EventArgs
{
    public ReplayDetectedEventArgs(string path) => Path = path;

    public string Path { get; }
}