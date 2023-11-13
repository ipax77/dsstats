using CsvHelper.Configuration;
using CsvHelper;
using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace dsstats.ratings;

public partial class RatingsSaveService
{
    public async Task<(int, int)> SaveArcadeStepResult(List<shared.Calc.ReplayRatingDto> ratings,
                                                       int replayRatingId = 0,
                                                       int replayPlayerRatingId = 0)
    {
        bool append = replayRatingId > 0;

        List<ArcadeReplayRatingCsv> replayRatings = new();
        List<ArcadeReplayPlayerRatingCsv> replayPlayerRatings = new();
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
                    ArcadeReplayPlayerRatingId = replayPlayerRatingId,
                    Rating = MathF.Round(rp.Rating, 2),
                    RatingChange = MathF.Round(rp.RatingChange, 2),
                    Games = rp.Games,
                    Consistency = MathF.Round(rp.Consistency, 2),
                    Confidence = MathF.Round(rp.Confidence, 2),
                    ArcadeReplayPlayerId = rp.ReplayPlayerId
                });
            }
        }

        var replayRatingCsv = GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayRatings));
        var replayPlayerRatingCsv = GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayPlayerRatings));
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


    public async Task SaveArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playerIds = (await context.ArcadePlayers
            .Select(s => new { s.ProfileId, s.RealmId, s.RegionId, s.ArcadePlayerId }).ToListAsync())
            .ToDictionary(k => new PlayerId(k.ProfileId, k.RealmId, k.RegionId), v => v.ArcadePlayerId);

        var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var connectionString = options.Value.ImportConnectionString;

        await WirteArcadePlayerRatingsCsvFile(mmrIdRatings, playerIds);

        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadePlayerRatings)),
        nameof(ReplayContext.ArcadePlayerRatings), connectionString);
        await FixArcadeForeignKey(connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayRatings)),
                nameof(ReplayContext.ArcadeReplayRatings), connectionString);
        await Csv2Mysql(GetFileName(RatingCalcType.Arcade, nameof(ReplayContext.ArcadeReplayPlayerRatings)),
                nameof(ReplayContext.ArcadeReplayPlayerRatings), connectionString);

        await SetArcadePlayerRatingsPos(connectionString);
        await SetArcadeRatingChange(connectionString);
    }

    private async Task WirteArcadePlayerRatingsCsvFile(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
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

                ratings.Add(GetArcadePlayerRatingCsvLine(entCalc, ent.Key, i, playerId));

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
            ArcadePlayerId = playerId,
        };
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
    public int ArcadePlayerId { get; set; }
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

internal record ArcadeReplayPlayerRatingCsv
{
    public int ArcadeReplayPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ArcadeReplayPlayerId { get; set; }
    public int ArcadeReplayRatingId { get; set; }
}