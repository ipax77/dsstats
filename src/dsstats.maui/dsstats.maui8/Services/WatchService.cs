namespace dsstats.maui8.Services;

public class WatchService : IDisposable
{
    private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    public event EventHandler<ReplayDetectedEventArgs>? NewFileDetected;
    public bool IsWatching { get; private set; }
    private readonly HashSet<string> filesDetected = new();
    private readonly object lockobject = new();
    private readonly List<string> Folders;
    private readonly string ReplayStartName;

    public WatchService(List<string> folders, string replayStartname)
    {
        Folders = folders;
        ReplayStartName = replayStartname;
    }

    protected virtual void OnNewFileDetected(ReplayDetectedEventArgs e)
    {
        EventHandler<ReplayDetectedEventArgs>? handler = NewFileDetected;
        handler?.Invoke(this, e);
    }

    public void WatchForNewReplays()
    {
        if (IsWatching) return;
        IsWatching = true;
        filesDetected.Clear();
        manualResetEvent.Reset();

        foreach (var path in Folders)
        {
            Task.Factory.StartNew(() =>
            {
                CreateWatcher(path, ReplayStartName);
            });
        }
    }

    public void StopWatching()
    {
        if (!IsWatching) return;
        IsWatching = false;
        manualResetEvent.Set();
    }

    private void CreateWatcher(string path, string startName)
    {
        using var watcher = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
        watcher.Changed += Watcher_Changed;
        watcher.Filter = $"{startName}*.SC2Replay";
        watcher.IncludeSubdirectories = false;
        watcher.EnableRaisingEvents = true;
        manualResetEvent.WaitOne();
        watcher.Changed -= Watcher_Changed;
        watcher.Dispose();
    }

    private async void Watcher_Changed(object? sender, FileSystemEventArgs e)
    {
        lock (lockobject)
        {
            if (filesDetected.Contains(e.FullPath))
                return;
            filesDetected.Add(e.FullPath);
        }
        if (await FileIsReady(e.FullPath))
        {
            OnNewFileDetected(new() { Path = e.FullPath });
        }
    }

    private async Task<bool> FileIsReady(string path)
    {
        int maxAttempts = 15;
        TimeSpan waitTime = TimeSpan.FromMilliseconds(250);

        await Task.Delay(waitTime);
        while (!IsFileReady(path))
        {
            maxAttempts--;
            if (maxAttempts == 0)
            {
                return false;
            }
            await Task.Delay(waitTime);
        }
        return true;
    }

    private static bool IsFileReady(string filename)
    {
        try
        {
            using var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
            return inputStream.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        manualResetEvent.Set();
    }
}

public class ReplayDetectedEventArgs : EventArgs
{
    public string Path { get; init; } = null!;
}