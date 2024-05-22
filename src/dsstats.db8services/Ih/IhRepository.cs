using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public partial class IhRepository(ReplayContext context,
                                  IServiceScopeFactory scopeFactory,
                                  IMapper mapper,
                                  ILogger<IhRepository> logger) : IIhRepository
{
    public async Task<GroupStateV2?> GetOpenGroupState(Guid groupId)
    {
        return await context.IhSessions
            .Where(x => !x.Closed && x.GroupId == groupId)
            .Select(s => s.GroupStateV2)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetIhSessionsCount(CancellationToken token = default)
    {
        return await context.IhSessions
            .Where(x => x.Closed)
            .CountAsync(token);
    }

    public async Task<List<IhSessionListDto>> GetIhSessions(int skip, int take, CancellationToken token)
    {
        return await context.IhSessions
            .Where(x => x.Closed)
            .OrderByDescending(o => o.Created)
            .Skip(skip)
            .Take(take)
            .ProjectTo<IhSessionListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    public async Task<IhSessionDto?> GetIhSession(Guid groupId)
    {
        return await context.IhSessions
            .Where(x => x.GroupId == groupId && x.Closed)
            .ProjectTo<IhSessionDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<GroupStateV2> GetOrCreateGroupState(Guid groupId, RatingType ratingType = RatingType.StdTE)
    {
        var ihSession = await context.IhSessions.FirstOrDefaultAsync(f => f.GroupId == groupId);
        DateTime created = DateTime.UtcNow;

        if (ihSession is null)
        {
            ihSession = new()
            {
                RatingType = ratingType,
                GroupId = groupId,
                Created = created,
                GroupStateV2 = new()
                {
                    RatingType = ratingType,
                    GroupId = groupId,
                    Created = created
                }
            };
            context.IhSessions.Add(ihSession);
            await context.SaveChangesAsync();
        }

        if (ihSession.GroupStateV2 is null)
        {
            ihSession.GroupStateV2 = new()
            {
                RatingType = ratingType,
                GroupId = groupId,
                Created = created
            };
        }
        return ihSession.GroupStateV2;
    }

    public async Task UpdateGroupState(GroupStateV2 groupState)
    {
        var ihSession = await context.IhSessions.FirstOrDefaultAsync(f => f.GroupId == groupState.GroupId);

        if (ihSession is null)
        {
            return;
        }

        ihSession.Players = Math.Max(ihSession.Players, groupState.PlayerStates.Count);
        ihSession.Games = groupState.ReplayHashes.Count;
        ihSession.GroupStateV2 = groupState;

        IProperty property = context.Entry(ihSession).Property(nameof(IhSession.GroupStateV2)).Metadata;
        context.Entry(ihSession).Property(property).IsModified = true;

        await context.SaveChangesAsync();
    }

    public async Task<List<GroupStateDto>> GetOpenGroups()
    {
        return await context.IhSessions
            .Where(x => !x.Closed)
            .OrderByDescending(o => o.Created)
            .Select(s => new GroupStateDto()
            {
                RatingType = s.RatingType,
                GroupId = s.GroupId,
                Visitors = s.Players,
                Created = s.Created
            }).ToListAsync();
    }

    public async Task CloseGroup(Guid groupId)
    {
        try
        {
            await context.IhSessions
                .Where(x => x.GroupId == groupId)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.Closed, true));
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting group state to closed: {error}", ex.Message);
        }
    }

    public async Task<List<ReplayListDto>> GetReplays(Guid groupId)
    {
        var replayHashes = await context.IhSessions
            .Where(x => x.GroupId == groupId
                && x.GroupStateV2 != null)
            .Select(s => s.GroupStateV2!.ReplayHashes)
            .FirstOrDefaultAsync();

        if (replayHashes is null || replayHashes.Count == 0)
        {
            return [];
        }

        return await context.Replays
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .Select(s => new ReplayListDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver,
                Exp2Win = s.ReplayRatingInfo == null ? 0 : s.ReplayRatingInfo.ExpectationToWin,
            })
            .OrderByDescending(o => o.GameTime)
            .ToListAsync();
    }

    public async Task CalculatePerformance(GroupStateV2 groupState)
    {
        var replayHashes = groupState.ReplayHashes;

        if (replayHashes is null || replayHashes.Count == 0)
        {
            return;
        }

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.ReplayPlayerRatingInfo)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .OrderBy(o => o.GameTime)
            .ToListAsync();

        Dictionary<PlayerId, PerformanceHelper> playerIds = [];

        foreach (var replay in replays)
        {
            if (replay.ReplayPlayers.Any(a => a.ReplayPlayerRatingInfo is null))
            {
                continue;
            }

            double team1Rating = replay.ReplayPlayers.Where(x => x.GamePos <= 3).Sum(s => s.ReplayPlayerRatingInfo!.Rating) / 3.0;
            double team2Rating = replay.ReplayPlayers.Where(x => x.GamePos > 3).Sum(s => s.ReplayPlayerRatingInfo!.Rating) / 3.0;

            foreach (var replayPlayer in replay.ReplayPlayers)
            {
                var playerId = new PlayerId(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId);
                var oppRating = replayPlayer.GamePos <= 3 ? team2Rating : team1Rating;
                if (playerIds.TryGetValue(playerId, out var performanceHelper))
                {
                    performanceHelper.OppRatings.Add(oppRating);
                }
                else
                {
                    performanceHelper = playerIds[playerId] = new() { OppRatings = [oppRating] };
                }

                if (replayPlayer.PlayerResult == PlayerResult.Win)
                {
                    performanceHelper.Wins++;
                }
            }
        }

        foreach (var ent in playerIds)
        {
            var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == ent.Key);
            if (playerState is null)
            {
                continue;
            }
            playerState.Performance = Convert.ToInt32(PerformanceRating(ent.Value));
        }
    }

    private static double ExpectedScore(List<double> oppRatings, double ownRating)
    {
        return oppRatings.Sum(s => 1 / (1 + Math.Pow(10, (s - ownRating) / 400)));
    }

    private static double PerformanceRating(PerformanceHelper performanceHelper)
    {
        if (performanceHelper.OppRatings.Count == 0 || performanceHelper.Wins == 0)
        {
            return 0;
        }

        (double low, double high) = (0.0, 4000.0);
        double mid = 0.0;
        while (high - low > 0.001)
        {
            mid = (low + high) / 2;
            if (ExpectedScore(performanceHelper.OppRatings, mid) < performanceHelper.Wins)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }
        return mid;
    }

    internal record PerformanceHelper
    {
        public List<double> OppRatings { get; set; } = new();
        public int Wins { get; set; }
    }
}
