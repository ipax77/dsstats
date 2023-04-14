using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

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
            var arcadePlayers = await context.ArcadePlayers
                .Where(x => x.ProfileId == player.ToonId
                    && x.RealmId == player.RealmId
                    && x.RegionId == player.RegionId)
                .ToListAsync();

            if (arcadePlayers.Count == 0)
            {
                notFound++;
            }
            else if (arcadePlayers.Count == 1)
            {
                if (player.Name == arcadePlayers.First().Name)
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

    public async Task CheckReplays()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Random random = new();

        int total = 1000;

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => (x.GameMode == pax.dsstats.shared.GameMode.Commanders || x.GameMode == pax.dsstats.shared.GameMode.Standard) && x.Playercount == 6 && x.Duration > 300)
            .OrderByDescending(o => o.GameTime)
            .Skip(random.Next(1000, 10000))
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
                .Where(x => x.CreatedAt > replay.GameTime.AddHours(-6) && x.CreatedAt < replay.GameTime.AddHours(6))
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
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int skip = 0;
        int take = 10000;

        var replays = context.ArcadeReplays
                .Include(i => i.ArcadeReplayPlayers)
                    .ThenInclude(i => i.ArcadePlayer)
                .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.ArcadeReplayId)
                .Where(x => String.IsNullOrEmpty(x.ReplayHash))
                .Skip(skip)
                .Take(take)
                .ToList();

        while (replays.Any())
        {
            foreach (var replay in replays)
            {
                replay.GenHash(md5);
                Console.WriteLine($"hash: {replay.ReplayHash}");
            }
            context.SaveChanges();

            skip += take;

            replays = context.ArcadeReplays
                            .Include(i => i.ArcadeReplayPlayers)
                                .ThenInclude(i => i.ArcadePlayer)
                            .OrderBy(o => o.CreatedAt)
                                .ThenBy(o => o.ArcadeReplayId)
                            .Where(x => String.IsNullOrEmpty(x.ReplayHash))
                            .Skip(skip)
                            .Take(take)
                            .ToList();
        }
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
                .Include(i => i.ArcadeReplayPlayers)
                .OrderBy(o => o.CreatedAt)
                    .ThenBy(o => o.ArcadeReplayId)
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate)
                .ToList();

            foreach (var replay in replays)
            {
                foreach (var rp in replay.ArcadeReplayPlayers.Where(x => x.Team != replay.WinnerTeam))
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

    public async Task FixPlayerNames()
    {
        Stopwatch sw = Stopwatch.StartNew();
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandTimeout = 500;
        command.CommandText =
            $@"
                UPDATE {nameof(ReplayContext.ArcadePlayers)} as p
                SET Name = (
                        SELECT rp.{nameof(ArcadeReplayPlayer.Name)}
                        FROM {nameof(ReplayContext.ArcadeReplayPlayers)} as rp
                        INNER JOIN {nameof(ReplayContext.ArcadeReplays)} AS r on r.{nameof(ArcadeReplay.ArcadeReplayId)} = rp.{nameof(ArcadeReplayPlayer.ArcadeReplayId)}
                        WHERE p.{nameof(ArcadePlayer.ArcadePlayerId)} = rp.{nameof(ArcadeReplayPlayer.ArcadePlayerId)}
                        ORDER BY r.{nameof(ArcadeReplay.CreatedAt)} DESC
                        LIMIT 1
                 )
                WHERE EXISTS(
                    SELECT rp.{nameof(ArcadeReplayPlayer.ArcadeReplayPlayerId)}
                    FROM {nameof(ReplayContext.ArcadeReplayPlayers)} as rp
                    WHERE rp.{nameof(ArcadeReplayPlayer.ArcadePlayerId)} = p.{nameof(ArcadePlayer.ArcadePlayerId)}
                );
            ";

        int affectedRows = 0;
        try
        {
            affectedRows = await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed fixing PlayerNames: {ex.Message}");
        }
        sw.Stop();
        logger.LogWarning($"Player names fixed in {sw.ElapsedMilliseconds} ms ({affectedRows})");
    }
}
