using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public class IhRepository(ReplayContext context, ILogger<IhRepository> logger) : IIhRepository
{
    public async Task<GroupState> GetOrCreateGroupState(Guid groupId, RatingType ratingType = RatingType.StdTE)
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
                GroupState = new()
                {
                    RatingType = ratingType,
                    GroupId = groupId,
                    Created = created
                }
            };
            context.IhSessions.Add(ihSession);
            await context.SaveChangesAsync();
        }

        if (ihSession.GroupState is null)
        {
            ihSession.GroupState = new()
            {
                RatingType = ratingType,
                GroupId = groupId,
                Created = created
            };
        }
        return ihSession.GroupState;
    }

    public async Task UpdateGroupState(GroupState groupState)
    {
        var ihSession = await context.IhSessions.FirstOrDefaultAsync(f => f.GroupId == groupState.GroupId);

        if (ihSession is null)
        {
            return;
        }

        ihSession.Players = Math.Max(ihSession.Players, groupState.PlayerStats.Count);
        ihSession.Games = groupState.ReplayHashes.Count;
        ihSession.GroupState = groupState;
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
            await context.IhSessions.ExecuteUpdateAsync(u => u.SetProperty(p => p.Closed, true));
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
                && x.GroupState != null)
            .Select(s => s.GroupState!.ReplayHashes) 
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

    public async Task CalcultePerformance(GroupState groupState)
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

            var team1Rating = replay.ReplayPlayers.Where(x => x.GamePos <= 3).Sum(s => s.ReplayPlayerRatingInfo!.Rating);
            var team2Rating = replay.ReplayPlayers.Where(x => x.GamePos > 3).Sum(s => s.ReplayPlayerRatingInfo!.Rating);

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
            playerState.Performance = PerformanceRating(ent.Value);
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
        return Math.Round(mid, 2);
    }

    internal record PerformanceHelper
    {
        public List<double> OppRatings { get; set; } = new();
        public int Wins { get; set; }
    } 
}
