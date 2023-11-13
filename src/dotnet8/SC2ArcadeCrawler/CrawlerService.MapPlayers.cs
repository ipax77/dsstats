using dsstats.db8;
using Microsoft.Extensions.DependencyInjection;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private async Task MapPlayers(List<ArcadeReplay> replays)
    {
        int newPlayers = await CreateMissingPlayers(replays);
        MapReplayPlayerPlayers(replays);
    }

    private void MapReplayPlayerPlayers(List<ArcadeReplay> replays)
    {
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ArcadeReplayPlayers)
            {
                if (rp.Player == null)
                {
                    continue;
                }

                var playerId = arcadePlayerIds[new(rp.Player.RegionId, rp.Player.RealmId, rp.Player.ToonId)];

                // todo: fix regionId !?

                rp.PlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                rp.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
    }

    private async Task<int> CreateMissingPlayers(List<ArcadeReplay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Player> newPlayers = new();

        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ArcadeReplayPlayers)
            {
                if (!arcadePlayerIds.ContainsKey(new(rp.Player.RegionId, rp.Player.RealmId, rp.Player.ToonId)))
                {
                    Player player = new()
                    {
                        Name = rp.Name,
                        RegionId = rp.Player.RegionId,
                        RealmId = rp.Player.RealmId,
                        ToonId = rp.Player.ToonId
                    };
                    context.Players.Add(player);
                    newPlayers.Add(player);
                    if (newPlayers.Count % 1000 == 0)
                    {
                        await context.SaveChangesAsync();
                    }
                    arcadePlayerIds[new(rp.Player.RegionId, rp.Player.RealmId, rp.Player.ToonId)] = 0;
                }
            }
        }

        await context.SaveChangesAsync();
        newPlayers.ForEach(f => arcadePlayerIds[new(f.RegionId, f.RealmId, f.ToonId)] = f.PlayerId );
        return newPlayers.Count;
    }
}
