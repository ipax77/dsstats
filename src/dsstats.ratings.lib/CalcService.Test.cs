namespace dsstats.ratings.lib;

public partial class CalcService
{
    public async Task Test()
    {

        // PAX: 2, 35921
        // Feralan: 174, 35712
        // WÖFFE: 11287, 35246

        var dsreps = await calcRepository.TestGetDsstatsCalcDtos(2);
        var arreps = await calcRepository.TestGetSc2ArcadeCalcDtos(35921);

        Console.WriteLine($"dsstats: {dsreps.Count}, sc2arcade: {arreps.Count}");

        var comb = CombineCalcDtos(dsreps, arreps, new());

        var dscomb = comb.Count(x => x.DsstatsReplayId > 0 && x.Sc2ArcadeReplayId == 0);
        var arcomb = comb.Count(x => x.DsstatsReplayId == 0 && x.Sc2ArcadeReplayId > 0);

        Console.WriteLine($"combined: {comb.Count} ({dscomb}|{arcomb})");
    }
}