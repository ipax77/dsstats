
using dsstats.db8;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    public async Task<List<string>> ScanForNewReplays(bool ordered = false)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

        var dbReplayPaths = await GetDbReplayPaths(context);
        var hdReplayPaths = await GetHdReplayPaths(configService.GetReplayFolders(),
                                                   configService.AppOptions.ReplayStartName,
                                                   ordered);
        hdReplayPaths.ExceptWith(configService.AppOptions.IgnoreReplays);
        hdReplayPaths.ExceptWith(dbReplayPaths);

        NewReplaysCount = hdReplayPaths.Count;
        DbReplaysCount = dbReplayPaths.Count;

        OnScanStateChanged(new() { DbReplays = DbReplaysCount, NewReplays = NewReplaysCount });

        return hdReplayPaths.ToList();
    }

    private async Task<List<string>> GetDbReplayPaths(ReplayContext context)
    {
        return await context.Replays
            .Select(s => s.FileName)
            .ToListAsync();
    }

    private async Task<HashSet<string>> GetHdReplayPaths(List<string> folders, string filenameStart, bool ordered)
    {
        var replayInfos = new List<FileInfo>();

        await Task.Run(() =>
        {
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                var fileInfos = new DirectoryInfo(folder)
                    .GetFiles($"{filenameStart}*.SC2Replay", SearchOption.AllDirectories);
                replayInfos.AddRange(fileInfos);
            }
        });

        if (ordered)
        {
            return replayInfos
                .OrderByDescending(o => o.CreationTime).Select(s => s.FullName)
                .ToHashSet();
        }
        else
        {
            return replayInfos.Select(s => s.FullName).ToHashSet();
        }
    }
}
