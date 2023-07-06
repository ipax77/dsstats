using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository
{
    public async Task FixPlayerNames()
    {
        Stopwatch sw = Stopwatch.StartNew();
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandTimeout = 120;
        command.CommandText =
            $@"
                UPDATE {nameof(ReplayContext.Players)} as p
                SET Name = (
                        SELECT rp.{nameof(ReplayPlayer.Name)}
                        FROM {nameof(ReplayContext.ReplayPlayers)} as rp
                        INNER JOIN {nameof(ReplayContext.Replays)} AS r on r.{nameof(Replay.ReplayId)} = rp.{nameof(ReplayPlayer.ReplayId)}
                        WHERE p.{nameof(Player.PlayerId)} = rp.{nameof(ReplayPlayer.PlayerId)}
                        ORDER BY r.{nameof(Replay.GameTime)} DESC
                        LIMIT 1
                 )
                WHERE EXISTS(
                    SELECT rp.{nameof(ReplayPlayer.ReplayPlayerId)}
                    FROM {nameof(ReplayContext.ReplayPlayers)} as rp
                    WHERE rp.{nameof(ReplayPlayer.PlayerId)} = p.{nameof(Player.PlayerId)}
                );
            ";

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed fixing PlayerNames: {ex.Message}");
        }
        sw.Stop();
        logger.LogWarning($"Player names fixed in {sw.ElapsedMilliseconds}");
    }

    public async Task FixArcadePlayerNames()
    {
        var fromDate = DateTime.Today.AddDays(-6);

        Stopwatch sw = Stopwatch.StartNew();

        var replays = await context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
                .ThenInclude(i => i.ArcadePlayer)
            .Where(x => x.CreatedAt > fromDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        if (!replays.Any())
        {
            return;
        }

        Dictionary<int, string> playersDone = new();

        foreach (var replay in replays)
        {
            foreach (var replayPlayer in replay.ArcadeReplayPlayers)
            {
                if (playersDone.ContainsKey(replayPlayer.ArcadePlayer.ArcadePlayerId))
                {
                    continue;
                }

                if (replayPlayer.Name != replayPlayer.ArcadePlayer.Name)
                {
                    replayPlayer.ArcadePlayer.Name = replayPlayer.Name;
                    playersDone[replayPlayer.ArcadePlayer.ArcadePlayerId] = replayPlayer.Name;
                }
                else
                {
                    playersDone[replayPlayer.ArcadePlayer.ArcadePlayerId] = replayPlayer.ArcadePlayer.Name;
                }
            }
        }
        int count = await context.SaveChangesAsync();
        sw.Stop();
        logger.LogWarning($"Arcade {count} player names fixed in {sw.ElapsedMilliseconds}ms");
    }
}
