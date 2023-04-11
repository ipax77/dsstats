using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsMergeService
{
    public async Task CheckReplays()
    {
        DateTime fromTime = DateTime.Today.AddMonths(-3);
        DateTime toTime = DateTime.Today.AddMonths(-2);

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => (x.GameMode == pax.dsstats.shared.GameMode.Commanders || x.GameMode == pax.dsstats.shared.GameMode.Standard)
                && x.Playercount == 6
                && x.Duration > 300
                && x.TournamentEdition == false
                && x.GameTime >= fromTime
                && x.GameTime < toTime)
            .OrderByDescending(o => o.GameTime)
            .AsNoTracking()
            .ToListAsync();

        int good = 0;
        int notFound = 0;
        int multiple = 0;

        foreach (var replay in replays)
        {
            var toonIds = replay.ReplayPlayers.Select(s => s.Player.ToonId).ToList();

            var arcadeReplays = await context.ArcadeReplays
                .Where(x => x.GameMode == replay.GameMode)
                .Where(x => x.CreatedAt > replay.GameTime.AddHours(-12) && x.CreatedAt < replay.GameTime.AddHours(12))
                .Where(x => x.ArcadeReplayPlayers.All(a => toonIds.Contains(a.ArcadePlayer.ProfileId)))
                .ToListAsync();

            if (arcadeReplays.Count == 0)
            {
                notFound++;
            }
            else if (arcadeReplays.Count == 1)
            {
                good++;
            }
            else
            {
                multiple++;
            }
        }
        logger.LogWarning($"Replay check: NotFound: {notFound}/{replays.Count}, Multiple: {multiple}/{replays.Count}, Good: {good}/{replays.Count}");
    }
}
