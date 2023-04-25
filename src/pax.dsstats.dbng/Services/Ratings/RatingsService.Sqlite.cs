
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    private async Task<UpdateResult> UpdateSqlitePlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings, bool recalc)
    {
        if (recalc)
        {
            await DeletePlayerRatingsTable();
        }

        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT OR REPLACE INTO {nameof(ReplayContext.PlayerRatings)} 
                    ({nameof(PlayerRating.PlayerRatingId)},
                     {nameof(PlayerRating.RatingType)},
                     {nameof(PlayerRating.Pos)},
                     {nameof(PlayerRating.Rating)},
                     {nameof(PlayerRating.Games)},
                     {nameof(PlayerRating.Wins)},
                     {nameof(PlayerRating.Mvp)},
                     {nameof(PlayerRating.TeamGames)},
                     {nameof(PlayerRating.MainCount)},
                     {nameof(PlayerRating.Main)},
                     {nameof(PlayerRating.Consistency)},
                     {nameof(PlayerRating.Confidence)},
                     {nameof(PlayerRating.IsUploader)},
                     {nameof(PlayerRating.PlayerId)})
                VALUES (
                    (SELECT {nameof(PlayerRating.PlayerRatingId)} from {nameof(ReplayContext.PlayerRatings)}
                     where {nameof(PlayerRating.RatingType)} = $value1 AND {nameof(PlayerRating.PlayerId)} = $value13),
                     $value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9,$value10,$value11,$value12,$value13)
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
                
                parameters[0].Value = (int)ent.Key; // Rating
                parameters[1].Value = 0;            // Pos
                parameters[2].Value = calcEnt.Mmr;
                parameters[3].Value = calcEnt.Games;
                parameters[4].Value = calcEnt.Wins;
                parameters[5].Value = calcEnt.Mvp;
                parameters[6].Value = calcEnt.TeamGames;
                parameters[7].Value = main.Value;
                parameters[8].Value = (int)main.Key;
                parameters[9].Value = calcEnt.Consistency;
                parameters[10].Value = calcEnt.Confidence;
                parameters[11].Value = calcEnt.IsUploader;
                parameters[12].Value = calcEnt.PlayerId;
                await command.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();

        await SetSqlitePlayerRatingPos();
        await SetSqliteRatingChange();

        return new();
    }

    private async Task SetSqlitePlayerRatingPos()
    {
        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
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

    private async Task SetSqliteRatingChange()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Dictionary<int, PlayerRatingChange> ratingChanges = new();

        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            foreach (RatingChangeTimePeriod timePeriod in Enum.GetValues(typeof(RatingChangeTimePeriod)))
            {
                if (timePeriod == RatingChangeTimePeriod.None)
                {
                    continue;
                }

                RatingChangesRequest request = new()
                {
                    RatingType = ratingType,
                    TimePeriod = timePeriod
                };

                var fromDate = GetRatingChangesFromDate(request.TimePeriod);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var statsQuery = from r in context.Replays
                                 from rr in context.ReplayPlayers.Where(x => x.ReplayId == r.ReplayId)
                                 from pr in rr.ReplayPlayerRatingInfo.ReplayPlayer.Player.PlayerRatings
                                 where r.GameTime > fromDate
                                   && pr.RatingType == request.RatingType
                                 group new { rr.ReplayPlayerRatingInfo.ReplayPlayer.Player, pr, rr }
                                   by new { rr.ReplayPlayerRatingInfo.ReplayPlayer.Player.PlayerId, pr.PlayerRatingId }
                            into g
                                 where g.Count() > GetRatingChangeLimit(request.TimePeriod)
                                 select new
                                 {
                                     g.Key.PlayerId,
                                     g.Key.PlayerRatingId,
                                     RatingChange = MathF.Round(g.Sum(s => s.rr.ReplayPlayerRatingInfo.RatingChange), 2)
                                 };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                var stats = await statsQuery.ToListAsync();

                foreach (var stat in stats)
                {
                    if (!ratingChanges.TryGetValue(stat.PlayerRatingId, out PlayerRatingChange? change))
                    {
                        change = ratingChanges[stat.PlayerRatingId] = new PlayerRatingChange()
                        {
                            PlayerRatingId = stat.PlayerRatingId
                        };
                    }

                    if (request.TimePeriod == RatingChangeTimePeriod.Past24h)
                    {
                        change.Change24h = stat.RatingChange;
                    }
                    else if (request.TimePeriod == RatingChangeTimePeriod.Past10Days)
                    {
                        change.Change10d = stat.RatingChange;
                    }
                    else
                    {
                        change.Change30d = stat.RatingChange;
                    }
                }
            }
        }

        await DeletePlayerRatingChangesTable();
        if (ratingChanges.Any())
        {
            context.PlayerRatingChanges.AddRange(ratingChanges.Values);
            await context.SaveChangesAsync();
        }
    }

    private async Task<(int, int)> UpdateSqliteMmrChanges(List<ReplayRatingDto> replayRatingDtos, int replayAppendId, int playerAppendId)
    {
        var newReplayAppendId = await UpdateSqliteReplayRatings(replayRatingDtos, replayAppendId);
        playerAppendId = await UpdateSqliteRepPlayerRatings(replayRatingDtos, replayAppendId, playerAppendId);

        return (newReplayAppendId, playerAppendId);
    }

    private async Task<int> UpdateSqliteReplayRatings(List<ReplayRatingDto> replayRatingDtos, int replayRatingAppendId)
    {
        if (replayRatingAppendId == 0)
        {
            await DeleteReplayRatingsTable();
        }

        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT INTO {nameof(ReplayContext.ReplayRatings)} ({nameof(ReplayRating.ReplayRatingId)}, {nameof(ReplayRating.RatingType)},{nameof(ReplayRating.LeaverType)},{nameof(ReplayRating.ExpectationToWin)},{nameof(ReplayRating.ReplayId)})
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

        for (int i = 0; i < replayRatingDtos.Count; i++)
        {
            replayRatingAppendId++;
            var replayRatingDto = replayRatingDtos[i];

            parameters[0].Value = replayRatingAppendId;
            parameters[1].Value = (int)replayRatingDto.RatingType;
            parameters[2].Value = (int)replayRatingDto.LeaverType;
            parameters[3].Value = replayRatingDto.ExpectationToWin;
            parameters[4].Value = replayRatingDto.ReplayId;
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();

        return replayRatingAppendId;
    }

    private async Task<int> UpdateSqliteRepPlayerRatings(List<ReplayRatingDto> replayRatingDtos, int replayRatingAppendId, int repPlayerRatingAppendId)
    {
        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText =
            $@"
                INSERT INTO {nameof(ReplayContext.RepPlayerRatings)} ({nameof(RepPlayerRating.RepPlayerRatingId)},{nameof(RepPlayerRating.GamePos)},{nameof(RepPlayerRating.Rating)},{nameof(RepPlayerRating.RatingChange)},{nameof(RepPlayerRating.Games)},{nameof(RepPlayerRating.Consistency)},{nameof(RepPlayerRating.Confidence)},{nameof(RepPlayerRating.ReplayPlayerId)},{nameof(RepPlayerRating.ReplayRatingInfoId)})
                VALUES ($value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9)
            ";

        List<SqliteParameter> parameters = new List<SqliteParameter>();
        for (int i = 1; i <= 9; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"$value{i}";
            command.Parameters.Add(parameter);
            parameters.Add(parameter);
        }

        for (int i = 0; i < replayRatingDtos.Count; i++)
        {
            var replayRatingDto = replayRatingDtos[i];
            replayRatingAppendId++;

            for (int j = 0; j < replayRatingDto.RepPlayerRatings.Count; j++)
            {
                var repPlayerRatingDto = replayRatingDto.RepPlayerRatings[j];

                repPlayerRatingAppendId++;
                parameters[0].Value = repPlayerRatingAppendId;
                parameters[1].Value = repPlayerRatingDto.GamePos;
                parameters[2].Value = repPlayerRatingDto.Rating;
                parameters[3].Value = repPlayerRatingDto.RatingChange;
                parameters[4].Value = repPlayerRatingDto.Games;
                parameters[5].Value = repPlayerRatingDto.Consistency;
                parameters[6].Value = repPlayerRatingDto.Confidence;
                parameters[7].Value = repPlayerRatingDto.ReplayPlayerId;
                parameters[8].Value = replayRatingAppendId;
                await command.ExecuteNonQueryAsync();
            }
        }
        await transaction.CommitAsync();

        return repPlayerRatingAppendId;
    }

    private async Task DeletePlayerRatingsTable()
    {
        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var delCommand = new SqliteCommand($"DELETE FROM {nameof(ReplayContext.PlayerRatings)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    private async Task DeleteReplayRatingsTable()
    {
        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var delCommand = new SqliteCommand($"DELETE FROM {nameof(ReplayContext.ReplayRatings)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    private async Task DeletePlayerRatingChangesTable()
    {
        using var connection = new SqliteConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        using var delCommand = new SqliteCommand($"DELETE FROM {nameof(ReplayContext.PlayerRatingChanges)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    public static DateTime GetRatingChangesFromDate(RatingChangeTimePeriod timePeriod)
    {
        DateTime now = DateTime.UtcNow;
        DateTime today = new DateTime(now.Year, now.Month, now.Day);
        return timePeriod switch
        {
            RatingChangeTimePeriod.Past24h => now.AddHours(-24),
            RatingChangeTimePeriod.Past10Days => today.AddDays(-10),
            _ => today.AddDays(-30)
        };
    }

    private static int GetRatingChangeLimit(RatingChangeTimePeriod timePeriod)
    {
        return timePeriod switch
        {
            RatingChangeTimePeriod.Past24h => 2,
            RatingChangeTimePeriod.Past10Days => 5,
            _ => 10
        };
    }
}