

using dsstats.shared;
using dsstats.shared.Calc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace dsstats.ratings.lib;

public partial class CalcService
{
    private readonly bool IsSqlite;
    private readonly SemaphoreSlim ss = new(1, 1);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<CalcService> logger;

    public CalcService(IServiceScopeFactory scopeFactory, ILogger<CalcService> logger)
    {
        using var scope = scopeFactory.CreateScope();
        var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        IsSqlite = dbOptions.Value.IsSqlite;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task<CalcRatingResult> GenerateRatings(RatingCalcType ratingCalcType, bool recalc = false)
    {
        logger.LogInformation("starting rating calculation: {rating}", ratingCalcType.ToString());
        await ss.WaitAsync();
        CalcRatingResult result = new();
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            result = ratingCalcType switch
            {
                RatingCalcType.Dsstats => await GenerateDsstatsRatings(recalc),
                RatingCalcType.Arcade => await GenerateArcadeRatings(),
                RatingCalcType.Combo => await GenerateCombinedRatings(),
                _ => throw new NotImplementedException()
            };
        }
        catch (Exception ex)
        {
            logger.LogError("failed generating {ratingCalcType} ratings: {error}", ratingCalcType.ToString(), ex.Message);
        }
        finally
        {
            ss.Release();
        }
        sw.Stop();
        logger.LogWarning("{ratingCalcType} ratings produced in {time}", ratingCalcType.ToString(), sw.Elapsed.ToString(@"hh\:mm\:ss"));
        return result;
    }

    private static void SetCmdr(CalcRating calcRating, Commander cmdr)
    {
        if (calcRating.CmdrCounts.TryGetValue(cmdr, out int count))
        {
            calcRating.CmdrCounts[cmdr] = count + 1;
        }
        else
        {
            calcRating.CmdrCounts[cmdr] = 1;
        }
    }

    private void Cleanup(CalcRatingResult result, CalcRatingRequest request)
    {
        if (IsSqlite)
        {
            return;
        }

        foreach (var ratingType in result.MmrIdRatings.Keys)
        {
            foreach (var player in request.BannedPlayers.Keys)
            {
                if (result.MmrIdRatings[ratingType].TryGetValue(player, out var rating))
                {
                    rating.Mvps = 0;
                    rating.Wins = 0;
                    rating.Mmr = 0;
                    rating.Confidence = 0;
                    rating.Consistency = 0;
                    rating.IsUploader = false;
                }
            }
        }
    }
}