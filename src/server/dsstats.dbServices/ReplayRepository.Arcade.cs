using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ReplayRepository
{
    public async Task<ReplayDetails?> GetArcadeReplayDetails(string replayHash)
    {
        if (!int.TryParse(replayHash, out var arcadeReplayId))
        {
            return null;
        }
        ;
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replay = await context.ArcadeReplays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.ArcadeReplayId == arcadeReplayId);

        if (replay is null)
        {
            return null;
        }

        var replayRating = await context.ArcadeReplayRatings
            .Where(x => x.ArcadeReplayId == arcadeReplayId)
            .FirstOrDefaultAsync();


        return new()
        {
            Replay = replay.ToDto(),
            ReplayRatings = replayRating == null ? [] : [
                new ReplayRatingDto
                {
                    RatingType = RatingType.All,
                    LeaverType = LeaverType.None,
                    ExpectedWinProbability = replayRating.ExpectedWinProbability / 100.0,
                    AvgRating = replayRating.AvgRating,
                    ReplayPlayerRatings = GetReplayPlayerRatings(replay, replayRating),
                }
            ],
        };
    }

    private static List<ReplayPlayerRatingDto> GetReplayPlayerRatings(ArcadeReplay replay, ArcadeReplayRating? replayRating)
    {
        if (replayRating == null)
        {
            return [];
        }
        var ratings = new List<ReplayPlayerRatingDto>();
        if (replayRating.PlayerRatings.Length >= replay.Players.Count &&
            replayRating.PlayerRatingDeltas.Length >= replay.Players.Count)
        {
            foreach (var item in replay.Players.OrderBy(o => o.SlotNumber).Select((s, index) => new { s, index }))
            {
                var playerRating = new ReplayPlayerRatingDto
                {
                    RatingType = RatingType.All,
                    RatingBefore = replayRating.PlayerRatings[item.index],
                    RatingDelta = replayRating.PlayerRatingDeltas[item.index],
                    ToonId = item.s.Player!.ToDto().ToonId,
                };
                ratings.Add(playerRating);
            }
        }
        return ratings;
    }

    public async Task<List<ReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        logger.LogInformation(
            "Requesting arcade replays: Page: {Page}, Size: {PageSize}, Skip: {Skip}, Take: {Take}",
            request.Page, request.PageSize, request.Skip, request.Take);
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        try
        {
            // First, get the base replays without ratings
            var query = GetArcadeReplaysQueriable(request, context);
            var ordered = GetOrderedArcadeReplays(query, request);
            var list = await ordered
                .Skip(((request.Page - 1) * request.PageSize) + request.Skip)
                .Take(request.Take)
                .AsSplitQuery()
                .ToListAsync(token);

            // If no results, return early
            if (list.Count == 0)
            {
                return [];
            }

            // Extract the replay IDs from the paginated results
            var replayIds = list
                .Select(s => int.Parse(s.ReplayHash))
                .ToList();

            // Load ratings only for these specific replays
            var ratings = await context.ArcadeReplayRatings
                .Where(x => replayIds.Contains(x.ArcadeReplayId))
                .ToDictionaryAsync(x => x.ArcadeReplayId, token);

            // Merge ratings into the results
            foreach (var replay in list)
            {
                var replayId = int.Parse(replay.ReplayHash);
                if (ratings.TryGetValue(replayId, out var rating))
                {
                    replay.RatingList = new()
                    {
                        Exp2Win = rating.ExpectedWinProbability / 100.0,
                        AvgRating = rating.AvgRating,
                    };
                }
            }

            return list.Select(s => s.GetDto()).ToList();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Error getting arcade replays: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetArcadeReplaysCount(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        try
        {
            // Count query - no need for ratings at all
            var query = GetArcadeReplaysQueriableForCount(request, context);
            var count = await query.CountAsync(token);
            logger.LogInformation("Got arcade replays count: {count}", count);
            return count;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Error getting arcade replays count: {error}", ex.Message);
        }
        return 0;
    }

    private static IQueryable<ReplayList> GetArcadeReplaysQueriable(ArcadeReplaysRequest request, DsstatsContext context)
    {
        // Query without loading ratings - they'll be loaded separately after pagination
        var query = from r in context.ArcadeReplays
                    select new ReplayList()
                    {
                        ReplayHash = r.ArcadeReplayId.ToString(),
                        GameTime = r.CreatedAt,
                        GameMode = r.GameMode,
                        Duration = r.Duration,
                        WinnerTeam = r.WinnerTeam,
                        Players = r.Players.Select(s => new ReplayPlayerList()
                        {
                            Name = s.Player!.Name,
                            Team = s.Team,
                        }).ToList(),
                        RatingList = null, // Will be populated after pagination
                    };

        if (!string.IsNullOrEmpty(request.Name))
        {
            var names = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in names)
            {
                query = query.Where(x => x.Players.Any(p => p.Name.Contains(name)));
            }
        }

        return query;
    }

    private IQueryable<ArcadeReplay> GetArcadeReplaysQueriableForCount(ArcadeReplaysRequest request, DsstatsContext context)
    {
        // Simplified query for counting - no need for projections or ratings
        var query = context.ArcadeReplays.AsQueryable();

        if (!string.IsNullOrEmpty(request.Name))
        {
            var names = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in names)
            {
                query = query.Where(x => x.Players.Any(p => p.Player!.Name.Contains(name)));
            }
        }

        return query;
    }

    private static IOrderedQueryable<ReplayList> GetOrderedArcadeReplays(IQueryable<ReplayList> query, ArcadeReplaysRequest request)
    {
        if (request.TableOrders is null || request.TableOrders.Count == 0)
        {
            return query.OrderByDescending(x => x.GameTime);
        }

        IOrderedQueryable<ReplayList>? orderedQuery = null;
        foreach (var order in request.TableOrders)
        {
            switch (order.Column)
            {
                case nameof(ReplayList.GameTime):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.GameTime) : query.OrderByDescending(x => x.GameTime))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.GameTime) : orderedQuery.ThenByDescending(x => x.GameTime));
                    break;
                case nameof(ReplayList.Duration):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Duration) : query.OrderByDescending(x => x.Duration))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Duration) : orderedQuery.ThenByDescending(x => x.Duration));
                    break;
                case nameof(ReplayList.GameMode):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.GameMode) : query.OrderByDescending(x => x.GameMode))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.GameMode) : orderedQuery.ThenByDescending(x => x.GameMode));
                    break;
            }
        }
        return orderedQuery ?? query.OrderByDescending(x => x.GameTime);
    }
}