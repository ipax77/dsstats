
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings.lib;

public partial class CalcService
{
    private async Task<CalcRatingResult> GenerateDsstatsRatings(bool recalc)
    {
        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2018, 1, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 10000
        };

        HashSet<int> processedSc2ArcadeReplayIds = new();

        using var scope = scopeFactory.CreateScope();
        var calcRepository = scope.ServiceProvider.GetRequiredService<ICalcRepository>();

        var ratingRequest = recalc ? new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            MmrIdRatings = new()
                {
                    { 1, new() },
                    { 2, new() },
                    { 3, new() },
                    { 4, new() }
                },
        }
           : await calcRepository.GetDsstatsCalcRatingRequest();

        if (ratingRequest == null)
        {
            logger.LogWarning("Nothing to do.");
            return new() { NothingToDo = true };
        }

        dsstatsRequest.FromDate = ratingRequest.StarTime;
        dsstatsRequest.Continue = ratingRequest.Continue;

        var dsstatsCalcDtos = await calcRepository.GetDsstatsCalcDtos(dsstatsRequest);


        int i = 0;

        CalcRatingResult result = new()
        {
            ReplayPlayerRatingAppendId = ratingRequest.ReplayPlayerRatingAppendId,
            ReplayRatingAppendId = ratingRequest.ReplayRatingAppendId,
            Continue = ratingRequest.Continue,
        };

        while (dsstatsCalcDtos.Count > 0)
        {
            i++;

            ratingRequest.CalcDtos = dsstatsCalcDtos;

            var stepResult = GeneratePlayerRatings(ratingRequest);

            if (IsSqlite)
            {
                result.DsstatsRatingDtos.AddRange(stepResult.DsstatsRatingDtos);
            }
            else
            {
                (result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId) =
                    calcRepository.CreateOrAppendReplayAndReplayPlayerRatingsCsv(stepResult.DsstatsRatingDtos,
                                                                                        result.ReplayRatingAppendId,
                                                                                        result.ReplayPlayerRatingAppendId,
                                                                                        RatingCalcType.Dsstats);
            }

            dsstatsRequest.Skip += dsstatsRequest.Take;
            dsstatsCalcDtos = await calcRepository.GetDsstatsCalcDtos(dsstatsRequest);
        }

        result.MmrIdRatings = ratingRequest.MmrIdRatings;
        Cleanup(result, ratingRequest);

        await calcRepository.StoreDsstatsResult(result, ratingRequest.Continue);

        return result;
    }
}