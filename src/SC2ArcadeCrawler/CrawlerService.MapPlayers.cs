using dsstats.db8;
using Microsoft.Extensions.DependencyInjection;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private async Task MapPlayers(List<ArcadeReplay> replays, CancellationToken token)
    {
        int newPlayers = await CreateMissingPlayers(replays, token);
        MapReplayPlayerPlayers(replays);
    }

    private void MapReplayPlayerPlayers(List<ArcadeReplay> replays)
    {
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ArcadeReplayPlayers)
            {
                if (rp.ArcadePlayer == null)
                {
                    continue;
                }

                var playerId = arcadePlayerIds[new(rp.ArcadePlayer.RegionId, rp.ArcadePlayer.RealmId, rp.ArcadePlayer.ProfileId)];

                // todo: fix regionId !?

                rp.ArcadePlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                rp.ArcadePlayer = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
    }

    private async Task<int> CreateMissingPlayers(List<ArcadeReplay> replays, CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<ArcadePlayer> newPlayers = new();

        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ArcadeReplayPlayers)
            {
                if (!arcadePlayerIds.ContainsKey(new(rp.ArcadePlayer.RegionId, rp.ArcadePlayer.RealmId, rp.ArcadePlayer.ProfileId)))
                {
                    ArcadePlayer player = new()
                    {
                        Name = rp.Name,
                        RegionId = rp.ArcadePlayer.RegionId,
                        RealmId = rp.ArcadePlayer.RealmId,
                        ProfileId = rp.ArcadePlayer.ProfileId
                    };
                    context.ArcadePlayers.Add(player);
                    newPlayers.Add(player);
                    if (newPlayers.Count % 1000 == 0)
                    {
                        await context.SaveChangesAsync(token);
                    }
                    arcadePlayerIds[new(rp.ArcadePlayer.RegionId, rp.ArcadePlayer.RealmId, rp.ArcadePlayer.ProfileId)] = 0;
                }
            }
        }

        await context.SaveChangesAsync(token);
        newPlayers.ForEach(f => arcadePlayerIds[new(f.RegionId, f.RealmId, f.ProfileId)] = f.ArcadePlayerId);
        return newPlayers.Count;
    }
}
