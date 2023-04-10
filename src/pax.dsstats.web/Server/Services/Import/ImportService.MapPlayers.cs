
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{
    private async Task<int> MapPlayers(List<Replay> replays)
    {
        int newPlayers = await CreateMissingPlayers(replays);
        MapReplayPlayerPlayers(replays);
        return newPlayers;
    }

    private void MapReplayPlayerPlayers(List<Replay> replays)
    {
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ReplayPlayers)
            {
                if (rp.Player == null)
                {
                    continue;
                }

                var playerId = dbCache.Players[new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)];

                // todo: fix regionId !?

                rp.PlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                rp.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
    }

    private async Task<int> CreateMissingPlayers(List<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Player> newPlayers = new();

        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ReplayPlayers)
            {
                if (rp.Player.RealmId == 0)
                {
                    rp.Player.RealmId = 1;
                }

                if (!dbCache.Players.ContainsKey(new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)))
                {
                    Player player = new()
                    {
                        Name = rp.Player.Name,
                        ToonId = rp.Player.ToonId,
                        RegionId = rp.Player.RegionId,
                        RealmId = rp.Player.RealmId,
                    };
                    context.Players.Add(player);
                    newPlayers.Add(player);
                    if (newPlayers.Count % 1000 == 0)
                    {
                        await context.SaveChangesAsync();
                    }
                    dbCache.Players[new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId)] = 0;
                }
            }
        }

        await context.SaveChangesAsync();
        newPlayers.ForEach(f => dbCache.Players[new(f.ToonId, f.RealmId, f.RegionId)] = f.PlayerId);
        return newPlayers.Count;
    }
}