
using pax.dsstats.dbng;

namespace dsstats.import.api.Services;

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

                var playerInfo = dbCache.Players[rp.Player.ToonId];

                // todo: fix regionId !?

                rp.PlayerId = playerInfo.Key;
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
                if (!dbCache.Players.ContainsKey(rp.Player.ToonId))
                {
                    Player player = new()
                    {
                        Name = rp.Player.Name,
                        ToonId = rp.Player.ToonId,
                        RegionId = rp.Player.RegionId,
                    };
                    context.Players.Add(player);
                    newPlayers.Add(player);
                    if (newPlayers.Count % 1000 == 0)
                    {
                        await context.SaveChangesAsync();
                    }
                    dbCache.Players[rp.Player.ToonId] = new(0, 0);
                }
            }
        }

        await context.SaveChangesAsync();
        newPlayers.ForEach(f => dbCache.Players[f.ToonId] = new(f.PlayerId, f.RegionId));
        return newPlayers.Count;
    }
}