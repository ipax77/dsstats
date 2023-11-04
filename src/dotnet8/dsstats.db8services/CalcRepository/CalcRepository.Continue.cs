
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Calc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.db8services;

public partial class CalcRepository
{
    private async Task CleanupPreRatings(ReplayContext context)
    {
        var preRatings = await context.ReplayRatings
            .Include(i => i.RepPlayerRatings)
            .Where(x => x.IsPreRating)
            .ToListAsync();

        if (preRatings.Count == 0)
        {
            return;
        }

        context.ReplayRatings.RemoveRange(preRatings);
        await context.SaveChangesAsync();
    }

    public async Task CleanupComboPreRatings()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var preRatings = await context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .ToListAsync();

        if (preRatings.Count == 0)
        {
            return;
        }

        var replayIds = preRatings.Select(s => s.ReplayId).ToList();


        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where replayIds.Contains(r.ReplayId)
                    select rpr;

        var rprs = await query.ToListAsync();

        context.ComboReplayRatings.RemoveRange(preRatings);
        context.ComboReplayPlayerRatings.RemoveRange(rprs);
        await context.SaveChangesAsync();
    }

    public async Task<CalcRatingRequest?> GetDsstatsCalcRatingRequest()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        await CleanupPreRatings(context);

        CalcRatingRequest ratingRequest = new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            StarTime = new(2018, 1, 1),
            MmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            },
        };

        var latestRatingsProduced = await context.Replays
            .Where(x => x.ReplayRatingInfo != null)
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();

        // recalc
        if (latestRatingsProduced == default)
        {
            return ratingRequest;
        }

        var todoReplays = context.Replays
            .Where(x => x.Imported != null
                && x.Imported > latestRatingsProduced
                && x.GameTime >= latestRatingsProduced
                && x.ReplayRatingInfo == null)
            .Select(s => new { s.Imported, s.GameTime });

        var count = await todoReplays.CountAsync();

        // nothing to do
        if (count == 0)
        {
            return null;
        }

        // too mutch to do => recalc
        if (count > 100)
        {
            return ratingRequest;
        }

        ratingRequest.Continue = true;
        ratingRequest.StarTime = latestRatingsProduced;

        ratingRequest.ReplayPlayerRatingAppendId = await context.RepPlayerRatings
            .OrderByDescending(o => o.RepPlayerRatingId)
            .Select(s => s.RepPlayerRatingId)
            .FirstOrDefaultAsync();
        ratingRequest.ReplayRatingAppendId = await context.ReplayRatings
            .OrderByDescending(o => o.ReplayRatingId)
            .Select(s => s.ReplayRatingId)
            .FirstOrDefaultAsync();

        ratingRequest.MmrIdRatings = await GetCalcRatings(latestRatingsProduced, context);

        return ratingRequest;
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetCalcRatings(DateTime fromDate, ReplayContext context)
    {
        Dictionary<int, Dictionary<PlayerId, CalcRating>> calcRatings = new();
        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            calcRatings[(int)ratingType] = new();
        }


        var playersRatingsQuery = from r in context.Replays
                                  from rp in r.ReplayPlayers
                                  from pr in rp.Player.PlayerRatings
                                  where r.GameTime >= fromDate
                                   && r.ReplayRatingInfo == null
                                  select pr;
        var playerRatings = await playersRatingsQuery
            .Distinct()
            .Select(s => new
            {
                s.Player!.ToonId,
                s.Player.RealmId,
                s.Player.RegionId,
                s.Games,
                s.Wins,
                s.Mvp,
                s.Consistency,
                s.Confidence,
                s.RatingType,
                s.Rating,
                s.Main,
                s.MainCount,
                s.IsUploader
            })
            .ToListAsync();

        foreach (var pr in playerRatings)
        {
            PlayerId playerId = new(pr.ToonId, pr.RealmId, pr.RegionId);
            calcRatings[(int)pr.RatingType][playerId] = new()
            {
                PlayerId = playerId,
                Games = pr.Games,
                Wins = pr.Wins,
                Mmr = pr.Rating,
                Mvps = pr.Mvp,
                IsUploader = pr.IsUploader,
                Consistency = pr.Consistency,
                Confidence = pr.Confidence,
                CmdrCounts = GetFakeCmdrDic((Commander)pr.Main, pr.MainCount, pr.Games)
            };
        }
        return calcRatings;
    }

    public static Dictionary<Commander, int> GetFakeCmdrDic(Commander main, int mainCount, int games)
    {
        Dictionary<Commander, int> cmdrDic = new();

        var mainPercentage = mainCount * 100.0 / games;

        if (mainPercentage > 99)
        {
            cmdrDic.Add(main, games);
            return cmdrDic;
        }

        if ((int)main <= 3)
        {
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.Std).Where(x => x != main))
            {
                cmdrDic[cmdr] = games / 3;
            }
        }
        else
        {
            int total = Data.GetCommanders(Data.CmdrGet.NoStd).Count;
            int avg = (games - mainCount) / (total - 1);
            foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoStd).Where(x => x != main))
            {
                cmdrDic[cmdr] = avg;
            }
        }

        cmdrDic[main] = (int)(((games - mainCount) * mainPercentage) / (100.0 - mainPercentage));
        return cmdrDic;
    }

    public async Task StoreDsstatsResult(CalcRatingResult result, bool isContinue)
    {
        if (IsSqlite)
        {
            await WriteRatingsToSqlite(result.MmrIdRatings,
                                          result.DsstatsRatingDtos,
                                          result.ReplayRatingAppendId,
                                          result.ReplayPlayerRatingAppendId,
                                          true);
        }
        else
        {
            if (isContinue)
            {
                await UpdatePlayerRatings(result.MmrIdRatings, new());
                CreateOrAppendReplayAndReplayPlayerRatingsCsv(result.DsstatsRatingDtos,
                                                              result.ReplayRatingAppendId,
                                                              result.ReplayPlayerRatingAppendId,
                                                              RatingCalcType.Dsstats);
                await ContinueReplayRatingsFromCsv2MySql();
                await ContinueReplayPlayerRatingsFromCsv2MySql();
            }
            else
            {
                await CreatePlayerRatingCsv(result.MmrIdRatings, RatingCalcType.Dsstats);
                await PlayerRatingsFromCsv2MySql(RatingCalcType.Dsstats);

                await ReplayRatingsFromCsv2MySql(RatingCalcType.Dsstats);
                await ReplayPlayerRatingsFromCsv2MySql(RatingCalcType.Dsstats);
            }
            await SetPlayerRatingsPos(RatingCalcType.Dsstats);
            await SetRatingChange(RatingCalcType.Dsstats);
        }
    }

    private async Task UpdatePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, Dictionary<int, int> playerArcadeNoUploads)
    {
        var playerIdDic = await GetPlayerIdDic(RatingCalcType.Dsstats);

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();

            command.CommandText =
                $@"
                INSERT INTO PlayerRatings
                    ({nameof(PlayerRating.PlayerRatingId)},
                     {nameof(PlayerRating.RatingType)},
                     {nameof(PlayerRating.Rating)},
                     {nameof(PlayerRating.Pos)},
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
                VALUES ((SELECT t.{nameof(PlayerRating.PlayerRatingId)} 
                         FROM (SELECT * from PlayerRatings WHERE {nameof(PlayerRating.RatingType)} = @value1 AND {nameof(PlayerRating.PlayerId)} = @value13) as t),
                    @value1,@value2,@value3,@value4,@value5,@value6,@value7,@value8,@value9,@value10,@value11,@value12,@value13,@value14)
                ON DUPLICATE KEY UPDATE {nameof(PlayerRating.Rating)}=@value2,
                                        {nameof(PlayerRating.Games)}=@value4,
                                        {nameof(PlayerRating.Wins)}=@value5,
                                        {nameof(PlayerRating.Mvp)}=@value6,
                                        {nameof(PlayerRating.TeamGames)}=@value7,
                                        {nameof(PlayerRating.MainCount)}=@value8,
                                        {nameof(PlayerRating.Main)}=@value9,
                                        {nameof(PlayerRating.Consistency)}=@value10,
                                        {nameof(PlayerRating.Confidence)}=@value11,
                                        {nameof(PlayerRating.IsUploader)}=@value12,
                                        {nameof(PlayerRating.ArcadeDefeatsSinceLastUpload)}=@value14
            ";
            command.Transaction = transaction;

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            for (int i = 1; i <= 14; i++)
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
                    if (!playerIdDic.TryGetValue(calcEnt.PlayerId, out var playerId))
                    {
                        continue;
                    }

                    var main = calcEnt.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();

                    parameters[0].Value = ent.Key;
                    parameters[1].Value = calcEnt.Mmr;
                    parameters[2].Value = 0;
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
                    parameters[13].Value = 0;
                    command.CommandTimeout = 240;
                    await command.ExecuteNonQueryAsync();
                }
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed continue players: {error}", ex.Message);
        }
    }

    private async Task ContinueReplayRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/ReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(connectionString);
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

    private async Task ContinueReplayPlayerRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/RepPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(connectionString);
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

    public async Task DebugDeleteContinueTest()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = await context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.ReplayPlayerRatingInfo)
            .Where(x => x.ReplayRatingInfo != null)
            .OrderByDescending(o => o.GameTime)
            .Take(Random.Shared.Next(50, 99))
            .ToListAsync();

        var replayRatings = replays.Select(s => s.ReplayRatingInfo!).ToList();
        var repPlayerRating = replays.SelectMany(s => s.ReplayPlayers)
            .Where(s => s.ReplayPlayerRatingInfo != null)
            .Select(s => s.ReplayPlayerRatingInfo!).ToList();

        context.ReplayRatings.RemoveRange(replayRatings);
        context.RepPlayerRatings.RemoveRange(repPlayerRating);

        await context.SaveChangesAsync();
    }
}
