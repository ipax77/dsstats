using dsstats.db8;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private async Task MapPlayers(List<ArcadeReplay> replays, CancellationToken token)
    {
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        foreach (var replay in replays)
        {
            foreach (var replayDsPlayer in replay.ArcadeReplayDsPlayers)
            {
                if (replayDsPlayer.Player is null)
                {
                    continue;
                }
                int playerId = await importService.GetPlayerIdAsync(new(replayDsPlayer.Player.RegionId,
                    replayDsPlayer.Player.RealmId,
                    replayDsPlayer.Player.ToonId),
                replayDsPlayer.Name);
                replayDsPlayer.Player = null;
                replayDsPlayer.PlayerId = playerId;
            }
        }
    }

    public async Task MapArcadePlayersToPlayers()
    {
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<int, int> ArcadePlayerIdDict = await importService.MapArcadePlayers();

        int skip = 0;
        int take = 100_000;

        var arcadeReplayPlayers = await context.ArcadeReplayPlayers
            .OrderBy(o => o.ArcadeReplayPlayerId)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        while (arcadeReplayPlayers.Count > 0)
        {
            List<ArcadeReplayDsPlayer> arcadeReplayDsPlayers = [];
            foreach (var replayPlayer in arcadeReplayPlayers)
            {
                var replayDsPlayer = new ArcadeReplayDsPlayer()
                {
                    Name = replayPlayer.Name,
                    SlotNumber = replayPlayer.SlotNumber,
                    Team = replayPlayer.Team,
                    Discriminator = replayPlayer.Discriminator,
                    PlayerResult = replayPlayer.PlayerResult,
                    PlayerId = ArcadePlayerIdDict[replayPlayer.ArcadePlayerId],
                    ArcadeReplayId = replayPlayer.ArcadeReplayId
                };
                arcadeReplayDsPlayers.Add(replayDsPlayer);
            }
            context.ArcadeReplayDsPlayers.AddRange(arcadeReplayDsPlayers);
            await context.SaveChangesAsync();
            skip += take;
            arcadeReplayPlayers = await context.ArcadeReplayPlayers
            .OrderBy(o => o.ArcadeReplayPlayerId)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            logger.LogInformation("skip: {skip}", skip);
        }
    }
}
