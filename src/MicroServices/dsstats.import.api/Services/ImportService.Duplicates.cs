using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.Linq;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    private async Task<bool> HandleDuplicate(Replay replay)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Replay? dupReplay = null;
        if (dbCache.ReplayHashes.TryGetValue(replay.ReplayHash, out int replayId))
        {
            dupReplay = await GetDupReplayFromId(replayId, context);
        }

        if (dupReplay == null)
        {
            List<string> replayLastSpawnHashes = replay.ReplayPlayers.Select(s => s.LastSpawnHash ?? "")
                .Where(x => !String.IsNullOrEmpty(x))
                .ToList();

            foreach (var lsHash in replayLastSpawnHashes)
            {
                if (dbCache.SpawnHashes.TryGetValue(lsHash, out int lsReplayId))
                {
                    dupReplay = await GetDupReplayFromId(lsReplayId, context);
                    break;
                }
            }
        }

        if (dupReplay == null)
        {
            return false;
        }

        if (!DuplicateIsPlausible(replay, dupReplay))
        {
            return false;
        }

        if (dupReplay.Duration >= replay.Duration)
        {
            dupReplay.ReplayPlayers = SyncReplayPlayers(dupReplay.ReplayPlayers.ToList(), replay.ReplayPlayers.ToList());
            
            await context.SaveChangesAsync();
            
            return true;
        }
        else
        {
            replay.ReplayPlayers = SyncReplayPlayers(replay.ReplayPlayers.ToList(), dupReplay.ReplayPlayers.ToList());

            var delReplay = await context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Upgrades)
                .FirstAsync(f => f.ReplayId == dupReplay.ReplayId);

            context.Replays.Remove(delReplay);
            
            await context.SaveChangesAsync();

            return false;
        }
    }

    private async Task<Replay?> GetDupReplayFromId(int replayId, ReplayContext context)
    {
        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .FirstOrDefaultAsync(f => f.ReplayId == replayId);
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
        if ((dupReplay.GameTime - replay.GameTime).Duration() > TimeSpan.FromDays(3))
        {
            logger.LogWarning($"false positive duplicate? {replay.GameTime} <=> {dupReplay.GameTime} : {dupReplay.ReplayHash}");
            return false;
        }
        return true;
    }
}
