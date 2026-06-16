using CommunityToolkit.Maui.Alerts;
using dsstats.db;
using dsstats.maui.Services.Models;
using dsstats.shared.Maui;
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
            var config = await GetConfigDto().ConfigureAwait(false);

            if (config.AutoDecode && WatchService is null)
            {
                StartWatching(config);
            }

            var existingReplays = await GetExistingReplayPathsAsync(token).ConfigureAwait(false);
            existingReplaysCount = existingReplays.Count;

            var folders = GetActiveReplayFolders(config);

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

        }, token).ConfigureAwait(false);
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

    public void StartWatching(MauiConfigDto config)
    {
        WatchService?.NewFileDetected -= ReplayDetected;
        WatchService = new(GetActiveReplayFolders(config), config.ReplayStartName);
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
            .ToHashSetAsync(ct)
            .ConfigureAwait(false);
    }

    private static HashSet<string> GetActiveReplayFolders(MauiConfigDto config)
    {
        HashSet<string> folders = new(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in config.Sc2Profiles)
        {
            if (profile.Active && !string.IsNullOrWhiteSpace(profile.Folder))
            {
                folders.Add(MauiConfigPersistence.NormalizeFolderPath(profile.Folder));
            }
        }

        foreach (var folder in config.ManualReplayFolders)
        {
            if (folder.Active && !string.IsNullOrWhiteSpace(folder.Folder))
            {
                folders.Add(MauiConfigPersistence.NormalizeFolderPath(folder.Folder));
            }
        }

        return folders;
    }

    #endregion
}
