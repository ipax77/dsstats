using CsvHelper;
using CsvHelper.Configuration;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Calc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Collections.Frozen;
using System.Globalization;

namespace dsstats.ratings;

public partial class RatingsSaveService
{
    public async Task<(int, int)> SaveDsstatsStepResult(List<shared.Calc.ReplayRatingDto> ratings,
    int replayRatingId = 0, int replayPlayerRatingId = 0)
    {
        bool append = replayRatingId > 0;

        List<ReplayRatingCsv> replayRatings = new();
        List<RepPlayerRatingCsv> replayPlayerRatings = new();
        for (int i = 0; i < ratings.Count; i++)
        {
            var rating = ratings[i];
            replayRatingId++;
            replayRatings.Add(new()
            {
                ReplayRatingId = replayRatingId,
                RatingType = (int)rating.RatingType,
                LeaverType = (int)rating.LeaverType,
                ExpectationToWin = MathF.Round(rating.ExpectationToWin, 2),
                ReplayId = rating.ReplayId,
                AvgRating = Convert.ToInt32(rating.RepPlayerRatings.Average(a => a.Rating))
            });
            foreach (var rp in rating.RepPlayerRatings)
            {
                replayPlayerRatingId++;

                replayPlayerRatings.Add(new()
                {
                    RepPlayerRatingId = replayPlayerRatingId,
                    GamePos = rp.GamePos,
                    Rating = MathF.Round(rp.Rating, 2),
                    RatingChange = MathF.Round(rp.RatingChange, 2),
                    Games = rp.Games,
                    Consistency = MathF.Round(rp.Consistency, 2),
                    Confidence = MathF.Round(rp.Confidence, 2),
                    ReplayPlayerId = rp.ReplayPlayerId,
                    ReplayRatingInfoId = replayRatingId
                });
            }
        }

        var replayRatingCsv = GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.ReplayRatings));
        var replayPlayerRatingCsv = GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.RepPlayerRatings));
        FileMode fileMode = append ? FileMode.Append : FileMode.Create;

        using var stream1 = File.Open(replayRatingCsv, fileMode);
        using var writer1 = new StreamWriter(stream1);
        using var csv1 = new CsvWriter(writer1, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv1.WriteRecordsAsync(replayRatings);

        using var stream2 = File.Open(replayPlayerRatingCsv, fileMode);
        using var writer2 = new StreamWriter(stream2);
        using var csv2 = new CsvWriter(writer2, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv2.WriteRecordsAsync(replayPlayerRatings);

        return (replayRatingId, replayPlayerRatingId);
    }


    public async Task SaveDsstatsPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans, bool isContinue)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerIds = (await context.Players
            .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;


        if (isContinue)
        {
            await ContinuePlayerRatings(mmrIdRatings, playerIds);
            await ContinueCsv2Mysql(GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.ReplayRatings)),
                    nameof(ReplayContext.ReplayRatings), connectionString);
            await ContinueCsv2Mysql(GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.RepPlayerRatings)),
                    nameof(ReplayContext.RepPlayerRatings), connectionString);
        }
        else
        {
            await WirteDsstatsPlayerRatingsCsvFile(mmrIdRatings, softbans, playerIds);
            await Csv2Mysql(GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.PlayerRatings)),
                nameof(ReplayContext.PlayerRatings), connectionString);
            await FixDsstatsForeignKey(connectionString);

            await Csv2Mysql(GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.ReplayRatings)),
                    nameof(ReplayContext.ReplayRatings), connectionString);
            await Csv2Mysql(GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.RepPlayerRatings)),
                    nameof(ReplayContext.RepPlayerRatings), connectionString);
        }

        await SetPlayerRatingsPos(connectionString);
        await SetDsstatsRatingChange(context, connectionString);
    }

    private async Task ClearDsstatsRatingChanges(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = $"TRUNCATE TABLE {nameof(ReplayContext.PlayerRatingChanges)};";
            command.CommandTimeout = 160;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed clearing dsstats rating changes: {error}", ex.Message);
        }
    }

    private async Task ContinuePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                             Dictionary<PlayerId, int> playerIdDic)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
               $@"INSERT INTO PlayerRatings
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
    FROM (SELECT * from PlayerRatings WHERE {nameof(PlayerRating.RatingType)} = @value1 
        AND {nameof(PlayerRating.PlayerId)} = @value13) as t),
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

    private async Task WirteDsstatsPlayerRatingsCsvFile(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                        FrozenDictionary<PlayerId, bool> softBans,
                                                        Dictionary<PlayerId, int> playerIdDic)
    {
        int i = 0;
        List<PlayerRatingCsv> ratings = new();
        foreach (var ent in mmrIdRatings)
        {
            foreach (var entCalc in ent.Value.Values)
            {
                if (entCalc is null)
                {
                    continue;
                }

                if (!playerIdDic.TryGetValue(entCalc.PlayerId, out var playerId))
                {
                    continue;
                }

                i++;

                var rating = GetDsstatsPlayerRatingCsvLine(entCalc, ent.Key, i, playerId);

                if (softBans.ContainsKey(entCalc.PlayerId))
                {
                    rating = rating with { Rating = rating.Rating - 1000.0 };
                }
                ratings.Add(rating);

            }
        }

        var playerRatingCsv = GetFileName(RatingCalcType.Dsstats, nameof(ReplayContext.PlayerRatings));

        using var stream = File.Open(playerRatingCsv, FileMode.Create);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv.WriteRecordsAsync(ratings);
    }

    private PlayerRatingCsv GetDsstatsPlayerRatingCsvLine(CalcRating calcRating,
                                                int ratingType,
                                                int line,
                                                int playerId)
    {
        var main = calcRating.CmdrCounts
            .OrderByDescending(o => o.Value)
            .FirstOrDefault();
        var maincount = main.Key == Commander.None ? 0 : main.Value;

        return new()
        {
            PlayerRatingId = line,
            RatingType = ratingType,
            Rating = calcRating.Mmr,
            Games = calcRating.Games,
            Wins = calcRating.Wins,
            Mvp = calcRating.Mvps,
            MainCount = maincount,
            Main = (int)main.Key,
            Consistency = calcRating.Consistency,
            Confidence = calcRating.Confidence,
            PlayerId = playerId,
        };
    }
}

internal record PlayerRatingCsv
{
    public int PlayerRatingId { get; init; }
    public int RatingType { get; init; }
    public double Rating { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public int Main { get; init; }
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public int IsUploader { get; init; }
    public int PlayerId { get; init; }
    public int Pos { get; init; }
    public int ArcadeDefeatsSinceLastUpload { get; init; }
}

internal record ReplayRatingCsv
{
    public int ReplayRatingId { get; init; }
    public int RatingType { get; init; }
    public int LeaverType { get; init; }
    public float ExpectationToWin { get; init; }
    public int ReplayId { get; init; }
    public int IsPreRating { get; init; }
    public int AvgRating { get; init; }
}

internal record RepPlayerRatingCsv
{
    public int RepPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ReplayPlayerId { get; set; }
    public int ReplayRatingInfoId { get; set; }
}