
using dsstats.db8;
using dsstats.shared;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data.SQLite;

namespace dsstats.db8services;

public partial class CalcRepository
{
    public async Task SetRatingChange(RatingCalcType ratingCalcType)
    {
        if (ratingCalcType == RatingCalcType.Combo)
        {
            return;
        }

        logger.LogInformation("Setting rating changes {type}", ratingCalcType.ToString());

        var commandText = ratingCalcType switch
        {
            RatingCalcType.Dsstats => "CALL SetRatingChange();",
            RatingCalcType.Arcade => "CALL SetArcadeRatingChange();",
            _ => throw new NotImplementedException()
        };

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 240;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetPlayerRatingsPos(RatingCalcType ratingCalcType)
    {
        var commandText = ratingCalcType switch
        {
            RatingCalcType.Dsstats => "CALL SetPlayerRatingPos();",
            RatingCalcType.Arcade => "CALL SetArcadePlayerRatingPos();",
            RatingCalcType.Combo => "CALL SetComboPlayerRatingPos();",
            _ => throw new NotImplementedException()
        };

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 240;
        await command.ExecuteNonQueryAsync();
    }

    private async Task SetSqlitePlayerRatingPos()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            var command = connection.CreateCommand();

            command.CommandText =
            $@"
                UPDATE {nameof(ReplayContext.PlayerRatings)} as c
                SET {nameof(PlayerRating.Pos)} = c2.rn
                FROM(SELECT c2.*, row_number() OVER(ORDER BY {nameof(PlayerRating.Rating)} DESC, {nameof(PlayerRating.PlayerId)}) AS rn
                FROM {nameof(ReplayContext.PlayerRatings)} as c2 WHERE c2.{nameof(PlayerRating.RatingType)} = {(int)ratingType}) c2
                WHERE c.{nameof(PlayerRating.RatingType)} = {(int)ratingType} AND c.{nameof(PlayerRating.PlayerRatingId)} = c2.{nameof(PlayerRating.PlayerRatingId)};
            ";

            await command.ExecuteNonQueryAsync();
        }
    }
}