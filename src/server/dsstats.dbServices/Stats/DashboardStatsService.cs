using dsstats.db;
using dsstats.shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using dsstats.shared.Interfaces;

namespace dsstats.dbServices.Stats;

public partial class DashboardStatsService(DsstatsContext context, IMemoryCache memoryCache) : IDashboardStatsService
{
    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(CancellationToken token = default)
    {
        var memKey = "dashboardStats";
        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await GetDashboardStatsInternalAsync(token);
        }) ?? new();
    }

    private async Task<DashboardStatsResponse> GetDashboardStatsInternalAsync(CancellationToken token)
    {
        var timeInfo = Data.GetTimePeriodInfo(TimePeriod.Last90Days);

        var arcadeCount = await context.ArcadeReplays
            .Where(x => x.CreatedAt >= timeInfo.Start)
            .CountAsync();

        var dsstatsGroup = await context.Replays
            .Where(x => x.Gametime >= timeInfo.Start)
            .GroupBy(x => x.GameMode)
            .Select(g => new DashboardGameModeStats
            {
                GameMode = g.Key,
                Count = g.Count()
            })
            .ToListAsync(token);

        var uploads = await context.UploadJobs
            .Where(x => x.CreatedAt >= timeInfo.Start)
            .CountAsync(token);

        var totalCount = dsstatsGroup.Sum(x => x.Count) + arcadeCount;
        return new DashboardStatsResponse
        {
            Total = totalCount,
            SC2Arcade = arcadeCount,
            Dsstats = totalCount - arcadeCount,
            GameModes = dsstatsGroup,
            Uploads = uploads
        };
    }
}

