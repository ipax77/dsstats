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
    public async Task<(int, int)> SaveArcadeStepResult(List<shared.Calc.ReplayRatingDto> ratings,
                                                       int replayRatingId = 0,
                                                       int replayPlayerRatingId = 0)
    {
        bool append = replayRatingId > 0;

        List<ArcadeReplayRatingCsv> replayRatings = new();
        List<ArcadeReplayDsPlayerRatingCsv> replayPlayerRatings = new();
        for (int i = 0; i < ratings.Count; i++)
        {
            var rating = ratings[i];
            replayRatingId++;
            replayRatings.Add(new()
            {
                ArcadeReplayRatingId = replayRatingId,
                RatingType = (int)rating.RatingType,
                LeaverType = (int)rating.LeaverType,
                ExpectationToWin = MathF.Round(rating.ExpectationToWin, 2),
                ArcadeReplayId = rating.ReplayId,
                AvgRating = Convert.ToInt32(rating.RepPlayerRatings.Average(a => a.Rating))
            });
            foreach (var rp in rating.RepPlayerRatings)
            {
                replayPlayerRatingId++;

                replayPlayerRatings.Add(new()
                {
                    ArcadeReplayDsPlayerRatingId = replayPlayerRatingId,
                    GamePos = rp.GamePos,
                    Rating = MathF.Round(rp.Rating, 2),
                    RatingChange = MathF.Round(rp.RatingChange, 2),
                    Games = rp.Games,
                    Consistency = MathF.Round(rp.Consistency, 2),
                    Confidence = MathF.Round(rp.Confidence, 2),
                    ArcadeReplayDsPlayerId = rp.ReplayPlayerId,
                    ArcadeReplayRatingId = replayRatingId
                });
            }
        }

        var replayRatingCsv = GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayRatings));
        var replayPlayerRatingCsv = GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayDsPlayerRatings));
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


    public async Task SaveArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerIds = (await context.Players
            .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;

        await WirteArcadePlayerRatingsCsvFile(mmrIdRatings, softbans, playerIds);

        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadePlayerRatings)),
        nameof(ReplayContext.ArcadePlayerRatings), connectionString);
        await FixArcadeForeignKey(connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayRatings)),
                nameof(ReplayContext.ArcadeReplayRatings), connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayDsPlayerRatings)),
                nameof(ReplayContext.ArcadeReplayDsPlayerRatings), connectionString);

        await SetArcadePlayerRatingsPos(connectionString);
        await SetArcadeRatingChange(context, connectionString);
    }

    private async Task ClearArcadeRatingChanges(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = $"TRUNCATE TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)};";
            command.CommandTimeout = 160;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed clearing arcade rating changes: {error}", ex.Message);
        }
    }

    private async Task WirteArcadePlayerRatingsCsvFile(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                FrozenDictionary<PlayerId, bool> softBans,
                                                Dictionary<PlayerId, int> playerIdDic)
    {
        int i = 0;
        List<ArcadePlayerRatingCsv> ratings = new();
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

                var rating = GetArcadePlayerRatingCsvLine(entCalc, ent.Key, i, playerId);

                if (softBans.ContainsKey(entCalc.PlayerId))
                {
                    rating = rating with { Rating = rating.Rating - 1000.0 };
                }
                ratings.Add(rating);
            }
        }

        var playerRatingCsv = GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadePlayerRatings));

        using var stream = File.Open(playerRatingCsv, FileMode.Create);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv.WriteRecordsAsync(ratings);
    }

    private ArcadePlayerRatingCsv GetArcadePlayerRatingCsvLine(CalcRating calcRating,
                                                int ratingType,
                                                int line,
                                                int playerId)
    {
        return new()
        {
            ArcadePlayerRatingId = line,
            RatingType = ratingType,
            Rating = calcRating.Mmr,
            Games = calcRating.Games,
            Wins = calcRating.Wins,
            Mvp = calcRating.Mvps,
            Consistency = calcRating.Consistency,
            Confidence = calcRating.Confidence,
            PlayerId = playerId,
        };
    }

    public async Task SaveContinueArcadeRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                            List<shared.Calc.ReplayRatingDto> replayRatings)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;

        var playerIds = (await context.Players
            .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        await ContinueArcadePlayerRatings(mmrIdRatings, playerIds);
        await ContinueCsv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayRatings)),
                                nameof(ReplayContext.ComboReplayRatings),
                                connectionString);
        await ContinueCsv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayPlayerRatings)),
                                            nameof(ReplayContext.ComboReplayPlayerRatings),
                                            connectionString);
        await SetArcadePlayerRatingsPos(connectionString);
    }

    private async Task ContinueArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
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
$@"INSERT INTO {nameof(ReplayContext.ArcadePlayerRatings)}
    ({nameof(ArcadePlayerRating.ArcadePlayerRatingId)},
    {nameof(ArcadePlayerRating.RatingType)},
    {nameof(ArcadePlayerRating.Rating)},
    {nameof(ArcadePlayerRating.Pos)},
    {nameof(ArcadePlayerRating.Games)},
    {nameof(ArcadePlayerRating.Wins)},
    {nameof(ArcadePlayerRating.Consistency)},
    {nameof(ArcadePlayerRating.Confidence)},
    ArcadePlayerId,
    {nameof(ArcadePlayerRating.PlayerId)})
