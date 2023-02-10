using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;


namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
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
