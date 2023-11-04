
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.ratings.lib;

public partial class CalcService
{
    private async Task<CalcRatingResult> GenerateArcadeRatings()
    {
        DateTime startDate = new DateTime(2021, 2, 1);
        Sc2ArcadeRequest sc2ArcadeRequest = new()
        {
            FromDate = new DateTime(2021, 2, 1),
            ToDate = startDate.AddMonths(1),
            GameModes = new() { 3, 4, 7 }
        };

        CalcRatingRequest ratingRequest = new()
        {
            RatingCalcType = RatingCalcType.Arcade,
            MmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            }
        };

        using var scope = scopeFactory.CreateScope();
        var calcRepository = scope.ServiceProvider.GetRequiredService<ICalcRepository>();
        var sc2ArcadeCalcDtos = await calcRepository.GetSc2ArcadeCalcDtos(sc2ArcadeRequest);

        int i = 0;

        CalcRatingResult result = new();

        while (sc2ArcadeCalcDtos.Count > 0)
        {
            i++;


            ratingRequest.CalcDtos = sc2ArcadeCalcDtos;

            var stepResult = GeneratePlayerRatings(ratingRequest);

            // result.DsstatsRatingDtos.AddRange(stepResult.DsstatsRatingDtos);
            // result.Sc2ArcadeRatingDtos.AddRange(stepResult.Sc2ArcadeRatingDtos);

            (result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId) =
                calcRepository.CreateOrAppendReplayAndReplayPlayerRatingsCsv(stepResult.Sc2ArcadeRatingDtos,
                                                                                    result.ReplayRatingAppendId,
                                                                                    result.ReplayPlayerRatingAppendId,
                                                                                    RatingCalcType.Arcade);

            sc2ArcadeRequest.FromDate = sc2ArcadeRequest.ToDate;
            sc2ArcadeRequest.ToDate = sc2ArcadeRequest.ToDate.AddMonths(1);
            sc2ArcadeCalcDtos = await calcRepository.GetSc2ArcadeCalcDtos(sc2ArcadeRequest);
        }

        result.MmrIdRatings = ratingRequest.MmrIdRatings;

        Cleanup(result, ratingRequest);

        await calcRepository.CreatePlayerRatingCsv(result.MmrIdRatings, RatingCalcType.Arcade);
        await calcRepository.PlayerRatingsFromCsv2MySql(RatingCalcType.Arcade);
        await calcRepository.ReplayRatingsFromCsv2MySql(RatingCalcType.Arcade);
        await calcRepository.ReplayPlayerRatingsFromCsv2MySql(RatingCalcType.Arcade);
        await calcRepository.SetPlayerRatingsPos(RatingCalcType.Arcade);
        await calcRepository.SetRatingChange(RatingCalcType.Arcade);

        return result;
    }
}