VALUES ((SELECT t.{nameof(ArcadePlayerRating.ArcadePlayerRatingId)} 
    FROM (SELECT * from {nameof(ReplayContext.ArcadePlayerRatings)} WHERE {nameof(ArcadePlayerRating.RatingType)} = @value1 
        AND {nameof(ArcadePlayerRating.PlayerId)} = @value8) as t),
    @value1,@value2,@value3,@value4,@value5,@value6,@value7,1,@value8)
ON DUPLICATE KEY UPDATE {nameof(ArcadePlayerRating.Rating)}=@value2,
                        {nameof(ArcadePlayerRating.Games)}=@value4,
                        {nameof(ArcadePlayerRating.Wins)}=@value5,
                        {nameof(ArcadePlayerRating.Consistency)}=@value6,
                        {nameof(ArcadePlayerRating.Confidence)}=@value7,
                        ArcadePlayerId = 1
            ";
            command.Transaction = transaction;

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            for (int i = 1; i <= 8; i++)
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

                    parameters[0].Value = ent.Key;
                    parameters[1].Value = calcEnt.Mmr;
                    parameters[2].Value = 0;
                    parameters[3].Value = calcEnt.Games;
                    parameters[4].Value = calcEnt.Wins;
                    parameters[5].Value = calcEnt.Consistency;
                    parameters[6].Value = calcEnt.Confidence;
                    parameters[7].Value = playerId;
                    command.CommandTimeout = 240;
                    await command.ExecuteNonQueryAsync();
                }
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed continue arcadeplayers: {error}", ex.Message);
        }
    }
}

internal record ArcadePlayerRatingCsv
{
    public int ArcadePlayerRatingId { get; set; }
    public int RatingType { get; set; }
    public double Rating { get; set; }
    public int Pos { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public int MainCount { get; set; }
    public int Main { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int IsUploader { get; set; }
    public int ArcadePlayerId { get; set; } = 1;
    public int PlayerId { get; set; }
}

internal record ArcadeReplayRatingCsv
{
    public int ArcadeReplayRatingId { get; set; }
    public int RatingType { get; set; }
    public int LeaverType { get; set; }
    public float ExpectationToWin { get; set; } // WinnerTeam
    public int ArcadeReplayId { get; set; }
    public int AvgRating { get; set; }
}

internal record ArcadeReplayDsPlayerRatingCsv
{
    public int ArcadeReplayDsPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ArcadeReplayDsPlayerId { get; set; }
    public int ArcadeReplayRatingId { get; set; }
}