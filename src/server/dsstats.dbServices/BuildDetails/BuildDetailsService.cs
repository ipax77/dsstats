using dsstats.db;
using dsstats.shared;
using dsstats.shared.DetailBuild;
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

    public async Task<List<BuildDetailsTeamBuildOverviewRow>> GetTeamBuildOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        try
        {
            return await memoryCache.GetOrCreateAsync($"team_build_overview_{request.GetMemKey()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var query = CreateTeamBuildBaseQuery(context, request);

                var rows = await query
                    .GroupBy(x => x.TeamBuild)
                    .Select(g => new
                    {
                        TeamBuild = g.Key,
                        Games = g.Count(),
                        Wins = g.Count(x => x.Won),
                        AverageRatingGain = g.Average(x => x.TeamRatingDelta),
                        AverageRating = g.Average(x => x.TeamRatingBefore),
                    })
                    .ToListAsync(token);

                return rows
                    .Select(x => new BuildDetailsTeamBuildOverviewRow
                    {
                        TeamBuild = x.TeamBuild,
                        Games = x.Games,
                        Wins = x.Wins,
                        AverageRatingGain = Math.Round(x.AverageRatingGain, 2),
                        Winrate = Math.Round(100.0 * x.Wins / x.Games, 2),
                        AverageRating = Math.Round(x.AverageRating, 0),
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

    public async Task<List<BuildDetailsTeamBuildSampleReplay>> GetTeamBuildSampleReplays(BuildDetailsTeamBuildSamplesRequest request, CancellationToken token = default)
    {
        try
        {
            var take = Math.Clamp(request.Count, 1, 25);
            await using var context = await contextFactory.CreateDbContextAsync(token);
            var query = CreateTeamBuildBaseQuery(context, request)
                .Where(x => x.TeamBuild == request.SelectedTeamBuild);

            var rows = await query
                .OrderByDescending(x => x.Gametime)
                .Select(x => new TeamBuildSampleReplayProjection
                {
                    ReplayId = x.ReplayId,
                    ReplayHash = x.ReplayHash,
                    Gametime = x.Gametime,
                    GameMode = x.GameMode,
                    Duration = x.Duration,
                    WinnerTeam = x.WinnerTeam,
                    TeamBuild = x.TeamBuild,
                    TeamId = x.TeamId,
                    LeaderGamePos = x.LeaderGamePos,
                    LeaderCommander = x.LeaderCommander,
                    LeaderBuild = x.LeaderBuild,
                    LeaderGasFirst = x.LeaderGasFirst,
                    LeaderGain = x.LeaderRatingDelta,
                    FollowerGamePos = x.FollowerGamePos,
                    FollowerCommander = x.FollowerCommander,
                    FollowerBuild = x.FollowerBuild,
                    FollowerGasFirst = x.FollowerGasFirst,
                    FollowerGain = x.FollowerRatingDelta,
                    Exp2Win = x.ExpectedWinProbability,
                    AvgRating = x.AvgRating,
                    LeaverType = x.LeaverType,
                    TeamRatingDelta = x.TeamRatingDelta,
                })
                .Take(take * 2)
                .ToListAsync(token);

            rows = rows
                .DistinctBy(x => x.ReplayHash)
                .Take(take)
                .ToList();

            var replayIds = rows.Select(x => x.ReplayId).ToList();
            var players = await context.ReplayPlayers.AsNoTracking()
                .Where(x => replayIds.Contains(x.ReplayId))
                .OrderBy(x => x.GamePos)
                .Select(x => new SampleReplayPlayerProjection
                {
                    ReplayId = x.ReplayId,
                    Race = x.Race,
                    Team = x.TeamId,
                })
                .ToListAsync(token);

            var playersByReplayId = players
                .GroupBy(x => x.ReplayId)
                .ToDictionary(x => x.Key, x => x.ToList());

            return rows
                .Select(x => x.ToDto(playersByReplayId.GetValueOrDefault(x.ReplayId) ?? []))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsRaceRosterOverviewRow>> GetRaceRosterOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        if (!IsAllowedRaceRosterCommander(request.Commander))
        {
            return [];
        }

        try
        {
            return await memoryCache.GetOrCreateAsync($"race_roster_overview_{request.GetMemKey()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var query = CreateRaceRosterBaseQuery(context, request);

                var rows = await query
                    .GroupBy(x => new { x.Race1, x.Race2, x.Race3 })
                    .Select(g => new
                    {
                        g.Key.Race1,
                        g.Key.Race2,
                        g.Key.Race3,
                        Games = g.Count(),
                        Wins = g.Count(x => x.Won),
                        AverageRatingGain = g.Average(x => x.TeamRatingDelta),
                        AverageRating = g.Average(x => x.TeamRatingBefore),
                    })
                    .ToListAsync(token);

                return rows
                    .Select(x => new BuildDetailsRaceRosterOverviewRow
                    {
                        Race1 = x.Race1,
                        Race2 = x.Race2,
                        Race3 = x.Race3,
                        Games = x.Games,
                        Wins = x.Wins,
                        AverageRatingGain = Math.Round(x.AverageRatingGain, 2),
                        Winrate = Math.Round(100.0 * x.Wins / x.Games, 2),
                        AverageRating = Math.Round(x.AverageRating, 0),
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

    public async Task<List<BuildDetailsRaceRosterMatchupRow>> GetRaceRosterMatchups(BuildDetailsRaceRosterMatchupRequest request, CancellationToken token = default)
    {
        if (!IsAllowedRaceRosterCommander(request.Commander)
            || !IsStandardRace(request.Race1)
            || !IsStandardRace(request.Race2)
            || !IsStandardRace(request.Race3))
        {
            return [];
        }

        try
        {
            return await memoryCache.GetOrCreateAsync($"race_roster_matchups_{request.GetMemKey()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var query = CreateRaceRosterBaseQuery(context, request)
                    .Where(x => x.Race1 == request.Race1
                        && x.Race2 == request.Race2
                        && x.Race3 == request.Race3);

                var rows = await query
                    .GroupBy(x => new { x.Race1, x.Race2, x.Race3, x.OpponentRace1, x.OpponentRace2, x.OpponentRace3 })
                    .Select(g => new
                    {
                        g.Key.Race1,
                        g.Key.Race2,
                        g.Key.Race3,
                        g.Key.OpponentRace1,
                        g.Key.OpponentRace2,
                        g.Key.OpponentRace3,
                        Games = g.Count(),
                        Wins = g.Count(x => x.Won),
                        AverageRatingGain = g.Average(x => x.TeamRatingDelta),
                        AverageRating = g.Average(x => x.TeamRatingBefore),
                    })
                    .ToListAsync(token);

                return rows
                    .Select(x => new BuildDetailsRaceRosterMatchupRow
                    {
                        Race1 = x.Race1,
                        Race2 = x.Race2,
                        Race3 = x.Race3,
                        OpponentRace1 = x.OpponentRace1,
                        OpponentRace2 = x.OpponentRace2,
                        OpponentRace3 = x.OpponentRace3,
                        Games = x.Games,
                        Wins = x.Wins,
                        AverageRatingGain = Math.Round(x.AverageRatingGain, 2),
                        Winrate = Math.Round(100.0 * x.Wins / x.Games, 2),
                        AverageRating = Math.Round(x.AverageRating, 0),
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

    public async Task<List<BuildDetailsRaceRosterSampleReplay>> GetRaceRosterSampleReplays(BuildDetailsRaceRosterSamplesRequest request, CancellationToken token = default)
    {
        if (!IsAllowedRaceRosterCommander(request.Commander)
            || !IsStandardRace(request.Race1)
            || !IsStandardRace(request.Race2)
            || !IsStandardRace(request.Race3)
            || !IsStandardRace(request.OpponentRace1)
            || !IsStandardRace(request.OpponentRace2)
            || !IsStandardRace(request.OpponentRace3))
        {
            return [];
        }

        try
        {
            var take = Math.Clamp(request.Count, 1, 25);
            await using var context = await contextFactory.CreateDbContextAsync(token);
            var query = CreateRaceRosterBaseQuery(context, request)
                .Where(x => x.Race1 == request.Race1
                    && x.Race2 == request.Race2
                    && x.Race3 == request.Race3
                    && x.OpponentRace1 == request.OpponentRace1
                    && x.OpponentRace2 == request.OpponentRace2
                    && x.OpponentRace3 == request.OpponentRace3);

            var rows = await query
                .OrderByDescending(x => x.Gametime)
                .Select(x => new RaceRosterSampleReplayProjection
                {
                    ReplayId = x.ReplayId,
                    ReplayHash = x.ReplayHash,
                    Gametime = x.Gametime,
                    GameMode = x.GameMode,
                    Duration = x.Duration,
                    WinnerTeam = x.WinnerTeam,
                    TeamId = x.TeamId,
                    FirstGamePos = x.FirstGamePos,
                    Race1 = x.Race1,
                    Race2 = x.Race2,
                    Race3 = x.Race3,
                    OpponentRace1 = x.OpponentRace1,
                    OpponentRace2 = x.OpponentRace2,
                    OpponentRace3 = x.OpponentRace3,
                    TeamRatingDelta = x.TeamRatingDelta,
                    Exp2Win = x.ExpectedWinProbability,
                    AvgRating = x.AvgRating,
                    LeaverType = x.LeaverType,
                })
                .Take(take * 2)
                .ToListAsync(token);

            rows = rows
                .DistinctBy(x => x.ReplayHash)
                .Take(take)
                .ToList();

            var replayIds = rows.Select(x => x.ReplayId).ToList();
            var players = await context.ReplayPlayers.AsNoTracking()
                .Where(x => replayIds.Contains(x.ReplayId))
                .OrderBy(x => x.GamePos)
                .Select(x => new SampleReplayPlayerProjection
                {
                    ReplayId = x.ReplayId,
                    Race = x.Race,
                    Team = x.TeamId,
                })
                .ToListAsync(token);

            var playersByReplayId = players
                .GroupBy(x => x.ReplayId)
                .ToDictionary(x => x.Key, x => x.ToList());

            return rows
                .Select(x => x.ToDto(playersByReplayId.GetValueOrDefault(x.ReplayId) ?? []))
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

    private static IQueryable<TeamBuildQueryRow> CreateTeamBuildBaseQuery(DsstatsContext context, BuildDetailsRequest request)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod) ?? Data.GetTimePeriodInfo(TimePeriod.Last90Days);
        var noMinRating = request.FromRating <= Data.MinBuildRating;
        var noMaxRating = request.ToRating >= Data.MaxBuildRating;
        var anyCommander = request.Commander == Commander.None;
        var playerId = request.Player?.PlayerId ?? 0;
        var noPlayer = playerId <= 0;

        var query = from teamBuild in context.ReplayTeamBuildDetails.AsNoTracking()
                    join detail in context.ReplayBuildDetails.AsNoTracking()
                        on teamBuild.ReplayBuildDetailId equals detail.ReplayBuildDetailId
                    join replay in context.Replays.AsNoTracking()
                        on detail.ReplayId equals replay.ReplayId
                    join replayRating in context.ReplayRatings.AsNoTracking()
                        on replay.ReplayId equals replayRating.ReplayId
                    join leaderRating in context.ReplayPlayerRatings.AsNoTracking()
                        on teamBuild.LeaderReplayPlayerId equals leaderRating.ReplayPlayerId
                    join followerRating in context.ReplayPlayerRatings.AsNoTracking()
                        on teamBuild.FollowerReplayPlayerId equals followerRating.ReplayPlayerId
                    join leaderBuild in context.ReplayPlayerBuildDetails.AsNoTracking()
                        on new { teamBuild.ReplayBuildDetailId, ReplayPlayerId = teamBuild.LeaderReplayPlayerId }
                        equals new { leaderBuild.ReplayBuildDetailId, leaderBuild.ReplayPlayerId }
                    join followerBuild in context.ReplayPlayerBuildDetails.AsNoTracking()
                        on new { teamBuild.ReplayBuildDetailId, ReplayPlayerId = teamBuild.FollowerReplayPlayerId }
                        equals new { followerBuild.ReplayBuildDetailId, followerBuild.ReplayPlayerId }
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
                                && replayRating.RatingType == RatingType.StandardTE
                                && leaderRating.RatingType == RatingType.StandardTE
                                && followerRating.RatingType == RatingType.StandardTE)
                            || (!replay.TE
                                && replayRating.RatingType == RatingType.Standard
                                && leaderRating.RatingType == RatingType.Standard
                                && followerRating.RatingType == RatingType.Standard))
                        && (request.WithLeavers || replayRating.LeaverType == LeaverType.None)
                        && (noMinRating || ((leaderRating.RatingBefore + followerRating.RatingBefore) / 2) >= request.FromRating)
                        && (noMaxRating || ((leaderRating.RatingBefore + followerRating.RatingBefore) / 2) <= request.ToRating)
                        && (anyCommander || leaderBuild.Commander == request.Commander || followerBuild.Commander == request.Commander)
                        && (noPlayer || leaderRating.PlayerId == playerId || followerRating.PlayerId == playerId)
                    select new TeamBuildQueryRow
                    {
                        ReplayId = replay.ReplayId,
                        ReplayHash = replay.ReplayHash,
                        Gametime = replay.Gametime,
                        GameMode = replay.GameMode,
                        Duration = replay.Duration,
                        WinnerTeam = replay.WinnerTeam,
                        TeamId = teamBuild.TeamId,
                        TeamBuild = teamBuild.TeamBuild,
                        LeaderGamePos = leaderBuild.GamePos,
                        LeaderCommander = leaderBuild.Commander,
                        LeaderBuild = leaderBuild.Build,
                        LeaderGasFirst = leaderBuild.GasFirst,
                        LeaderRatingBefore = leaderRating.RatingBefore,
                        LeaderRatingDelta = leaderRating.RatingDelta,
                        FollowerGamePos = followerBuild.GamePos,
                        FollowerCommander = followerBuild.Commander,
                        FollowerBuild = followerBuild.Build,
                        FollowerGasFirst = followerBuild.GasFirst,
                        FollowerRatingBefore = followerRating.RatingBefore,
                        FollowerRatingDelta = followerRating.RatingDelta,
                        Won = replay.WinnerTeam == teamBuild.TeamId,
                        TeamRatingBefore = (leaderRating.RatingBefore + followerRating.RatingBefore) / 2,
                        TeamRatingDelta = (leaderRating.RatingDelta + followerRating.RatingDelta) / 2,
                        ExpectedWinProbability = replayRating.ExpectedWinProbability,
                        AvgRating = replayRating.AvgRating,
                        LeaverType = replayRating.LeaverType,
                    };

        return query;
    }

    private static IQueryable<RaceRosterQueryRow> CreateRaceRosterBaseQuery(DsstatsContext context, BuildDetailsRequest request)
    {
        return CreateRaceRosterTeamQuery(context, request, 1, 1, 2, 3, 4, 5, 6)
            .Concat(CreateRaceRosterTeamQuery(context, request, 2, 4, 5, 6, 1, 2, 3));
    }

    private static IQueryable<RaceRosterQueryRow> CreateRaceRosterTeamQuery(
        DsstatsContext context,
        BuildDetailsRequest request,
        int teamId,
        int pos1,
        int pos2,
        int pos3,
        int oppPos1,
        int oppPos2,
        int oppPos3)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod) ?? Data.GetTimePeriodInfo(TimePeriod.Last90Days);
        var noMinRating = request.FromRating <= Data.MinBuildRating;
        var noMaxRating = request.ToRating >= Data.MaxBuildRating;
        var anyCommander = request.Commander == Commander.None;
        var playerId = request.Player?.PlayerId ?? 0;
        var noPlayer = playerId <= 0;

        return from replay in context.Replays.AsNoTracking()
               join replayRating in context.ReplayRatings.AsNoTracking()
                   on replay.ReplayId equals replayRating.ReplayId
               join p1 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = pos1 } equals new { p1.ReplayId, p1.GamePos }
               join p2 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = pos2 } equals new { p2.ReplayId, p2.GamePos }
               join p3 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = pos3 } equals new { p3.ReplayId, p3.GamePos }
               join o1 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = oppPos1 } equals new { o1.ReplayId, o1.GamePos }
               join o2 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = oppPos2 } equals new { o2.ReplayId, o2.GamePos }
               join o3 in context.ReplayPlayers.AsNoTracking()
                   on new { replay.ReplayId, GamePos = oppPos3 } equals new { o3.ReplayId, o3.GamePos }
               join r1 in context.ReplayPlayerRatings.AsNoTracking()
                   on new { replayRating.ReplayRatingId, p1.ReplayPlayerId } equals new { r1.ReplayRatingId, r1.ReplayPlayerId }
               join r2 in context.ReplayPlayerRatings.AsNoTracking()
                   on new { replayRating.ReplayRatingId, p2.ReplayPlayerId } equals new { r2.ReplayRatingId, r2.ReplayPlayerId }
               join r3 in context.ReplayPlayerRatings.AsNoTracking()
                   on new { replayRating.ReplayRatingId, p3.ReplayPlayerId } equals new { r3.ReplayRatingId, r3.ReplayPlayerId }
               where replay.GameMode == GameMode.Standard
                   && replay.PlayerCount == 6
                   && replay.WinnerTeam > 0
                   && p1.TeamId == teamId
                   && p2.TeamId == teamId
                   && p3.TeamId == teamId
                   && o1.TeamId != teamId
                   && o2.TeamId != teamId
                   && o3.TeamId != teamId
                   && (p1.Race == Commander.Protoss || p1.Race == Commander.Terran || p1.Race == Commander.Zerg)
                   && (p2.Race == Commander.Protoss || p2.Race == Commander.Terran || p2.Race == Commander.Zerg)
                   && (p3.Race == Commander.Protoss || p3.Race == Commander.Terran || p3.Race == Commander.Zerg)
                   && (o1.Race == Commander.Protoss || o1.Race == Commander.Terran || o1.Race == Commander.Zerg)
                   && (o2.Race == Commander.Protoss || o2.Race == Commander.Terran || o2.Race == Commander.Zerg)
                   && (o3.Race == Commander.Protoss || o3.Race == Commander.Terran || o3.Race == Commander.Zerg)
                   && (request.TeFilter == BuildDetailsTeFilter.All
                       || (request.TeFilter == BuildDetailsTeFilter.TE && replay.TE)
                       || (request.TeFilter == BuildDetailsTeFilter.NonTE && !replay.TE))
                   && replay.Gametime >= timeInfo.Start
                   && (!timeInfo.HasEnd || replay.Gametime < timeInfo.End)
                   && ((replay.TE
                           && replayRating.RatingType == RatingType.StandardTE
                           && r1.RatingType == RatingType.StandardTE
                           && r2.RatingType == RatingType.StandardTE
                           && r3.RatingType == RatingType.StandardTE)
                       || (!replay.TE
                           && replayRating.RatingType == RatingType.Standard
                           && r1.RatingType == RatingType.Standard
                           && r2.RatingType == RatingType.Standard
                           && r3.RatingType == RatingType.Standard))
                   && (request.WithLeavers || replayRating.LeaverType == LeaverType.None)
                   && (noMinRating || ((r1.RatingBefore + r2.RatingBefore + r3.RatingBefore) / 3.0) >= request.FromRating)
                   && (noMaxRating || ((r1.RatingBefore + r2.RatingBefore + r3.RatingBefore) / 3.0) <= request.ToRating)
                   && (anyCommander || p1.Race == request.Commander || p2.Race == request.Commander || p3.Race == request.Commander)
                   && (noPlayer || r1.PlayerId == playerId || r2.PlayerId == playerId || r3.PlayerId == playerId)
               select new RaceRosterQueryRow
               {
                   ReplayId = replay.ReplayId,
                   ReplayHash = replay.ReplayHash,
                   Gametime = replay.Gametime,
                   GameMode = replay.GameMode,
                   Duration = replay.Duration,
                   WinnerTeam = replay.WinnerTeam,
                   TeamId = teamId,
                   FirstGamePos = pos1,
                   Race1 = p1.Race,
                   Race2 = p2.Race,
                   Race3 = p3.Race,
                   OpponentRace1 = o1.Race,
                   OpponentRace2 = o2.Race,
                   OpponentRace3 = o3.Race,
                   Won = replay.WinnerTeam == teamId,
                   TeamRatingBefore = (r1.RatingBefore + r2.RatingBefore + r3.RatingBefore) / 3.0,
                   TeamRatingDelta = (r1.RatingDelta + r2.RatingDelta + r3.RatingDelta) / 3.0,
                   ExpectedWinProbability = replayRating.ExpectedWinProbability,
                   AvgRating = replayRating.AvgRating,
                   LeaverType = replayRating.LeaverType,
               };
    }

    private static bool IsAllowedRaceRosterCommander(Commander commander)
    {
        return commander == Commander.None || IsStandardRace(commander);
    }

    private static bool IsStandardRace(Commander commander)
    {
        return commander is Commander.Protoss or Commander.Terran or Commander.Zerg;
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
        public int ReplayId { get; init; }
        public Commander Race { get; init; }
        public int Team { get; init; }
    }

    private sealed class TeamBuildQueryRow
    {
        public int ReplayId { get; init; }
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public int TeamId { get; init; }
        public TeamBuild TeamBuild { get; init; }
        public int LeaderGamePos { get; init; }
        public Commander LeaderCommander { get; init; }
        public int LeaderBuild { get; init; }
        public bool LeaderGasFirst { get; init; }
        public double LeaderRatingBefore { get; init; }
        public double LeaderRatingDelta { get; init; }
        public int FollowerGamePos { get; init; }
        public Commander FollowerCommander { get; init; }
        public int FollowerBuild { get; init; }
        public bool FollowerGasFirst { get; init; }
        public double FollowerRatingBefore { get; init; }
        public double FollowerRatingDelta { get; init; }
        public bool Won { get; init; }
        public double TeamRatingBefore { get; init; }
        public double TeamRatingDelta { get; init; }
        public double ExpectedWinProbability { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }
    }

    private sealed class TeamBuildSampleReplayProjection
    {
        public int ReplayId { get; init; }
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public TeamBuild TeamBuild { get; init; }
        public int TeamId { get; init; }
        public int LeaderGamePos { get; init; }
        public Commander LeaderCommander { get; init; }
        public int LeaderBuild { get; init; }
        public bool LeaderGasFirst { get; init; }
        public double LeaderGain { get; init; }
        public int FollowerGamePos { get; init; }
        public Commander FollowerCommander { get; init; }
        public int FollowerBuild { get; init; }
        public bool FollowerGasFirst { get; init; }
        public double FollowerGain { get; init; }
        public double Exp2Win { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }
        public double TeamRatingDelta { get; init; }

        public BuildDetailsTeamBuildSampleReplay ToDto(List<SampleReplayPlayerProjection> players)
        {
            return new BuildDetailsTeamBuildSampleReplay
            {
                TeamBuild = TeamBuild,
                LeaderGamePos = LeaderGamePos,
                LeaderCommander = LeaderCommander,
                LeaderBuild = LeaderBuild,
                LeaderGasFirst = LeaderGasFirst,
                LeaderGain = LeaderGain,
                FollowerGamePos = FollowerGamePos,
                FollowerCommander = FollowerCommander,
                FollowerBuild = FollowerBuild,
                FollowerGasFirst = FollowerGasFirst,
                FollowerGain = FollowerGain,
                Replay = new ReplayListDto
                {
                    ReplayHash = ReplayHash,
                    Gametime = Gametime,
                    GameMode = GameMode,
                    Duration = Duration,
                    WinnerTeam = WinnerTeam,
                    CommandersTeam1 = players.Where(x => x.Team == 1).Select(x => x.Race).ToList(),
                    CommandersTeam2 = players.Where(x => x.Team == 2).Select(x => x.Race).ToList(),
                    Exp2Win = Exp2Win,
                    AvgRating = AvgRating,
                    LeaverType = LeaverType,
                    PlayerPos = LeaderGamePos,
                    PlayerGain = TeamRatingDelta,
                }
            };
        }
    }

    private sealed class RaceRosterQueryRow
    {
        public int ReplayId { get; init; }
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public int TeamId { get; init; }
        public int FirstGamePos { get; init; }
        public Commander Race1 { get; init; }
        public Commander Race2 { get; init; }
        public Commander Race3 { get; init; }
        public Commander OpponentRace1 { get; init; }
        public Commander OpponentRace2 { get; init; }
        public Commander OpponentRace3 { get; init; }
        public bool Won { get; init; }
        public double TeamRatingBefore { get; init; }
        public double TeamRatingDelta { get; init; }
        public double ExpectedWinProbability { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }
    }

    private sealed class RaceRosterSampleReplayProjection
    {
        public int ReplayId { get; init; }
        public string ReplayHash { get; init; } = string.Empty;
        public DateTime Gametime { get; init; }
        public GameMode GameMode { get; init; }
        public int Duration { get; init; }
        public int WinnerTeam { get; init; }
        public int TeamId { get; init; }
        public int FirstGamePos { get; init; }
        public Commander Race1 { get; init; }
        public Commander Race2 { get; init; }
        public Commander Race3 { get; init; }
        public Commander OpponentRace1 { get; init; }
        public Commander OpponentRace2 { get; init; }
        public Commander OpponentRace3 { get; init; }
        public double TeamRatingDelta { get; init; }
        public double Exp2Win { get; init; }
        public int AvgRating { get; init; }
        public LeaverType LeaverType { get; init; }

        public BuildDetailsRaceRosterSampleReplay ToDto(List<SampleReplayPlayerProjection> players)
        {
            return new BuildDetailsRaceRosterSampleReplay
            {
                Race1 = Race1,
                Race2 = Race2,
                Race3 = Race3,
                OpponentRace1 = OpponentRace1,
                OpponentRace2 = OpponentRace2,
                OpponentRace3 = OpponentRace3,
                Replay = new ReplayListDto
                {
                    ReplayHash = ReplayHash,
                    Gametime = Gametime,
                    GameMode = GameMode,
                    Duration = Duration,
                    WinnerTeam = WinnerTeam,
                    CommandersTeam1 = players.Where(x => x.Team == 1).Select(x => x.Race).ToList(),
                    CommandersTeam2 = players.Where(x => x.Team == 2).Select(x => x.Race).ToList(),
                    Exp2Win = Exp2Win,
                    AvgRating = AvgRating,
                    LeaverType = LeaverType,
                    PlayerPos = FirstGamePos,
                    PlayerGain = TeamRatingDelta,
                }
            };
        }
    }
}
