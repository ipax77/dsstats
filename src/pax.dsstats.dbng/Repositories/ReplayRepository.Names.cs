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
        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
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
}
