using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.maui.Services;

public class WatchService : IDisposable
{
    private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    public event EventHandler<ReplayDetectedEventArgs>? NewFileDetected;
    public bool IsWatching { get; private set; }
    private readonly HashSet<string> filesDetected = new();
    private readonly object lockobject = new();

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

        foreach (var path in UserSettingsService.UserSettings.ReplayPaths)
        {
            Task.Factory.StartNew(() =>
            {
                CreateWatcher(path);
            });
        }
    }

    public void StopWatching()
    {
        IsWatching = false;
        manualResetEvent.Set();
    }

    private void CreateWatcher(string path)
    {
        using var watcher = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
        watcher.Changed += Watcher_Changed;
        watcher.Filter = "*.SC2Replay";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        manualResetEvent.WaitOne();
        watcher.Dispose();
    }

    private async void Watcher_Changed(object sender, FileSystemEventArgs e)
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
        TimeSpan waitTime = TimeSpan.FromSeconds(250);

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
            using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
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