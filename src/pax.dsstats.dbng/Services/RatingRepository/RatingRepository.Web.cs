
using MySqlConnector;
using pax.dsstats.shared;
using pax.dsstats;

namespace pax.dsstats.dbng.Services;
public partial class RatingRepository
{
    private async Task<UpdateResult> MysqlUpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        //command.CommandText =
        //    $@"
        //        INSERT INTO PlayerRatings ({nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.RatingType)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGames)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.MmrOverTime)},{nameof(PlayerRating.Consistency)},{nameof(PlayerRating.Confidence)},{nameof(PlayerRating.IsUploader)},{nameof(PlayerRating.PlayerId)})
        //        VALUES ((SELECT {nameof(PlayerRating.PlayerRatingId)} from PlayerRatings where {nameof(PlayerRating.RatingType)} = @value1 AND {nameof(PlayerRating.PlayerId)} = @value13),@value1,@value2,@value3,@value4,@value5,@value6,@value7,@value8,@value9,@value10,@value11,@value12,@value13)
        //        ON DUPLICATE KEY UPDATE {nameof(PlayerRating.Rating)}=@value3,{nameof(PlayerRating.Games)}=@value4,{nameof(PlayerRating.Wins)}=@value5,{nameof(PlayerRating.Mvp)}=@value6,{nameof(PlayerRating.TeamGames)}=@value7,{nameof(PlayerRating.MainCount)}=@value8,{nameof(PlayerRating.Main)}=@value9,{nameof(PlayerRating.MmrOverTime)}=""@value10"",{nameof(PlayerRating.Consistency)}=@value11,{nameof(PlayerRating.Confidence)}=@value12,{nameof(PlayerRating.IsUploader)}=@value13
        //    ";
        command.CommandText =
            $@"
                INSERT INTO PlayerRatings ({nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.RatingType)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGames)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.MmrOverTime)},{nameof(PlayerRating.Consistency)},{nameof(PlayerRating.Confidence)},{nameof(PlayerRating.IsUploader)},{nameof(PlayerRating.PlayerId)})
                VALUES ((SELECT t.{nameof(PlayerRating.PlayerRatingId)} FROM (SELECT * from PlayerRatings where {nameof(PlayerRating.RatingType)} = @value1 AND {nameof(PlayerRating.PlayerId)} = @value13) as t),@value1,@value2,@value3,@value4,@value5,@value6,@value7,@value8,@value9,@value10,@value11,@value12,@value13)
                ON DUPLICATE KEY UPDATE {nameof(PlayerRating.Rating)}=@value2,{nameof(PlayerRating.Games)}=@value3,{nameof(PlayerRating.Wins)}=@value4,{nameof(PlayerRating.Mvp)}=@value5,{nameof(PlayerRating.TeamGames)}=@value6,{nameof(PlayerRating.MainCount)}=@value7,{nameof(PlayerRating.Main)}=@value8,{nameof(PlayerRating.MmrOverTime)}=@value9,{nameof(PlayerRating.Consistency)}=@value10,{nameof(PlayerRating.Confidence)}=@value11,{nameof(PlayerRating.IsUploader)}=@value12
            ";
        command.Transaction = transaction;

        List<MySqlParameter> parameters = new List<MySqlParameter>();
        for (int i = 1; i <= 13; i++)
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
                parameters[8].Value = GetDbMmrOverTime(calcEnt.MmrOverTime);
                parameters[9].Value = calcEnt.Consistency;
                parameters[10].Value = calcEnt.Confidence;
                parameters[11].Value = calcEnt.IsUploader;
                parameters[12].Value = calcEnt.PlayerId;
                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();

        await SetPlayerRatingsPos();
        return new();
    }

    public async Task<int> MysqlUpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        if (appendId == 0)
        {
            await DeleteMyqlReplayPlayerRatingsTable();
        }

        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.CommandText =
            $@"
                INSERT INTO ReplayPlayerRatings ({nameof(ReplayPlayerRating.ReplayPlayerRatingId)},{nameof(ReplayPlayerRating.MmrChange)},{nameof(ReplayPlayerRating.Pos)},{nameof(ReplayPlayerRating.ReplayPlayerId)},{nameof(ReplayPlayerRating.ReplayId)})
                VALUES (@value1,@value2,@value3,@value4,@value5)
            ";
        command.Transaction = transaction;

        List<MySqlParameter> parameters = new List<MySqlParameter>();
        for (int i = 1; i <= 5; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@value{i}";
            command.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        for (int i = 0; i < replayPlayerMmrChanges.Count; i++)
        {
            for (int j = 0; j < replayPlayerMmrChanges[i].Changes.Count; j++)
            {
                appendId++;
                parameters[0].Value = appendId;
                parameters[1].Value = replayPlayerMmrChanges[i].Changes[j].Change;
                parameters[2].Value = replayPlayerMmrChanges[i].Changes[j].Pos;
                parameters[3].Value = replayPlayerMmrChanges[i].Changes[j].ReplayPlayerId;
                parameters[4].Value = replayPlayerMmrChanges[i].ReplayId;
                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();
        return appendId;
    }

    private async Task DeleteMyqlReplayPlayerRatingsTable()
    {
        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        using var delCommand = new MySqlCommand("TRUNCATE ReplayPlayerRatings;", connection);
        await delCommand.ExecuteNonQueryAsync();
    }
}
