using CommunityToolkit.Maui.Alerts;
using dsstats.db;
using dsstats.maui.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui.Services;

public sealed partial class DsstatsService
{
    private WatchService? WatchService;

    public async Task<DecodeStatus> GetDecodeStatus(CancellationToken token = default, bool dry = false)
    {
        int existingReplaysCount = 0;
        HashSet<string> replayPaths = [];
        await Task.Run(async () =>
        {
            var config = await GetConfig();

            if (config.AutoDecode && WatchService is null)
            {
                StartWatching(config);
            }

            var existingReplays = await GetExistingReplayPathsAsync(token);
            existingReplaysCount = existingReplays.Count;

            var folders = config.Sc2Profiles
                .Where(x => x.Active)
                .Select(s => s.Folder).ToHashSet();

            List<FileInfo> fileInfos = [];
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }
                var dir = new DirectoryInfo(folder);
                fileInfos.AddRange(dir.GetFiles(
                        $"{config.ReplayStartName}*.SC2Replay",
                        SearchOption.AllDirectories)
                    );
            }

            replayPaths = fileInfos
                .OrderByDescending(o => o.CreationTimeUtc).Select(s => s.FullName)
                .ToHashSet();
            replayPaths.ExceptWith(existingReplays);
            replayPaths.ExceptWith(config.IgnoreReplays);

        }, token);
        return new(existingReplaysCount, replayPaths.Count, dry ? [] : replayPaths.ToList());
    }

    private void ReplayDetected(object? sender, ReplayDetectedEventArgs e)
    {
        using var scope = scopeFactory.CreateScope();
        var importState = scope.ServiceProvider.GetRequiredService<ImportState>();
        if (importState.IsRunning)
        {
            return;
        }
        Toast.Make("New Replay detected.").Show();
        DecodeStatus decodeStatus = new(0, 1, [e.Path]);
        try
        {
            _ = StartImportAsync(importState, decodeStatus);
        }
        finally { }
    }

    public void StartWatching(MauiConfig config)
    {
        WatchService?.NewFileDetected -= ReplayDetected;
        WatchService = new(config.Sc2Profiles.Select(s => s.Folder), config.ReplayStartName);
        WatchService.NewFileDetected += ReplayDetected;
        WatchService.WatchForNewReplays();
    }

    public void StopWatching()
    {
        WatchService?.StopWatching();
    }

    #region Data Access Helpers

    private async Task<HashSet<string>> GetExistingReplayPathsAsync(
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        return await context.Replays
            .Where(x => x.FileName != null)
            .Select(x => x.FileName!)
            .ToHashSetAsync(ct);
    }

    #endregion
}
