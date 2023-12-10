using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using System.Data.SQLite;

namespace dsstats.maui8.Services;

internal class RatingsSaveService : IRatingsSaveService
{
    private readonly string connectionString;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<RatingsSaveService> logger;

    public RatingsSaveService(IServiceScopeFactory scopeFactory, ILogger<RatingsSaveService> logger)
    {
        var scope = scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        connectionString = options.Value.ImportConnectionString;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public Task SaveArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans)
    {
        throw new NotImplementedException();
    }

    public Task<(int, int)> SaveArcadeStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0)
    {
        throw new NotImplementedException();
    }

    public Task SaveComboPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans)
    {
        throw new NotImplementedException();
    }

    public Task<(int, int)> SaveComboStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0)
    {
        throw new NotImplementedException();
    }

    public async Task SaveDsstatsPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans, bool isContinue)
    {
        await UpdateSqlitePlayers(mmrIdRatings, !isContinue);
        await SetSqlitePlayerRatingPos();
        await SetSqliteRatingChange();
    }

    public async Task<(int, int)> SaveDsstatsStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0)
    {
        int repId = await UpdateSqliteReplayRatings(ratings, replayRatingId);
        int repPlId =  await UpdateSqliteRepPlayerRatings(ratings, replayRatingId, replayPlayerRatingId);
        return (repId, repPlId);
    }

    private async Task UpdateSqlitePlayers(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, bool isRecalc)
    {
        if (isRecalc)
        {
            await DeletePlayerRatingsTable();
        }

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var playerIdDic = (await context.Players
            .Select(s => new { PlayerId = new PlayerId(s.ToonId, s.RealmId, s.RegionId), Id = s.PlayerId })
            .ToListAsync())
            .ToDictionary(k => k.PlayerId, v => v.Id);

        try
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

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
                     {nameof(PlayerRating.PlayerId)},
                     {nameof(PlayerRating.ArcadeDefeatsSinceLastUpload)})
                VALUES (
                    (SELECT {nameof(PlayerRating.PlayerRatingId)} from {nameof(ReplayContext.PlayerRatings)}
                     where {nameof(PlayerRating.RatingType)} = $value1 AND {nameof(PlayerRating.PlayerId)} = $value13),
                     $value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9,$value10,$value11,$value12,$value13,0)
            
        ";

            List<SQLiteParameter> parameters = new();
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
                    if (!playerIdDic.TryGetValue(calcEnt.PlayerId, out var playerId))
                    {
                        continue;
                    }

                    var main = calcEnt.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();

                    parameters[0].Value = (int)ent.Key; // Rating
                    parameters[1].Value = 0;            // Pos
                    parameters[2].Value = calcEnt.Mmr;
                    parameters[3].Value = calcEnt.Games;
                    parameters[4].Value = calcEnt.Wins;
                    parameters[5].Value = calcEnt.Mvps;
                    parameters[6].Value = 0;
                    parameters[7].Value = main.Value;
                    parameters[8].Value = (int)main.Key;
                    parameters[9].Value = calcEnt.Consistency;
                    parameters[10].Value = calcEnt.Confidence;
                    parameters[11].Value = calcEnt.IsUploader;
                    parameters[12].Value = playerId;
                    await command.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed updating player ratings: {error}", ex.Message);
        }
    }

    private async Task<int> UpdateSqliteReplayRatings(List<shared.Calc.ReplayRatingDto> replayRatingDtos, int replayRatingAppendId)
    {
        if (replayRatingAppendId == 0)
        {
            await DeleteReplayRatingsTable();
        }

        try
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
                $@"
                INSERT INTO {nameof(ReplayContext.ReplayRatings)} ({nameof(ReplayRating.ReplayRatingId)},
                                                                   {nameof(ReplayRating.RatingType)},
                                                                   {nameof(ReplayRating.LeaverType)},
                                                                   {nameof(ReplayRating.ExpectationToWin)},
                                                                   {nameof(ReplayRating.ReplayId)},
                                                                   {nameof(ReplayRating.AvgRating)},
                                                                   {nameof(ReplayRating.IsPreRating)})
                VALUES ($value1,$value2,$value3,$value4,$value5,$value6,0)
            ";

            List<SQLiteParameter> parameters = new List<SQLiteParameter>();
            for (int i = 1; i <= 6; i++)
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
                parameters[1].Value = replayRatingDto.RatingType;
                parameters[2].Value = replayRatingDto.LeaverType;
                parameters[3].Value = replayRatingDto.ExpectationToWin;
                parameters[4].Value = replayRatingDto.ReplayId;
                parameters[5].Value = Convert.ToInt32(replayRatingDto.RepPlayerRatings.Average(a => a.Rating));
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed inserting replay ratings: {error}", ex.Message);
        }
        return replayRatingAppendId;
    }

    private async Task<int> UpdateSqliteRepPlayerRatings(List<shared.Calc.ReplayRatingDto> replayRatingDtos, int replayRatingAppendId, int repPlayerRatingAppendId)
    {
        if (repPlayerRatingAppendId == 0)
        {
            await DeleteReplayPlayerRatingsTable();
        }

        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            $@"
                INSERT INTO {nameof(ReplayContext.RepPlayerRatings)} ({nameof(RepPlayerRating.RepPlayerRatingId)},{nameof(RepPlayerRating.GamePos)},{nameof(RepPlayerRating.Rating)},{nameof(RepPlayerRating.RatingChange)},{nameof(RepPlayerRating.Games)},{nameof(RepPlayerRating.Consistency)},{nameof(RepPlayerRating.Confidence)},{nameof(RepPlayerRating.ReplayPlayerId)},{nameof(RepPlayerRating.ReplayRatingInfoId)})
                VALUES ($value1,$value2,$value3,$value4,$value5,$value6,$value7,$value8,$value9)
            ";

        List<SQLiteParameter> parameters = new List<SQLiteParameter>();
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

    private async Task SetSqliteRatingChange()
    {
        Dictionary<int, PlayerRatingChange> ratingChanges = new();
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

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

                var limit = GetRatingChangeLimit(timePeriod);
                var fromDate = GetRatingChangesFromDate(timePeriod);
                var statsQuery = from r in context.Replays
                                 from rp in r.ReplayPlayers
                                 from pr in rp.Player.PlayerRatings
                                 join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                                 join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                                 join p in context.Players on rp.PlayerId equals p.PlayerId
                                 where r.GameTime > fromDate
                                   && rr.RatingType == ratingType
                                   && pr.RatingType == ratingType
                                 group new { rp, pr, rpr } by new { p.PlayerId, pr.PlayerRatingId }
                            into g
                                 where g.Count() > limit
                                 select new
                                 {
                                     g.Key.PlayerId,
                                     g.Key.PlayerRatingId,
                                     RatingChange = MathF.Round(g.Sum(s => s.rpr.RatingChange), 2)
                                 };
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

                    if (timePeriod == RatingChangeTimePeriod.Past24h)
                    {
                        change.Change24h = stat.RatingChange;
                    }
                    else if (timePeriod == RatingChangeTimePeriod.Past10Days)
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
        if (ratingChanges.Count != 0)
        {
            context.PlayerRatingChanges.AddRange(ratingChanges.Values);
            await context.SaveChangesAsync();
        }
    }

    private static DateTime GetRatingChangesFromDate(RatingChangeTimePeriod timePeriod)
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

    private async Task DeletePlayerRatingsTable()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var delCommand = new SQLiteCommand($"DELETE FROM {nameof(ReplayContext.PlayerRatings)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    private async Task DeleteReplayRatingsTable()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var delCommand = new SQLiteCommand($"DELETE FROM {nameof(ReplayContext.ReplayRatings)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    private async Task DeleteReplayPlayerRatingsTable()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var delCommand = new SQLiteCommand($"DELETE FROM {nameof(ReplayContext.RepPlayerRatings)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    private async Task DeletePlayerRatingChangesTable()
    {
        using var connection = new SQLiteConnection(connectionString);
        await connection.OpenAsync();

        using var delCommand = new SQLiteCommand($"DELETE FROM {nameof(ReplayContext.PlayerRatingChanges)};", connection);
        await delCommand.ExecuteNonQueryAsync();
    }

    public Task SaveContinueComboRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, List<shared.Calc.ReplayRatingDto> replayRatings)
    {
        throw new NotImplementedException();
    }
}
