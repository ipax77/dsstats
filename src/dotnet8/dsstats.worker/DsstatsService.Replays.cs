
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace dsstats.worker;

public partial class DsstatsService
{
    private async Task<List<string>> GetNewReplays()
    {
        var dbReplayPaths = await GetDbReplayPaths();
        var hdReplayPaths = GetHdReplayPaths();

        var newReplays = hdReplayPaths.Except(dbReplayPaths).ToList();

        return newReplays.Take(100).ToList();
    }

    private List<string> GetHdReplayPaths()
    {
        List<KeyValuePair<string, DateTime>> files = new();
        foreach (var dir in AppConfigOptions.ReplayFolders)
        {
            DirectoryInfo info = new(dir);
            files.AddRange(info.GetFiles($"{AppConfigOptions.ReplayStartName}*.SC2Replay", SearchOption.AllDirectories)
                .Where(x => x.Length > 100)
                .Select(s => new KeyValuePair<string, DateTime>(s.FullName, s.CreationTime)));
        }
        return files.OrderBy(o => o.Value).Select(s => s.Key).ToList();
    }

    private async Task<List<string>> GetDbReplayPaths()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (AppConfigOptions.ExcludeReplays.Count > 0)
        {
            return (await context.Replays
                .AsNoTracking()
                .Select(s => s.FileName)
                .ToListAsync())
                .Union(AppConfigOptions.ExcludeReplays)
                .ToList();
        }
        else
        {
            return await context.Replays
                .AsNoTracking()
                .Select(s => s.FileName)
                .ToListAsync();
        }
    }
}