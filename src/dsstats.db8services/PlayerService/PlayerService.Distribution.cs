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
            response  = await ProduceDistribution(request);
            memoryCache.Set(mamKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<DistributionResponse> ProduceDistribution(DistributionRequest request)
    {
        return request.RatingCalcType switch
        {
            RatingCalcType.Combo => await ProduceComboDistribution(request),
            RatingCalcType.Dsstats => await ProduceDsstatsDistribution(request),
            RatingCalcType.Arcade => await ProduceArcadeDistribution(request),
            _ => new()
        };
    }

    private async Task<DistributionResponse> ProduceComboDistribution(DistributionRequest request)
    {
        var group = from r in context.ComboPlayerRatings
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

    private async Task<DistributionResponse> ProduceDsstatsDistribution(DistributionRequest request)
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

    private async Task<DistributionResponse> ProduceArcadeDistribution(DistributionRequest request)
    {
        var group = from r in context.ArcadePlayerRatings
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
