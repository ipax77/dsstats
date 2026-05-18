using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.BuildDetails;

public sealed class BuildDetailsService(IDbContextFactory<DsstatsContext> contextFactory, IMemoryCache memoryCache) : IBuildDetailsService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(3);

    public async Task<List<BuildDetailsOverviewRow>> GetOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        try
        {
            return await memoryCache.GetOrCreateAsync($"overview_{request.GetMemKey()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var query = CreateBaseQuery(context, request);

                var rows = await query
                    .GroupBy(x => new { x.Commander, x.Build })
                    .Select(g => new
                    {
                        g.Key.Commander,
                        g.Key.Build,
                        Games = g.Count(),
                        Wins = g.Count(x => x.Won),
                        AverageRatingGain = g.Average(x => x.RatingDelta),
                        AverageRating = g.Average(x => x.RatingBefore),
                        GasFirstGames = g.Count(x => x.GasFirst),
                    })
                    .ToListAsync(token);

                return rows
                    .Select(x => new BuildDetailsOverviewRow
                    {
                        Commander = x.Commander,
                        Build = x.Build,
                        Games = x.Games,
                        Wins = x.Wins,
                        AverageRatingGain = Math.Round(x.AverageRatingGain, 2),
                        Winrate = Math.Round(100.0 * x.Wins / x.Games, 2),
                        AverageRating = Math.Round(x.AverageRating, 0),
                        GasFirstGames = x.GasFirstGames,
                        GasFirstRate = Math.Round(100.0 * x.GasFirstGames / x.Games, 2),
                    })
                    .OrderByDescending(x => x.AverageRatingGain)
                    .ThenByDescending(x => x.Games)
                    .ToList();
            }) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsMatchupRow>> GetMatchups(BuildDetailsMatchupRequest request, CancellationToken token = default)
    {
        try
        {
            return await memoryCache.GetOrCreateAsync($"matchups_{request.GetMemKey()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var query = CreateBaseQuery(context, request)
                    .Where(x => x.Commander == request.SelectedCommander
                        && x.Build == request.SelectedBuild);

                var rows = await query
                    .GroupBy(x => new { x.Commander, x.Build, x.OpponentCommander, x.OpponentBuild })
                    .Select(g => new
                    {
                        g.Key.Commander,
                        g.Key.Build,
                        g.Key.OpponentCommander,
                        g.Key.OpponentBuild,
                        Games = g.Count(),
                        Wins = g.Count(x => x.Won),
                        AverageRatingGain = g.Average(x => x.RatingDelta),
                        AverageRating = g.Average(x => x.RatingBefore),
                        SelectedGasFirstGames = g.Count(x => x.GasFirst),
                        OpponentGasFirstGames = g.Count(x => x.OpponentGasFirst),
                    })
                    .ToListAsync(token);

                return rows
                    .Select(x => new BuildDetailsMatchupRow
                    {
                        Commander = x.Commander,
                        Build = x.Build,
                        OpponentCommander = x.OpponentCommander,
                        OpponentBuild = x.OpponentBuild,
                        Games = x.Games,
                        Wins = x.Wins,
                        AverageRatingGain = Math.Round(x.AverageRatingGain, 2),
                        Winrate = Math.Round(100.0 * x.Wins / x.Games, 2),
                        AverageRating = Math.Round(x.AverageRating, 0),
                        SelectedGasFirstGames = x.SelectedGasFirstGames,
                        OpponentGasFirstGames = x.OpponentGasFirstGames,
                    })
                    .OrderByDescending(x => x.AverageRatingGain)
                    .ThenByDescending(x => x.Games)
                    .ToList();
            }) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsSampleReplay>> GetSampleReplays(BuildDetailsSamplesRequest request, CancellationToken token = default)
    {
        try
        {
            var take = Math.Clamp(request.Count, 1, 25);
            await using var context = await contextFactory.CreateDbContextAsync(token);
            var query = CreateBaseQuery(context, request)
                .Where(x => x.Commander == request.SelectedCommander
                    && x.Build == request.SelectedBuild
                    && x.OpponentCommander == request.OpponentCommander
                    && x.OpponentBuild == request.OpponentBuild);

            var rows = await query
                .OrderByDescending(x => x.Gametime)
                .Select(x => new SampleReplayProjection
                {
                    ReplayHash = x.ReplayHash,
                    Gametime = x.Gametime,
                    GameMode = x.GameMode,
                    Duration = x.Duration,
                    WinnerTeam = x.WinnerTeam,
                    PlayerPos = x.GamePos,
                    PlayerGain = x.RatingDelta,
                    Exp2Win = x.ExpectedWinProbability,
                    AvgRating = x.AvgRating,
                    LeaverType = x.LeaverType,
                    Commander = x.Commander,
                    Build = x.Build,
                    GasFirst = x.GasFirst,
                    OpponentCommander = x.OpponentCommander,
                    OpponentBuild = x.OpponentBuild,
                    OpponentGasFirst = x.OpponentGasFirst,
                    Players = context.ReplayPlayers.AsNoTracking()
                        .Where(p => p.ReplayId == x.ReplayId)
                        .OrderBy(p => p.GamePos)
                        .Select(p => new SampleReplayPlayerProjection
                        {
                            Race = p.Race,
                            Team = p.TeamId,
                        })
                        .ToList(),
                })
                .Take(take * 6)
                .ToListAsync(token);

            return rows
                .DistinctBy(x => x.ReplayHash)
                .Take(take)
                .Select(x => x.ToDto())
                .ToList();
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    private static IQueryable<BuildDetailsQueryRow> CreateBaseQuery(DsstatsContext context, BuildDetailsRequest request)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod) ?? Data.GetTimePeriodInfo(TimePeriod.Last90Days);
        var noMinRating = request.FromRating <= Data.MinBuildRating;
        var noMaxRating = request.ToRating >= Data.MaxBuildRating;
        var anyCommander = request.Commander == Commander.None;
        var playerId = request.Player?.PlayerId ?? 0;
        var noPlayer = playerId <= 0;

        var query = from build in context.ReplayPlayerBuildDetails.AsNoTracking()
                    join detail in context.ReplayBuildDetails.AsNoTracking()
                        on build.ReplayBuildDetailId equals detail.ReplayBuildDetailId
                    join replay in context.Replays.AsNoTracking()
                        on detail.ReplayId equals replay.ReplayId
                    join rating in context.ReplayPlayerRatings.AsNoTracking()
                        on build.ReplayPlayerId equals rating.ReplayPlayerId
                    join replayRating in context.ReplayRatings.AsNoTracking()
                        on rating.ReplayRatingId equals replayRating.ReplayRatingId
                    where detail.Status == ReplayBuildDetailStatus.Detected
                        && replay.GameMode == GameMode.Standard
                        && replay.PlayerCount == 6
                        && replay.WinnerTeam > 0
                        && (request.TeFilter == BuildDetailsTeFilter.All
                            || (request.TeFilter == BuildDetailsTeFilter.TE && replay.TE)
                            || (request.TeFilter == BuildDetailsTeFilter.NonTE && !replay.TE))
                        && replay.Gametime >= timeInfo.Start
                        && (!timeInfo.HasEnd || replay.Gametime < timeInfo.End)
                        && ((replay.TE
                                && rating.RatingType == RatingType.StandardTE
                                && replayRating.RatingType == RatingType.StandardTE)
                            || (!replay.TE
                                && rating.RatingType == RatingType.Standard
                                && replayRating.RatingType == RatingType.Standard))
                        && (request.WithLeavers || replayRating.LeaverType == LeaverType.None)
                        && (noMinRating || rating.RatingBefore >= request.FromRating)
                        && (noMaxRating || rating.RatingBefore <= request.ToRating)
                        && (anyCommander || build.Commander == request.Commander)
                        && (noPlayer || rating.PlayerId == playerId)
                    select new BuildDetailsQueryRow
                    {
                        ReplayId = replay.ReplayId,
                        ReplayHash = replay.ReplayHash,
                        Gametime = replay.Gametime,
                        GameMode = replay.GameMode,
                        Duration = replay.Duration,
                        WinnerTeam = replay.WinnerTeam,
                        GamePos = build.GamePos,
                        Commander = build.Commander,
                        Build = build.Build,
                        GasFirst = build.GasFirst,
                        OpponentCommander = build.OppCommander,
                        OpponentBuild = build.OppBuild,
                        OpponentGasFirst = build.OppGasFirst,
                        Won = build.Won,
                        RatingBefore = rating.RatingBefore,
                        RatingDelta = rating.RatingDelta,
                        ExpectedWinProbability = replayRating.ExpectedWinProbability,
                        AvgRating = replayRating.AvgRating,
                        LeaverType = replayRating.LeaverType,
                    };

        return request.GasFilter switch
        {
            BuildDetailsGasFilter.WithGas => query.Where(x => x.GasFirst),
            BuildDetailsGasFilter.WithoutGas => query.Where(x => !x.GasFirst),
            _ => query,
        };
    }

    private sealed class BuildDetailsQueryRow
    {
        public int ReplayId { get; init; }
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public int GamePos { get; init; }
        public Commander Commander { get; init; }
        public int Build { get; init; }
        public bool GasFirst { get; init; }
        public Commander OpponentCommander { get; init; }
        public int OpponentBuild { get; init; }
        public bool OpponentGasFirst { get; init; }
        public bool Won { get; init; }
        public double RatingBefore { get; init; }
        public double RatingDelta { get; init; }
        public double ExpectedWinProbability { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }
    }

    private sealed class SampleReplayProjection
    {
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public int PlayerPos { get; init; }
        public double PlayerGain { get; init; }
        public double Exp2Win { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }
        public Commander Commander { get; init; }
        public int Build { get; init; }
        public bool GasFirst { get; init; }
        public Commander OpponentCommander { get; init; }
        public int OpponentBuild { get; init; }
        public bool OpponentGasFirst { get; init; }
        public List<SampleReplayPlayerProjection> Players { get; init; } = [];

        public BuildDetailsSampleReplay ToDto()
        {
            return new BuildDetailsSampleReplay
            {
                Commander = Commander,
                Build = Build,
                GasFirst = GasFirst,
                OpponentCommander = OpponentCommander,
                OpponentBuild = OpponentBuild,
                OpponentGasFirst = OpponentGasFirst,
                Replay = new ReplayListDto
                {
                    ReplayHash = ReplayHash,
                    Gametime = Gametime,
                    GameMode = GameMode,
                    Duration = Duration,
                    WinnerTeam = WinnerTeam,
                    CommandersTeam1 = Players.Where(x => x.Team == 1).Select(x => x.Race).ToList(),
                    CommandersTeam2 = Players.Where(x => x.Team == 2).Select(x => x.Race).ToList(),
                    Exp2Win = Exp2Win,
                    AvgRating = AvgRating,
                    LeaverType = LeaverType,
                    PlayerPos = PlayerPos,
                    PlayerGain = PlayerGain,
                }
            };
        }
    }

    private sealed class SampleReplayPlayerProjection
    {
        public Commander Race { get; init; }
        public int Team { get; init; }
    }
}
