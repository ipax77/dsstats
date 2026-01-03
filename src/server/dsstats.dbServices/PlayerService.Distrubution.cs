using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<DistributionResponse> GetRatingDistribution(DistributionRequest request)
    {
        var memKey = request.GetMemKey();
        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return await CreateDistributionResponse(request);
        }) ?? new();
    }

    private async Task<DistributionResponse> CreateDistributionResponse(DistributionRequest request)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var group = from r in context.PlayerRatings
                    where r.RatingType == request.RatingType
                        && r.Rating > 0 && r.Rating < 3000
                    group r by new { Rating = Math.Floor(r.Rating / 10.0) * 10 } into g
                    orderby g.Key.Rating
                    select new DistributionItem
                    {
                        Rating = (int)g.Key.Rating + 5,
                        Count = g.Count()
                    };

        var items = await group.ToListAsync();

        // remove outliers at the start and end - 'normalize' 1k default raing here
        items = items
                .SkipWhile(x => x.Count <= 2)
                .ToList();

        items.Reverse();
        items = items
            .SkipWhile(x => x.Count <= 2)
            .ToList();
        items.Reverse();

        var defaultBucket = items.FirstOrDefault(x => x.Rating == 1000);
        if (defaultBucket != null)
        {
            items.Remove(defaultBucket);
        }

        int mergeFrom = 2200;
        int mergedCount = items.Where(x => x.Rating >= mergeFrom).Sum(x => x.Count);

        items = items
            .Where(x => x.Rating < mergeFrom)
            .ToList();

        items.Add(new DistributionItem { Rating = mergeFrom, Count = mergedCount });

        return new()
        {
            Items = items,
        };
    }
}
