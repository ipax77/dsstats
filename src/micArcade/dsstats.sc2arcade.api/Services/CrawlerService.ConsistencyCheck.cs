using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace dsstats.sc2arcade.api.Services;

public partial class CrawlerService
{
    public async Task CheckPlayers()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var players = await context.Players
            .OrderBy(o => o.PlayerId)
            .Take(100)
            .ToListAsync();

        int notFound = 0;
        int good = 0;
        int multiple = 0;

        foreach (var player in players)
        {
            var arcadePlayers = await context.ArcadePlayers
                .Where(x => x.ProfileId == player.ToonId)
                .ToListAsync();

            if (arcadePlayers.Count == 0)
            {
                notFound++;
            }
            else if (arcadePlayers.Count == 1 && arcadePlayers.First().RegionId == player.RegionId)
            {
                good++;
            }
            else
            {
                multiple++;
            }
        }

        logger.LogWarning($"Player check: NotFound: {notFound}, Multiple: {multiple}, Good: {good}");
    }
}
