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
}
