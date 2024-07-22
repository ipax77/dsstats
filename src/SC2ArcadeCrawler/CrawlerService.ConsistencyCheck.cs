using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    public async Task CheckPlayers()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int total = 1000;
        Random random = new();

        var gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic, GameMode.Standard };

        var playersQuery = from r in context.Replays
                           from rp in r.ReplayPlayers
                           where gameModes.Contains(r.GameMode)
                            && r.Playercount == 6
                            && r.Duration > 300
                            && r.WinnerTeam > 0
                           select rp.Player;

        var players = await playersQuery
            .Distinct()
            .OrderBy(o => o.PlayerId)
            .Skip(random.Next(1000, 10000))
            .Take(1000)
            .ToListAsync();

        int notFound = 0;
        int good = 0;
        int verygood = 0;

        foreach (var player in players)
        {
            var Players = await context.Players
                .Where(x => x.ToonId == player.ToonId
                    && x.RealmId == player.RealmId
                    && x.RegionId == player.RegionId)
                .ToListAsync();

            if (Players.Count == 0)
            {
                notFound++;
            }
            else if (Players.Count == 1)
            {
                if (player.Name == Players.First().Name)
                {
                    verygood++;
                }
                else
                {
                    good++;
                }
            }
        }

        logger.LogWarning($"Player check: NotFound: {notFound}/{total}, verygood: {verygood}/{total}, Good: {good}/{total}");
    }

    public void FixPlayerResults()
    {
        DateTime startDate = new DateTime(2021, 1, 1);
        DateTime endDate = startDate.AddMonths(3);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        while (endDate < DateTime.Today.AddDays(2))
        {
            var replays = context.ArcadeReplays
                .Include(i => i.ArcadeReplayDsPlayers)
                .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.ArcadeReplayId)
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate)
                .ToList();

            foreach (var replay in replays)
            {
                foreach (var rp in replay.ArcadeReplayDsPlayers.Where(x => x.Team != replay.WinnerTeam))
                {
                    rp.PlayerResult = PlayerResult.Los;
                }
            }

            context.SaveChanges();

            startDate = endDate;
            endDate = endDate.AddMonths(3);

            logger.LogInformation($"Fixing playerresults for {startDate.ToShortDateString()} - {endDate.ToShortDateString()}");
        }
    }
}
