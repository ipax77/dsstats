
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class PlayerService
{
    private async Task<(double cmdrRank, double stdRank)> GetPercentileRank(int cmdrPos, int stdPos)
    {
        using var scope = scopeFactory.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        double cmdrRank = 0;
        double stdRank = 0;

        if (cmdrPos != 0)
        {
            var maxPos = await GetMaxPos(RatingType.Cmdr);
            cmdrRank = CalculatePercentileRank(maxPos, cmdrPos);
        }

        if (stdPos != 0)
        {
            var maxPos = await GetMaxPos(RatingType.Std);
            stdRank = CalculatePercentileRank(maxPos, stdPos);
        }

        return (Math.Round(cmdrRank, 2), Math.Round(stdRank, 2));
    }

    private static double CalculatePercentileRank(int maxPos, int playerPos)
    {
        if (maxPos == 0)
        {
            return 0;
        }

        int totalPlayers = maxPos;
        int lowerPlayers = maxPos - playerPos;

        return (double)lowerPlayers / totalPlayers * 100;
    }

    private async Task<int> GetMaxPos(RatingType ratingType)
    {
        using var scope = scopeFactory.CreateScope();
        var memeoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var memKey = $"MaxPos{ratingType}";

        if (!memeoryCache.TryGetValue(memKey, out int maxPos))
        {
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            maxPos = await context.PlayerRatings
                .Where(x => x.RatingType == ratingType)
                .OrderByDescending(o => o.Pos)
                .Select(s => s.Pos)
                .FirstOrDefaultAsync();

            if (maxPos > 0)
            {
                memeoryCache.Set(memKey, maxPos, TimeSpan.FromHours(24));
            }
        }
        return maxPos;
    }
}