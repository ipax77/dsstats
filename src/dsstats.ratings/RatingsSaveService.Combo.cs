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
    public async Task<(int, int)> SaveComboStepResult(List<shared.Calc.ReplayRatingDto> ratings,
                                                   int replayRatingId = 0,
                                                   int replayPlayerRatingId = 0)
    {
        bool append = replayRatingId > 0;

        List<ComboReplayRatingCsv> replayRatings = new();
        List<ComboReplayPlayerRatingCsv> replayPlayerRatings = new();
        for (int i = 0; i < ratings.Count; i++)
        {
            var rating = ratings[i];
            replayRatingId++;
            replayRatings.Add(new()
            {
                ComboReplayRatingId = replayRatingId,
                RatingType = rating.RatingType,
                LeaverType = rating.LeaverType,
                ExpectationToWin = MathF.Round(rating.ExpectationToWin, 2),
                ReplayId = rating.ReplayId,
                AvgRating = Convert.ToInt32(rating.RepPlayerRatings.Average(a => a.Rating))
            });
            foreach (var rp in rating.RepPlayerRatings)
            {
                replayPlayerRatingId++;

                replayPlayerRatings.Add(new()
                {
                    ComboReplayPlayerRatingId = replayPlayerRatingId,
                    GamePos = rp.GamePos,
                    Rating = Convert.ToInt32(rp.Rating),
                    Change = MathF.Round(rp.RatingChange, 2),
                    Games = rp.Games,
                    Consistency = MathF.Round(rp.Consistency, 2),
                    Confidence = MathF.Round(rp.Confidence, 2),
                    ReplayPlayerId = rp.ReplayPlayerId
                });
            }
        }

        var replayRatingCsv = GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayRatings));
        var replayPlayerRatingCsv = GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayPlayerRatings));
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


    public async Task SaveComboPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerIds = (await context.Players
            .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();

        await WirteComboPlayerRatingsCsvFile(mmrIdRatings, softbans, playerIds, options.Value.ImportConnectionString);
    }

    private async Task WirteComboPlayerRatingsCsvFile(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                                FrozenDictionary<PlayerId, bool> softBans,
                                                Dictionary<PlayerId, int> playerIdDic,
                                                string connectionString)
    {
        int i = 0;
        List<ComboPlayerRatingCsv> ratings = new();
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

                var rating = GetComboPlayerRatingCsvLine(entCalc, ent.Key, i, playerId);

                if (softBans.ContainsKey(entCalc.PlayerId))
                {
                    rating = rating with { Rating = rating.Rating - 1000.0 };
                }
                ratings.Add(rating);

            }
        }

        var playerRatingCsv = GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboPlayerRatings));

        using (var stream = File.Open(playerRatingCsv, FileMode.Create))
        {
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            });
            await csv.WriteRecordsAsync(ratings);
        }

        await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboPlayerRatings)),
                nameof(ReplayContext.ComboPlayerRatings), connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayRatings)),
                nameof(ReplayContext.ComboReplayRatings), connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayPlayerRatings)),
                nameof(ReplayContext.ComboReplayPlayerRatings), connectionString);

        await SetComboPlayerRatingsPos(connectionString);
    }

    private ComboPlayerRatingCsv GetComboPlayerRatingCsvLine(CalcRating calcRating,
                                                int ratingType,
                                                int line,
                                                int playerId)
    {
        return new()
        {
            ComboPlayerRatingId = line,
            RatingType = ratingType,
            Rating = calcRating.Mmr,
            Games = calcRating.Games,
            Wins = calcRating.Wins,
            Consistency = calcRating.Consistency,
            Confidence = calcRating.Confidence,
            PlayerId = playerId,
        };
    }

    public async Task SaveContinueComboRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                               List<shared.Calc.ReplayRatingDto> replayRatings)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;

        var playerIds = (await context.Players
            .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);

        await ContinueComboPlayerRatings(mmrIdRatings, playerIds);
        await ContinueCsv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayRatings)),
                                nameof(ReplayContext.ComboReplayRatings),
                                connectionString);
        await ContinueCsv2Mysql(GetFileName(RatingCalcType.Combo, nameof(ReplayContext.ComboReplayPlayerRatings)),
                                            nameof(ReplayContext.ComboReplayPlayerRatings),
                                            connectionString);
        await SetComboPlayerRatingsPos(connectionString);
    }

    private async Task ContinueComboPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
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
$@"INSERT INTO {nameof(ReplayContext.ComboPlayerRatings)}
    ({nameof(ComboPlayerRating.ComboPlayerRatingId)},
    {nameof(ComboPlayerRating.RatingType)},
    {nameof(ComboPlayerRating.Rating)},
    {nameof(ComboPlayerRating.Pos)},
    {nameof(ComboPlayerRating.Games)},
    {nameof(ComboPlayerRating.Wins)},
    {nameof(ComboPlayerRating.Consistency)},
    {nameof(ComboPlayerRating.Confidence)},
    {nameof(ComboPlayerRating.PlayerId)})
VALUES ((SELECT t.{nameof(ComboPlayerRating.ComboPlayerRatingId)} 
    FROM (SELECT * from {nameof(ReplayContext.ComboPlayerRatings)} WHERE {nameof(ComboPlayerRating.RatingType)} = @value1 
        AND {nameof(ComboPlayerRating.PlayerId)} = @value8) as t),
    @value1,@value2,@value3,@value4,@value5,@value6,@value7,@value8)
ON DUPLICATE KEY UPDATE {nameof(ComboPlayerRating.Rating)}=@value2,
                        {nameof(ComboPlayerRating.Games)}=@value4,
                        {nameof(ComboPlayerRating.Wins)}=@value5,
                        {nameof(ComboPlayerRating.Consistency)}=@value6,
                        {nameof(ComboPlayerRating.Confidence)}=@value7
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
            logger.LogError("failed continue comboplayers: {error}", ex.Message);
        }
    }
}

internal record ComboPlayerRatingCsv
{
    public int ComboPlayerRatingId { get; init; }
    public int RatingType { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public double Rating { get; init; }
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public int Pos { get; init; }
    public int PlayerId { get; init; }
}

internal record ComboReplayRatingCsv
{
    public int ComboReplayRatingId { get; init; }
    public int RatingType { get; init; }
    public int LeaverType { get; init; }
    public double ExpectationToWin { get; init; }
    public int ReplayId { get; init; }
    public int IsPreRating { get; init; }
    public int AvgRating { get; init; }
}

internal record ComboReplayPlayerRatingCsv
{
    public int ComboReplayPlayerRatingId { get; init; }
    public int GamePos { get; init; }
    public int Rating { get; init; }
    public double Change { get; init; }
    public int Games { get; init; }
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public int ReplayPlayerId { get; init; }
}