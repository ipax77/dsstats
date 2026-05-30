using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public partial class DashboardStatsService(IDbContextFactory<DsstatsContext> contextFactory, IMemoryCache memoryCache) : IDashboardStatsService
{
    private const string MauiUploadSource = "maui";
    private const string MyDsstatsUploadSource = "mydsstats";
    private const string ServiceUploadSource = "service";
    private const string UnknownVersion = "unknown";
    private static readonly string[] UploadSources = [MauiUploadSource, MyDsstatsUploadSource, ServiceUploadSource];

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
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var timeInfo = Data.GetTimePeriodInfo(TimePeriod.Last90Days);

        var arcadeCount = await context.ArcadeReplays
            .Where(x => x.CreatedAt >= timeInfo.Start)
            .CountAsync(token);

        var dsstatsGroup = await context.Replays
            .Where(x => x.Gametime >= timeInfo.Start)
            .GroupBy(x => x.GameMode)
            .Select(g => new DashboardGameModeStats
            {
                GameMode = g.Key,
                Count = g.Count()
            })
            .ToListAsync(token);

        var uploadVersionGroups = await context.UploadJobs
            .Where(x => x.CreatedAt >= timeInfo.Start)
            .GroupBy(x => x.Version)
            .Select(g => new
            {
                Version = g.Key,
                Count = g.Count()
            })
            .ToListAsync(token);

        var uploadStats = NormalizeUploadStats(uploadVersionGroups.Select(x => (x.Version, x.Count)));
        var uploads = uploadStats.Sum(x => x.Count);

        var totalCount = dsstatsGroup.Sum(x => x.Count) + arcadeCount;
        return new DashboardStatsResponse
        {
            Total = totalCount,
            SC2Arcade = arcadeCount,
            Dsstats = totalCount - arcadeCount,
            GameModes = dsstatsGroup,
            Uploads = uploads,
            UploadStats = uploadStats
        };
    }

    public static List<DashboardUploadSourceStats> NormalizeUploadStats(IEnumerable<(string? Version, int Count)> versionCounts)
    {
        Dictionary<string, Dictionary<string, int>> uploadsBySource = new(StringComparer.OrdinalIgnoreCase);
        foreach (var source in UploadSources)
        {
            uploadsBySource[source] = new(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var (rawVersion, count) in versionCounts)
        {
            if (count <= 0)
            {
                continue;
            }

            var (source, version) = NormalizeUploadVersion(rawVersion);
            var versions = uploadsBySource[source];
            if (!versions.TryAdd(version, count))
            {
                versions[version] += count;
            }
        }

        return UploadSources
            .Select(source =>
            {
                var versions = uploadsBySource[source]
                    .Select(x => new DashboardUploadVersionStats
                    {
                        Version = x.Key,
                        Count = x.Value
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Version, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new DashboardUploadSourceStats
                {
                    Source = source,
                    Count = versions.Sum(x => x.Count),
                    Versions = versions
                };
            })
            .ToList();
    }

    private static (string Source, string Version) NormalizeUploadVersion(string? rawVersion)
    {
        var version = rawVersion?.Trim();
        if (string.IsNullOrWhiteSpace(version))
        {
            return (MauiUploadSource, UnknownVersion);
        }

        if (version.StartsWith("myds", StringComparison.OrdinalIgnoreCase))
        {
            return (MyDsstatsUploadSource, NormalizeVersionSuffix(version["myds".Length..]));
        }

        if (version.StartsWith("ser", StringComparison.OrdinalIgnoreCase))
        {
            return (ServiceUploadSource, NormalizeVersionSuffix(version["ser".Length..]));
        }

        if (version.StartsWith("ma", StringComparison.OrdinalIgnoreCase))
        {
            return (MauiUploadSource, NormalizeVersionSuffix(version["ma".Length..]));
        }

        return (MauiUploadSource, version);
    }

    private static string NormalizeVersionSuffix(string suffix)
    {
        var version = suffix.Trim();
        return string.IsNullOrWhiteSpace(version) ? UnknownVersion : version;
    }
}
