using MySqlConnector;
using pax.dsstats.shared.Ratings;
using pax.dsstats.shared;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Globalization;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class ArcadeRatingsService
{
    private async Task UpdatePlayerRatings(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT INTO {nameof(ReplayContext.ArcadePlayerRatings)} ({nameof(ArcadePlayerRating.ArcadePlayerRatingId)},{nameof(ArcadePlayerRating.RatingType)},{nameof(ArcadePlayerRating.Pos)},{nameof(ArcadePlayerRating.Rating)},{nameof(ArcadePlayerRating.Games)},{nameof(ArcadePlayerRating.Wins)},{nameof(ArcadePlayerRating.Mvp)},{nameof(ArcadePlayerRating.TeamGames)},{nameof(ArcadePlayerRating.MainCount)},{nameof(ArcadePlayerRating.Main)},{nameof(ArcadePlayerRating.MmrOverTime)},{nameof(ArcadePlayerRating.Deviation)},{nameof(ArcadePlayerRating.IsUploader)},{nameof(ArcadePlayerRating.ArcadePlayerId)})
                VALUES ((SELECT t.{nameof(ArcadePlayerRating.ArcadePlayerRatingId)} FROM (SELECT * from {nameof(ReplayContext.ArcadePlayerRatings)} where {nameof(ArcadePlayerRating.RatingType)} = @value1 AND {nameof(ArcadePlayerRating.ArcadePlayerId)} = @value12) as t),@value1,@value2,0,@value3,@value4,@value5,@value6,@value7,@value8,@value9,@value10,@value11,@value12)
                ON DUPLICATE KEY UPDATE {nameof(ArcadePlayerRating.Rating)}=@value2,{nameof(ArcadePlayerRating.Games)}=@value3,{nameof(ArcadePlayerRating.Wins)}=@value4,{nameof(ArcadePlayerRating.Mvp)}=@value5,{nameof(ArcadePlayerRating.TeamGames)}=@value6,{nameof(ArcadePlayerRating.MainCount)}=@value7,{nameof(ArcadePlayerRating.Main)}=@value8,{nameof(ArcadePlayerRating.MmrOverTime)}=@value9,{nameof(ArcadePlayerRating.Deviation)}=@value10,{nameof(ArcadePlayerRating.IsUploader)}=@value11
            ";

        command.Transaction = transaction;

        List<MySqlParameter> parameters = new List<MySqlParameter>();
        for (int i = 1; i <= 12; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@value{i}";
            command.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        foreach (var ent in mmrIdRatings)
        {
            foreach (var calcEnt in ent.Value.Values)
            {
                parameters[0].Value = (int)ent.Key;
                parameters[1].Value = calcEnt.Mmr;
                parameters[2].Value = calcEnt.Games;
                parameters[3].Value = calcEnt.Wins;
                parameters[4].Value = calcEnt.Mvp;
                parameters[5].Value = calcEnt.TeamGames;
                parameters[6].Value = 0;
                parameters[7].Value = 0;
                parameters[8].Value = RatingsCsvService.GetDbMmrOverTime(calcEnt.MmrOverTime);
                parameters[9].Value = calcEnt.Deviation;
                parameters[10].Value = 0;
                parameters[11].Value = calcEnt.PlayerId;
                command.CommandTimeout = 120;

                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();
    }

    public void LogCommand(MySqlCommand cmd)
    {
        string cmdText = cmd.CommandText;

        foreach (MySqlParameter p in cmd.Parameters)
        {
            cmdText = cmdText.Replace(p.ParameterName, Convert.ToString(p.Value, CultureInfo.InvariantCulture));
        }

        logger.LogWarning($"{cmdText}");
    }

    private async Task ContinueReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ArcadeReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.ArcadeReplayRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ContinueReplayPlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ArcadeReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
        $@"
            SET FOREIGN_KEY_CHECKS = 0;
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.ArcadeReplayRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }
}
