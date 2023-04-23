using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Ratings;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    private async Task UpdatePlayerRatings(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT INTO PlayerRatings ({nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.RatingType)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGames)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.Consistency)},{nameof(PlayerRating.Confidence)},{nameof(PlayerRating.IsUploader)},{nameof(PlayerRating.PlayerId)})
                VALUES ((SELECT t.{nameof(PlayerRating.PlayerRatingId)} FROM (SELECT * from PlayerRatings where {nameof(PlayerRating.RatingType)} = @value1 AND {nameof(PlayerRating.PlayerId)} = @value12) as t),@value1,@value2,@value3,@value4,@value5,@value6,@value7,@value8,@value9,@value10,@value11,@value12)
                ON DUPLICATE KEY UPDATE {nameof(PlayerRating.Rating)}=@value2,{nameof(PlayerRating.Games)}=@value3,{nameof(PlayerRating.Wins)}=@value4,{nameof(PlayerRating.Mvp)}=@value5,{nameof(PlayerRating.TeamGames)}=@value6,{nameof(PlayerRating.MainCount)}=@value7,{nameof(PlayerRating.Main)}=@value8,{nameof(PlayerRating.Consistency)}=@value9,{nameof(PlayerRating.Confidence)}=@value10,{nameof(PlayerRating.IsUploader)}=@value11
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
                var main = calcEnt.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();

                parameters[0].Value = (int)ent.Key;
                parameters[1].Value = calcEnt.Mmr;
                parameters[2].Value = calcEnt.Games;
                parameters[3].Value = calcEnt.Wins;
                parameters[4].Value = calcEnt.Mvp;
                parameters[5].Value = calcEnt.TeamGames;
                parameters[6].Value = main.Value;
                parameters[7].Value = (int)main.Key;
                parameters[8].Value = calcEnt.Consistency;
                parameters[9].Value = calcEnt.Confidence;
                parameters[10].Value = calcEnt.IsUploader;
                parameters[11].Value = calcEnt.PlayerId;
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();
    }

    private async Task ContinueReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayRatings.csv";
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
            INTO TABLE {nameof(ReplayContext.ReplayRatings)}
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
        var csvFile = $"{csvBasePath}/ReplayPlayerRatings.csv";
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
            INTO TABLE {nameof(ReplayContext.RepPlayerRatings)}
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
