using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using System.Linq;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    private async Task<bool> HandleDuplicate(Replay replay)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dupReplay = await context.Replays
            .Include(i => i.Uploaders)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .FirstOrDefaultAsync(f => f.ReplayHash == replay.ReplayHash);

        if (dupReplay == null)
        {
            List<string> replayLastSpawnHashes = replay.ReplayPlayers.Select(s => s.LastSpawnHash ?? "")
                .Where(x => !String.IsNullOrEmpty(x))
                .ToList();

#pragma warning disable CS8604 // Possible null reference argument.
            dupReplay = await context.Replays
                .Include(i => i.Uploaders)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .Where(x => x.ReplayPlayers.Any(a => replayLastSpawnHashes.Contains(a.LastSpawnHash)))
                .FirstOrDefaultAsync();
#pragma warning restore CS8604 // Possible null reference argument.
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
