
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    private async Task<bool> IsDbDuplicate(ReplayContext context, Replay replay, Dictionary<int, Uploader> fakeUploaderDic)
    {
        var dupReplayIds = await GetDuplicateReplayIds(context, replay);

        if (!dupReplayIds.Any())
        {
            return false;
        }
        else
        {
            return await HandleDuplicates(context, replay, dupReplayIds, fakeUploaderDic);
        }
    }

    private static async Task<HashSet<int>> GetDuplicateReplayIds(ReplayContext context, Replay replay)
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

    private async Task<bool> HandleDuplicates(ReplayContext context, Replay replay, HashSet<int> dupReplayIds, Dictionary<int, Uploader> fakeUploaderDic)
    {
        HashSet<int> delReplayIds = new();
        Replay keepReplay = replay;
        HashSet<Uploader> uploaders = new();
        if (replay.UploaderId > 0)
        {
            var uploader = fakeUploaderDic[replay.UploaderId];
            context.Attach(uploader);
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
            uploaders = uploaders.Union(dupReplay.Uploaders).ToHashSet();

            if (dupReplay.Duration >= keepReplay.Duration)
            {
                keepReplay = dupReplay;
                keepReplay.ReplayPlayers = SyncReplayPlayers(keepReplay.ReplayPlayers.ToList(), replay.ReplayPlayers.ToList());
            }
            else
            {
                delReplayIds.Add(dupReplayId);
                keepReplay.ReplayPlayers = SyncReplayPlayers(keepReplay.ReplayPlayers.ToList(), dupReplay.ReplayPlayers.ToList());
            }
        }

        keepReplay.ReplayPlayers.ToList().ForEach(f => { f.Replay = keepReplay; f.ReplayId = keepReplay.ReplayId; });
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

    private static List<ReplayPlayer> SyncReplayPlayers(List<ReplayPlayer> keepReplayPlayers, List<ReplayPlayer> replayPlayers)
    {
        List<ReplayPlayer> syncedReplayPlayers = new();

        foreach (var keepReplayPlayer in keepReplayPlayers)
        {
            var replayPlayer = replayPlayers.FirstOrDefault(f => f.PlayerId == keepReplayPlayer.PlayerId);
            if (replayPlayer == null)
            {
                return new();
            }

            bool isUploader = replayPlayer.IsUploader || keepReplayPlayer.IsUploader;

            if (replayPlayer.Duration > keepReplayPlayer.Duration)
            {
                replayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(replayPlayer);
            }
            else
            {
                keepReplayPlayer.IsUploader = isUploader;
                syncedReplayPlayers.Add(keepReplayPlayer);
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
}