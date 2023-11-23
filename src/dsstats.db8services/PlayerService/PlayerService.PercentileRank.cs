
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public partial class PlayerService
{
    private async Task<(double cmdrRank, double stdRank)> GetPercentileRank(int cmdrPos, int stdPos, RatingCalcType ratingCalcType)
    {
        double cmdrRank = 0;
        double stdRank = 0;

        if (cmdrPos != 0)
        {
            var maxPos = await GetMaxPos(RatingType.Cmdr, ratingCalcType);
            cmdrRank = CalculatePercentileRank(maxPos, cmdrPos);
        }

        if (stdPos != 0)
        {
            var maxPos = await GetMaxPos(RatingType.Std, ratingCalcType);
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

    private async Task<int> GetMaxPos(RatingType ratingType, RatingCalcType ratingCalcType)
    {
        var memKey = $"MaxPos{ratingCalcType}{ratingType}";

        if (!memoryCache.TryGetValue(memKey, out int maxPos))
        {
            maxPos = ratingCalcType switch
            {
                RatingCalcType.Dsstats =>  await context.PlayerRatings
                .Where(x => x.RatingType == ratingType)
                .OrderByDescending(o => o.Pos)
                .Select(s => s.Pos)
                .FirstOrDefaultAsync(),
                RatingCalcType.Arcade => await context.ArcadePlayerRatings
                .Where(x => x.RatingType == ratingType)
                .OrderByDescending(o => o.Pos)
                .Select(s => s.Pos)
                .FirstOrDefaultAsync(),
                RatingCalcType.Combo => await context.ComboPlayerRatings
                .Where(x => x.RatingType == ratingType)
                .OrderByDescending(o => o.Pos)
                .Select(s => s.Pos)
                .FirstOrDefaultAsync(),
                _ => 0
            };

            if (maxPos > 0)
            {
                memoryCache.Set(memKey, maxPos, TimeSpan.FromHours(24));
            }
        }
        return maxPos;
    }
}