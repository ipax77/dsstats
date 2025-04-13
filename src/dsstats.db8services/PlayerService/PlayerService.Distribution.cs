using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public partial class PlayerService
{
    public async Task<DistributionResponse> GetDistribution(DistributionRequest request)
    {
        var mamKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(mamKey, out DistributionResponse? response)
            || response is null)
        {
            response = await ProduceDistribution(request);
            memoryCache.Set(mamKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<DistributionResponse> ProduceDistribution(DistributionRequest request)
    {
        var group = from r in context.PlayerRatings
                    where r.RatingType == request.RatingType
                    group r by new { Mmr = Math.Floor((r.Rating / 10) + 1) * 10 } into g
                    orderby g.Key.Mmr
                    select new MmrDevDto()
                    {
                        Mmr = g.Key.Mmr,
                        Count = g.Count()
                    };

        return new()
        {
            MmrDevs = await group.ToListAsync()
        };
    }


}
