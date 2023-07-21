using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Services.Import;

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
            return true;
        }

        if (dupReplay.Duration >= replay.Duration)
        {
            await SyncReplayPlayers(dupReplay.ReplayPlayers.ToList(), replay.ReplayPlayers.ToList(), context);
            await context.SaveChangesAsync();

            return true;
        }
        else
        {
            await SyncReplayPlayers(replay.ReplayPlayers.ToList(), dupReplay.ReplayPlayers.ToList(), context);

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

    private static async Task<Replay?> GetDupReplayFromId(int replayId, ReplayContext context)
    {
        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .FirstOrDefaultAsync(f => f.ReplayId == replayId);
    }

    private async Task SyncReplayPlayers(List<ReplayPlayer> keepReplayPlayers, List<ReplayPlayer> replayPlayers, ReplayContext context)
    {
        foreach (var keepReplayPlayer in keepReplayPlayers)
        {
            var replayPlayer = replayPlayers.FirstOrDefault(f => f.PlayerId == keepReplayPlayer.PlayerId);
            if (replayPlayer == null)
            {
                await TryFixDupPlayer(keepReplayPlayer, GetReplayRegion(keepReplayPlayers, replayPlayers), context);
                continue;
            }

            if (replayPlayer.IsUploader)
            {
                keepReplayPlayer.IsUploader = true;
            }
        }
    }

    private async Task TryFixDupPlayer(ReplayPlayer keepReplayPlayer, int regionId, ReplayContext context)
    {
        if (regionId == 0)
        {
            logger.LogWarning("dup replayPlayer not found 1: ReplayPlayerId {ReplayPlayerId}", keepReplayPlayer.ReplayPlayerId);
            return;
        }

        var player = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.PlayerId == keepReplayPlayer.PlayerId);

        if (player == null || player.RegionId == regionId)
        {
            // logger.LogWarning($"dup replayPlayer not found 2: ReplayPlayerId {keepReplayPlayer.ReplayPlayerId}");
            return;
        }

        var players = await context.Players
            .Where(x => x.ToonId == player.ToonId && x.RegionId == regionId)
            .AsNoTracking()
            .ToListAsync();

        int newPlayerId;
        if (!players.Any())
        {
            if (!dbCache.Players.TryGetValue(new(player.ToonId, player.RealmId, regionId), out int playerId))
            {
                var newPlayer = new Player()
                {
                    Name = player.Name,
                    ToonId = player.ToonId,
                    RegionId = regionId,
                    RealmId = player.RealmId
                };

                context.Players.Add(newPlayer);
                await context.SaveChangesAsync();

                playerId = dbCache.Players[new(newPlayer.ToonId, newPlayer.RealmId, newPlayer.RegionId)] = newPlayer.PlayerId;
            }
            newPlayerId = playerId;
        }
        else
        {
            newPlayerId = players.First().PlayerId;
        }

        logger.LogWarning("fixing playerId for replayPlayer {ReplayPlayerId} from {PlayerId} to {newPlayerId}",
            keepReplayPlayer.ReplayPlayerId,
            keepReplayPlayer.PlayerId,
            newPlayerId);

        keepReplayPlayer.PlayerId = newPlayerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        keepReplayPlayer.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    private int GetReplayRegion(List<ReplayPlayer> keepReplayPlayers, List<ReplayPlayer> replayPlayers)
    {
        if (keepReplayPlayers.All(a => a.Player != null))
        {
            return MostFrequentNumber(keepReplayPlayers.Select(s => Math.Min(s.Player.RegionId, 3)).ToList());
        }

        if (replayPlayers.All(a => a.Player != null))
        {
            return MostFrequentNumber(replayPlayers.Select(s => Math.Min(s.Player.RealmId, 3)).ToList());
        }

        return 0;
    }

    public int MostFrequentNumber(List<int> numbers)
    {
        int[] frequency = new int[4];
        foreach (int number in numbers)
        {
            frequency[number]++;
        }
        int maxFrequency = frequency.Max();
        int mostFrequentNumber = frequency.ToList().IndexOf(maxFrequency);
        return mostFrequentNumber;
    }

    private bool DuplicateIsPlausible(Replay replay, Replay dupReplay)
    {
        if ((dupReplay.GameTime - replay.GameTime).Duration() > TimeSpan.FromDays(3))
        {
            logger.LogWarning("false positive duplicate? {GameTime} <=> {GameTime} : {ReplayHash}",
            replay.GameTime,
            dupReplay.GameTime,
            dupReplay.ReplayHash);
            return false;
        }
        return true;
    }
}
