using CsvHelper.Configuration;
using CsvHelper;
using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Frozen;

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