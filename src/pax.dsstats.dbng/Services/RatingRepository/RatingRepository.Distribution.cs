using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;


namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();

        using var scope = scopeFactory.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        if (!memoryCache.TryGetValue(memKey, out DistributionResponse response))
        {
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            //var mmrDevs = await context.PlayerRatings
            //    .Where(x => x.RatingType == request.RatingType)
            //    .GroupBy(g => Math.Round(g.Rating, 0))
            //    .Select(s => new MmrDevDto
            //    {
            //        Count = s.Count(),
            //        //Mmr = s.Average(a => Math.Round(a.Rating, 0))
            //        Mmr = s.Key
            //    })
            //    .OrderBy(o => o.Mmr)
            //    .ToListAsync();

            var mmrDevs = await context.PlayerRatings
                .Where(x => x.RatingType == request.RatingType)
                .GroupBy(g => Math.Floor((g.Rating / 10) + 1) * 10)
                .Select(s => new MmrDevDto
                {
                    Count = s.Count(),
                    //Mmr = s.Average(a => Math.Round(a.Rating, 0))
                    Mmr = s.Key
                })
                .OrderBy(o => o.Mmr)
                .ToListAsync();

            response = new()
            {
                MmrDevs = mmrDevs
                //MmrDevs = GetStepedMmrDevs(mmrDevs)
            };

            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private List<MmrDevDto> GetStepedMmrDevs(List<MmrDevDto> mmrDevs, int stepSize = 10)
    {
        if (!mmrDevs.Any())
        {
            return mmrDevs;
        }

        int step = stepSize;
        int max = Convert.ToInt32(mmrDevs.Last().Mmr) + stepSize;

        List<MmrDevDto> result = new();

        MmrDevDto stepDev = new() { Mmr = step };
        for (int i = 0; i < mmrDevs.Count; i++)
        {
            var mmrDev = mmrDevs[i];

            if (mmrDev.Mmr <= step)
            {
                stepDev.Count += mmrDev.Count;
            }
            else
            {
                result.Add(stepDev);
                step += stepSize;
                stepDev = new() { Mmr = step };
            }
        }
        return result;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation123()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == RatingType.Cmdr)
            .GroupBy(g => Math.Round(g.Rating, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Rating, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToListAsync();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd123()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == RatingType.Std)
            .GroupBy(g => Math.Round(g.Rating, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Rating, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToListAsync();
    }
}
