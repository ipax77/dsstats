﻿using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Extensions;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    public async Task CheckPlayers()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int total = 1000;

        var players = await context.Players
            .OrderBy(o => o.PlayerId)
            .Take(total)
            .AsNoTracking()
            .ToListAsync();

        int notFound = 0;
        int good = 0;
        int multiple = 0;

        foreach (var player in players)
        {
            var arcadePlayers = await context.ArcadePlayers
                .Where(x => x.ProfileId == player.ToonId
                    && x.RealmId == player.RealmId
                    && x.RegionId == player.RegionId)
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

        logger.LogWarning($"Player check: NotFound: {notFound}/{total}, Multiple: {multiple}/{total}, Good: {good}/{total}");
    }

    public async Task CheckReplays()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int total = 1000;

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => (x.GameMode == pax.dsstats.shared.GameMode.Commanders || x.GameMode == pax.dsstats.shared.GameMode.Standard) && x.Playercount == 6 && x.Duration > 300)
            .OrderByDescending(o => o.GameTime)
            .Skip(5000)
            .Take(total)
            .AsNoTracking()
            .ToListAsync();

        int good = 0;
        int notFound = 0;
        int multiple = 0;
        List<int> diff = new();

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
                var arcadeReplay = arcadeReplays.First();
                diff.Add((int)(replay.GameTime - arcadeReplay.CreatedAt.AddSeconds(arcadeReplay.Duration)).TotalSeconds);
            }
            else
            {
                multiple++;
            }
        }
        logger.LogWarning($"Replay check: NotFound: {notFound}/{total}, Multiple: {multiple}/{total}, Good: {good}/{total}");
        logger.LogWarning($"diff: {diff.Average()} - max {diff.Max()} min: {diff.Min()}");
    }

    public void SetReplaysHash()
    {
        DateTime startDate = new DateTime(2021, 1, 1);
        DateTime endDate = startDate.AddMonths(3);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        while (endDate < DateTime.Today.AddDays(2))
        {
            var replays = context.ArcadeReplays
                .Include(i => i.ArcadeReplayPlayers)
                    .ThenInclude(i => i.ArcadePlayer)
                .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.ArcadeReplayId)
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate)
                .ToList();

            foreach (var replay in replays)
            {
                replay.GenHash(md5);
            }

            context.SaveChanges();

            startDate = endDate;
            endDate = endDate.AddMonths(3);

            logger.LogInformation($"Settings hashes for {startDate.ToShortDateString()} - {endDate.ToShortDateString()}");
        }
    }
}
