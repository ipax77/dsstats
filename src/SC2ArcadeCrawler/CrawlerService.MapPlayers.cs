using dsstats.db8;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using dsstats.shared;

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
                PlayerId dsplayerId = new(replayDsPlayer.Player.ToonId, replayDsPlayer.Player.RealmId, replayDsPlayer.Player.RegionId);
                int playerId = await importService.GetPlayerIdAsync(dsplayerId,
                replayDsPlayer.Name);
                replayDsPlayer.Player = null;
                replayDsPlayer.PlayerId = playerId;
            }
        }
    }
}
