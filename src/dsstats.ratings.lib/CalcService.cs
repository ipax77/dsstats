

using pax.dsstats.shared.Calc;
using pax.dsstats.shared.Interfaces;

namespace dsstats.ratings.lib;

public partial class CalcService
{
    private readonly ICalcRepository calcRepository;

    public CalcService(ICalcRepository calcRepository)
    {
        this.calcRepository = calcRepository;
    }

    public async Task<CalcRatingResult> GenerateCombinedRatings()
    {
        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2021, 2, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 1000
        };

        CalcRatingRequest ratingRequest= new() 
        {
            MmrIdRatings = new() 
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            }
        };

        HashSet<int> processedSc2ArcadeReplayIds = new();

        var dsstatsCalcDtos = await calcRepository.GetDsstatsCalcDtos(dsstatsRequest);

        int i = 0;

        CalcRatingResult result = new();

        while (dsstatsCalcDtos.Count > 0)
        {
            i++;

            Sc2ArcadeRequest sc2ArcadeRequest = new()
            {
                FromDate = dsstatsCalcDtos.First().GameTime.AddDays(-1),
                ToDate = dsstatsCalcDtos.Last().GameTime.AddDays(1),
                GameModes = dsstatsRequest.GameModes
            };

            var sc2ArcadeCalcDtos = await calcRepository.GetSc2ArcadeCalcDtos(sc2ArcadeRequest);

            var combinedCalcDtos = CombineCalcDtos(dsstatsCalcDtos, sc2ArcadeCalcDtos, processedSc2ArcadeReplayIds);
            processedSc2ArcadeReplayIds.UnionWith(combinedCalcDtos.Select(s => s.Sc2ArcadeReplayId));

            ratingRequest.CalcDtos = combinedCalcDtos;
            
            var stepResult = GeneratePlayerRatings(ratingRequest);
            
            // result.DsstatsRatingDtos.AddRange(stepResult.DsstatsRatingDtos);
            // result.Sc2ArcadeRatingDtos.AddRange(stepResult.Sc2ArcadeRatingDtos);

            (result.ReplayRatingAppendId, result.ReplayPlayerRatingAppendId) =
                calcRepository.DsstatsCreateOrAppendReplayAndReplayPlayerRatingsCsv(stepResult.DsstatsRatingDtos,
                                                                                    result.ReplayRatingAppendId,
                                                                                    result.ReplayPlayerRatingAppendId);

            // DEBUG
            // if (i > 10)
            // {
            //     break;
            // }

            dsstatsRequest.Skip += dsstatsRequest.Take;
            dsstatsCalcDtos = await calcRepository.GetDsstatsCalcDtos(dsstatsRequest);
        }

        result.MmrIdRatings = ratingRequest.MmrIdRatings;

        await calcRepository.CreateDsstatsPlayerRatingCsv(result.MmrIdRatings);
        await calcRepository.DsstatsPlayerRatingsFromCsv2MySql();

        await calcRepository.DsstatsReplayRatingsFromCsv2MySql();
        await calcRepository.DsstatsReplayPlayerRatingsFromCsv2MySql();

        // await calcRepository.SetPlayerRatingsPos();
        // await calcRepository.SetRatingChange();

        return result;
    }
}