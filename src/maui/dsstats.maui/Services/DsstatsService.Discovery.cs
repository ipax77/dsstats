using CommunityToolkit.Maui.Alerts;
using dsstats.db;
using dsstats.maui.Services.Models;
using dsstats.shared.Maui;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui.Services;

public sealed partial class DsstatsService
{
    private const int ExistingReplayLookupBatchSize = 250;
    private WatchService? WatchService;

    public async Task<DecodeStatus> GetDecodeStatus(CancellationToken token = default, bool dry = false)
    {
        int existingReplaysCount = 0;
        int newInFolders = 0;
        List<ReplayFileCandidate>? replayCandidates = dry ? null : [];
        await Task.Run(async () =>
        {
            var config = await GetConfigDto().ConfigureAwait(false);

            if (config.AutoDecode && WatchService is null)
            {
                StartWatching(config);
            }

            existingReplaysCount = await GetExistingReplayCountAsync(token).ConfigureAwait(false);
            var folders = GetActiveReplayFolders(config);
            var ignoreReplays = config.IgnoreReplays
                .Select(MauiConfigPersistence.NormalizeReplayPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> discoveredReplayPaths = new(StringComparer.OrdinalIgnoreCase);
            List<ReplayFileCandidate> pending = new(ExistingReplayLookupBatchSize);
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (var candidate in EnumerateReplayFileCandidates(folder, config.ReplayStartName))
                {
                    token.ThrowIfCancellationRequested();
                    if (!discoveredReplayPaths.Add(candidate.Path))
                    {
                        continue;
                    }

                    if (ignoreReplays.Contains(candidate.Path))
                    {
                        continue;
                    }

                    pending.Add(candidate);
                    if (pending.Count >= ExistingReplayLookupBatchSize)
                    {
                        newInFolders += await AddNewReplayCandidatesAsync(pending, replayCandidates, token)
                            .ConfigureAwait(false);
                        pending.Clear();
                    }
                }
            }

            if (pending.Count > 0)
            {
                newInFolders += await AddNewReplayCandidatesAsync(pending, replayCandidates, token)
                    .ConfigureAwait(false);
            }
        }, token).ConfigureAwait(false);

        var replayPaths = replayCandidates?
            .OrderByDescending(candidate => candidate.CreationTimeUtc)
            .Select(candidate => candidate.Path)
            .ToList() ?? [];
        return new(existingReplaysCount, newInFolders, replayPaths);
    }

    private async void ReplayDetected(object? sender, ReplayDetectedEventArgs e)
    {
        using var scope = scopeFactory.CreateScope();
        var importState = scope.ServiceProvider.GetRequiredService<ImportState>();
        if (importState.IsRunning)
        {
            return;
        }

        var config = await GetConfigDto().ConfigureAwait(false);
        var replayPath = MauiConfigPersistence.NormalizeReplayPath(e.Path);
        var ignoreReplays = config.IgnoreReplays
            .Select(MauiConfigPersistence.NormalizeReplayPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ignoreReplays.Contains(replayPath))
        {
            return;
        }
        await Toast.Make("New Replay detected.").Show();
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

    private async Task<int> GetExistingReplayCountAsync(
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        return await context.Replays
            .AsNoTracking()
            .Where(x => x.FileName != null)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetExistingReplayPathsAsync(
        IReadOnlyCollection<string> replayPaths,
        CancellationToken ct)
    {
        if (replayPaths.Count == 0)
        {
            return [];
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var existingReplayPaths = await context.Replays
            .AsNoTracking()
            .Where(x => x.FileName != null && replayPaths.Contains(x.FileName))
            .Select(x => x.FileName!)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return existingReplayPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<int> AddNewReplayCandidatesAsync(
        List<ReplayFileCandidate> pending,
        List<ReplayFileCandidate>? replayCandidates,
        CancellationToken ct)
    {
        var pendingPaths = pending.Select(candidate => candidate.Path).ToList();
        var existingReplayPaths = await GetExistingReplayPathsAsync(pendingPaths, ct).ConfigureAwait(false);
        var addedCount = 0;
        foreach (var candidate in pending)
        {
            if (existingReplayPaths.Contains(candidate.Path))
            {
                continue;
            }

            replayCandidates?.Add(candidate);
            addedCount++;
        }

        return addedCount;
    }

    private static IEnumerable<ReplayFileCandidate> EnumerateReplayFileCandidates(
        string folder,
        string replayStartName)
    {
        foreach (var file in EnumerateReplayFilesSafe(folder, $"{replayStartName}*.SC2Replay"))
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new(file);
            }
            catch (Exception)
            {
                continue;
            }

            yield return new(fileInfo.FullName, fileInfo.CreationTimeUtc);
        }
    }

    private static IEnumerable<string> EnumerateReplayFilesSafe(string folder, string pattern)
    {
        var pendingFolders = new Stack<string>();
        pendingFolders.Push(folder);

        while (pendingFolders.Count > 0)
        {
            var currentFolder = pendingFolders.Pop();
            string[] files;
            try
            {
                files = Directory.GetFiles(currentFolder, pattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            string[] childFolders;
            try
            {
                childFolders = Directory.GetDirectories(currentFolder);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var childFolder in childFolders)
            {
                pendingFolders.Push(childFolder);
            }
        }
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

    private sealed record ReplayFileCandidate(string Path, DateTime CreationTimeUtc);
}
