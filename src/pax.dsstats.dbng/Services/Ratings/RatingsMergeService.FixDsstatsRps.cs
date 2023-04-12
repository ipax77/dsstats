using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsMergeService
{
    public void FixDsstatsReplayPlayersRegionId(bool dry = false)
    {
        var players = context.Players
            .Select(s => new { s.ToonId, s.PlayerId, s.RegionId, s.RealmId })
            .ToList().ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        for (int regionId = 1; regionId <= 2; regionId++)
        {
            int otherRegion = regionId == 1 ? 2 : 1;

            var replaysQuery = from r in context.Replays
                               .Include(i => i.ReplayPlayers)
                                    .ThenInclude(i => i.Player)
                               from rp1 in r.ReplayPlayers
                               from rp2 in r.ReplayPlayers
                               where rp1.Player.RealmId == 1
                                && rp1.Player.RegionId == regionId
                                && rp2 != rp1
                                && rp2.Player.RegionId == otherRegion
                               select r;

            var replays = replaysQuery.Distinct().ToList();

            int playersCreated = 0;
            int playersFixed = 0;

            foreach (var replay in replays)
            {
                var replayRegion = GetReplayRegion(replay);
                foreach (var rp in replay.ReplayPlayers.Where(x => x.Player.RegionId != replayRegion))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (!players.TryGetValue(new(rp.Player.ToonId, rp.Player.RealmId, replayRegion), out var playerId))
                    {
                        Player player = new()
                        {
                            Name = rp.Name,
                            ToonId = rp.Player.ToonId,
                            RegionId = replayRegion,
                            RealmId = rp.Player.RealmId
                        };
                        if (!dry)
                        {
                            context.Players.Add(player);
                            context.SaveChanges();
                        }
                        // playerId = players[new(player.ToonId, player.RealmId, player.RegionId)] = player.PlayerId;
                        playerId = players[new(player.ToonId, player.RealmId, player.RegionId)] = 77;
                        playersCreated++;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    rp.PlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    rp.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    playersFixed++;
                }
            }
            if (!dry)
            {
                context.SaveChanges();
            }
            logger.LogWarning($"{regionId} => created: {playersCreated}, fixed: {playersFixed}");

            if (dry)
            {
                break;
            }
        }
    }

    public int GetReplayRegion(Replay replay)
    {
        Dictionary<int, int> regionCounts = new()
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
        };

        foreach (var rp in replay.ReplayPlayers)
        {
            if (rp.Player.RegionId <= 0 || rp.Player.RegionId > 3)
            {
                logger.LogWarning($"Unknown player region found {replay.ReplayHash} => Pos: {rp.GamePos}, Region: {rp.Player.RegionId}, PlayerId: {rp.Player.PlayerId}");
                continue;
            }
            regionCounts[rp.Player.RegionId]++;
        }

        return regionCounts.OrderByDescending(o => o.Value).First().Key;
    }
}
