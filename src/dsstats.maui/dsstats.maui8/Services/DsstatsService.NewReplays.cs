
using dsstats.db8;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    public async Task<List<string>> ScanForNewReplays(bool ordered = false)
    {
        var dbReplayPaths = await GetDbReplayPaths();
        var hdReplayPaths = await GetHdReplayPaths(ordered);

        var newReplayPaths = hdReplayPaths.Except(dbReplayPaths).ToList();

        NewReplaysCount = newReplayPaths.Count;
        DbReplaysCount = dbReplayPaths.Count;

        OnScanStateChanged(new() { DbReplays = DbReplaysCount, NewReplays = NewReplaysCount });

        return newReplayPaths;
    }

    private async Task<List<string>> GetDbReplayPaths()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Replays
            .Select(s => s.FileName)
            .ToListAsync();
    }

    private async Task<List<string>> GetHdReplayPaths(bool ordered)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        var folders = configService.GetReplayFolders();
        var filenameStart = configService.AppOptions.ReplayStartName;

        var replayPaths = new List<string>();

        await Task.Run(() =>
        {
            foreach (var folder in folders)
            {
                var replayFiles = Directory.GetFiles(folder, $"{filenameStart}*.SC2Replay", SearchOption.TopDirectoryOnly);

                replayFiles = replayFiles.Where(file => !File.GetAttributes(file).HasFlag(FileAttributes.Directory)).ToArray();

                replayPaths.AddRange(replayFiles);
            }
        });

        if (ordered)
        {
            replayPaths = replayPaths.OrderByDescending(path =>
            {
                var fileInfo = new FileInfo(path);
                return fileInfo.CreationTime;
            }).ToList();
        }

        return replayPaths
            .Except(configService.AppOptions.IgnoreReplays)
            .ToList();
    }
}
