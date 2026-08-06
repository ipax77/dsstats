using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public partial class DashboardStatsService(IDbContextFactory<DsstatsContext> contextFactory, IMemoryCache memoryCache) : IDashboardStatsService
{
    private const string MauiUploadSource = "maui";
    private const string MyDsstatsUploadSource = "mydsstats";
    private const string ServiceUploadSource = "service";
    private const string ApiUploadSource = "api";
    private const string UnknownVersion = "unknown";
    private static readonly string[] UploadSources =
        [MauiUploadSource, MyDsstatsUploadSource, ServiceUploadSource, ApiUploadSource];

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
            .GroupBy(x => new { x.DecoderSource, x.DecoderVersion })
            .Select(g => new
            {
                g.Key.DecoderSource,
                g.Key.DecoderVersion,
                Count = g.Count()
            })
            .ToListAsync(token);

        var replayUploadVersionGroups = await context.ReplayUploadJobs
            .Where(x => x.CreatedAt >= timeInfo.Start)
            .GroupBy(x => new { x.DecoderSource, x.DecoderVersion })
            .Select(g => new
            {
                g.Key.DecoderSource,
                g.Key.DecoderVersion,
                Count = g.Count()
            })
            .ToListAsync(token);

        List<(ReplayDecoderSource? Source, string? DecoderVersion, string? RawVersion, int Count)> normalizedGroups =
            new(uploadVersionGroups.Count + replayUploadVersionGroups.Count);
        normalizedGroups.AddRange(uploadVersionGroups.Select(x =>
            (x.DecoderSource, (string?)x.DecoderVersion, (string?)null, x.Count)));
        normalizedGroups.AddRange(replayUploadVersionGroups.Select(x =>
            ((ReplayDecoderSource?)(x.DecoderSource ?? ReplayDecoderSource.Api),
             (string?)(x.DecoderVersion ?? UnknownVersion),
             (string?)null,
             x.Count)));

        var uploadStats = NormalizeUploadStats(normalizedGroups);
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
        => NormalizeUploadStats(versionCounts.Select(x =>
            ((ReplayDecoderSource?)null, (string?)null, x.Version, x.Count)));

    public static List<DashboardUploadSourceStats> NormalizeUploadStats(
        IEnumerable<(ReplayDecoderSource? Source, string? DecoderVersion, string? RawVersion, int Count)> versionCounts)
    {
        Dictionary<string, Dictionary<string, int>> uploadsBySource = new(StringComparer.OrdinalIgnoreCase);
        foreach (var source in UploadSources)
        {
            uploadsBySource[source] = new(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var (decoderSource, decoderVersion, rawVersion, count) in versionCounts)
        {
            if (count <= 0)
            {
                continue;
            }

            var (source, version) = decoderSource is null
                ? NormalizeUploadVersion(rawVersion)
                : (GetSourceName(decoderSource.Value), NormalizeVersionSuffix(decoderVersion ?? string.Empty));
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
        var parsed = ReplayDecoderVersion.Parse(rawVersion);
        return (GetSourceName(parsed.Source), parsed.Version);
    }

    private static string GetSourceName(ReplayDecoderSource source) => source switch
    {
        ReplayDecoderSource.MyDsstats => MyDsstatsUploadSource,
        ReplayDecoderSource.Service => ServiceUploadSource,
        ReplayDecoderSource.Api => ApiUploadSource,
        _ => MauiUploadSource,
    };

    private static string NormalizeVersionSuffix(string suffix)
    {
        var version = suffix.Trim();
        return string.IsNullOrWhiteSpace(version) ? UnknownVersion : version;
    }
}
