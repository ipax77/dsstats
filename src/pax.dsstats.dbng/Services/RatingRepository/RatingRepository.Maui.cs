using Microsoft.Data.Sqlite;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Generic;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    private async Task<UpdateResult> MauiUpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT OR REPLACE INTO PlayerRatings ({nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.RatingType)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGames)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.MmrOverTime)},{nameof(PlayerRating.Consistency)},{nameof(PlayerRating.Confidence)},{nameof(PlayerRating.IsUploader)},{nameof(PlayerRating.PlayerId)})
                VALUES ((SELECT {nameof(PlayerRating.PlayerRatingId)} from PlayerRatings where {nameof(PlayerRating.RatingType)} = $value1 AND {nameof(PlayerRating.PlayerId)} = $value13),$value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9,$value10,$value11,$value12,$value13)
            ";

        List<SqliteParameter> parameters = new List<SqliteParameter>();
        for (int i = 1; i <= 13; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$value{i}";
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

        await SetPlayerRatingPos();
        
        return new();
    }

    private async Task SetPlayerRatingPos()
    {
        using var connection = new SqliteConnection(Data.SqliteConnectionString);
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

    private async Task<int> MauiUpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        if (appendId == 0)
        {
            await DeleteReplayPlayerRatingsTable();
        }

        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
        $@"
                INSERT INTO ReplayPlayerRatings ({nameof(ReplayPlayerRating.ReplayPlayerRatingId)},{nameof(ReplayPlayerRating.MmrChange)},{nameof(ReplayPlayerRating.Pos)},{nameof(ReplayPlayerRating.ReplayPlayerId)},{nameof(ReplayPlayerRating.ReplayId)})
                VALUES ($value1,$value2,$value3,$value4,$value5)
            ";

        List<SqliteParameter> parameters = new List<SqliteParameter>();
        for (int i = 1; i <= 5; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$value{i}";
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

    private async Task DeleteReplayPlayerRatingsTable()
    {
        using var connection = new SqliteConnection(Data.SqliteConnectionString);
        await connection.OpenAsync();

        using var delCommand = new SqliteCommand("DELETE FROM ReplayPlayerRatings;", connection);
        await delCommand.ExecuteNonQueryAsync();
    }
}
