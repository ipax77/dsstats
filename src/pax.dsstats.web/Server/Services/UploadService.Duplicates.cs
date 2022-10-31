using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Extensions;
using System.Diagnostics;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    public async Task<bool> CheckIsDuplicate(ReplayContext context, Replay replay, Uploader? uploader)
    {
        SetReplayPlayerLastSpawnHashes(replay);

        var dupReplayIds = await GetDuplicateReplayIds(context, replay);

        if (!dupReplayIds.Any())
        {
            return false;
        }
        else
        {
            return await HandleDuplicates(context, replay, dupReplayIds, uploader);
        }
    }

    private async Task<bool> HandleDuplicates(ReplayContext context, Replay replay, HashSet<int> dupReplayIds, Uploader? uploader)
    {
        List<ReplayPlayer> replayPlayers = replay.ReplayPlayers.ToList();
        HashSet<int> delReplayIds = new();
        Replay keepReplay = replay;
        HashSet<Uploader> uploaders = new();
        if (uploader != null)
        {
            uploaders.Add(uploader);
        }

        foreach (var dupReplayId in dupReplayIds)
        {
            var dupReplay = await context.Replays
                .Include(i => i.Uploaders)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .FirstAsync(f => f.ReplayId == dupReplayId);

            if (!DuplicateIsPlausible(replay, dupReplay))
            {
                continue;
            }

            var syncedReplayPlayers = SyncReplayPlayers(replayPlayers, dupReplay.ReplayPlayers.ToList());
            if (!syncedReplayPlayers.Any())
            {
                logger.LogWarning($"false positive duplicate (dtoPlayer)? {dupReplay.ReplayHash}");
                continue;
            }
            replayPlayers = syncedReplayPlayers;
            uploaders = uploaders.Union(dupReplay.Uploaders).ToHashSet();

            if (dupReplay.Duration > keepReplay.Duration)
            {
                keepReplay = dupReplay;
            }
            else
            {
                delReplayIds.Add(dupReplayId);
            }
        }

        replayPlayers.ForEach(f => { f.Replay = keepReplay; f.ReplayId = keepReplay.ReplayId; });
        keepReplay.ReplayPlayers = replayPlayers;
        keepReplay.Uploaders = uploaders;

        try
        {
            await DeleteReplays(context, delReplayIds);

            if (keepReplay.ReplayId == replay.ReplayId)
            {
                context.Replays.Add(keepReplay);
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed saving/deleting dupReplays: {ex.Message}");
        }
        return true;
    }

    private static async Task DeleteReplays(ReplayContext context, HashSet<int> delReplayIds)
    {
        if (!delReplayIds.Any())
        {
            return;
        }
        foreach (var delReplayId in delReplayIds)
        {
            var delReplay = await context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Upgrades)
                .FirstAsync(f => f.ReplayId == delReplayId);
            context.Replays.Remove(delReplay);
        }
        await context.SaveChangesAsync();
    }

    private static List<ReplayPlayer> SyncReplayPlayers(List<ReplayPlayer> replayPlayers, List<ReplayPlayer> dupReplayPlayers)
    {
        List<ReplayPlayer> syncedReplayPlayers = new();

        foreach (var replayPlayer in replayPlayers)
        {
            var dupReplayPlayer = dupReplayPlayers.FirstOrDefault(f => f.PlayerId == replayPlayer.PlayerId);
            if (dupReplayPlayer == null)
            {
                return new();
            }

            bool isUploader = dupReplayPlayer.IsUploader || replayPlayer.IsUploader;

            if (dupReplayPlayer.Duration > replayPlayer.Duration)
            {
                dupReplayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(dupReplayPlayer);
            }
            else
            {
                replayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(replayPlayer);
            }
        }

        return syncedReplayPlayers;
    }

    private bool DuplicateIsPlausible(Replay replay, Replay dupReplay)
    {
        if ((dupReplay.GameTime - replay.GameTime).Duration() > TimeSpan.FromDays(1))
        {
            logger.LogWarning($"false positive duplicate? {replay.GameTime} <=> {dupReplay.GameTime} : {dupReplay.ReplayHash}");
            return false;
        }
        return true;
    }

    private static void SetReplayPlayerLastSpawnHashes(Replay replay)
    {
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            replayPlayer.LastSpawnHash = replayPlayer.Spawns
                .FirstOrDefault(f => f.Breakpoint == shared.Breakpoint.All)?
                .GenHash(replay);
        }
    }

    private async Task<HashSet<int>> GetDuplicateReplayIds(ReplayContext context, Replay replay)
    {
        var dupReplayId = await context.Replays
            .Where(x => x.ReplayHash == replay.ReplayHash)
            .Select(s => s.ReplayId)
            .FirstOrDefaultAsync();

        List<string> replayLastSpawnHashes = replay.ReplayPlayers.Select(s => s.LastSpawnHash ?? "")
            .Where(x => !String.IsNullOrEmpty(x))
            .ToList();

        var dupReplayIds = replayLastSpawnHashes.Any() ? await context.ReplayPlayers
                .Include(i => i.Replay)
                .Where(x => x.LastSpawnHash != null && replayLastSpawnHashes.Contains(x.LastSpawnHash))
                .Select(s => s.Replay.ReplayId)
                .ToListAsync()
            : new List<int>();

        if (dupReplayId > 0)
        {
            dupReplayIds.Add(dupReplayId);
        }

        return dupReplayIds.ToHashSet();
    }











    public async Task CheckDuplicateSpeed()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = await GetSetPlayerHashesReplays(context, 3567, 1);

        Stopwatch sw = new();
        sw.Start();

        int i = 0;
        foreach (var replay in replays)
        {
            if (await CheckIsDuplicate(context, replay, null))
            {
                i++;
            }
        }

        sw.Stop();
        Console.WriteLine($"elapsed: {sw.ElapsedMilliseconds} ms ({i}");
    }

    public async Task SetReplayPlayerHashes()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int skip = 0;
        int take = 1000;

        var replays = await GetSetPlayerHashesReplays(context, skip, take);

        while (replays.Any())
        {
            foreach (var replay in replays)
            {
                SetReplayPlayerLastSpawnHashes(replay);
            }
            await context.SaveChangesAsync();
            skip += take;
            replays = await GetSetPlayerHashesReplays(context, skip, take);
            Console.WriteLine($"hashes set: {skip}");
        }
    }

    private async Task<List<Replay>> GetSetPlayerHashesReplays(ReplayContext context, int skip, int take)
    {
        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .OrderBy(o => o.ReplayId)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
