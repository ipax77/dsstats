using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class PlayerService(IServiceScopeFactory scopeFactory, IMemoryCache memoryCache, ILogger<PlayerService> logger) : IPlayerService
{
    public async Task<int> GetRatingsCount(PlayerRatingsRequest request, CancellationToken token = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var query = GetRatingsQueryable(request, context);
            return await query.CountAsync(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetRatingsCount");
        }
        return 0;
    }

    public async Task<List<PlayerRatingListItem>> GetRatings(PlayerRatingsRequest request, CancellationToken token = default)
    {
        if (request.PageSize < 10)
        {
            return await GetTopRatings(request, token);
        }
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var query = GetRatingsQueryable(request, context);
            var ordered = GetOrderedRatings(query, request);

            var rawData = await ordered
                .AsNoTracking()
                .Skip(((request.Page - 1) * request.PageSize) + request.Skip)
                .Take(request.Take)
                .ToListAsync(token);
            return rawData.Select(s => s.ToPlayerRatingListItem()).ToList();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetRatings");
        }
        return [];
    }

    private async Task<List<PlayerRatingListItem>> GetTopRatings(PlayerRatingsRequest request, CancellationToken token)
    {
        var memKey = $"topRatings_{request.RatingType}";
        if (memoryCache.TryGetValue(memKey, out var value)
            && value is List<PlayerRatingListItem> items)
        {
            return items;
        }
        else
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var query = GetRatingsQueryable(request, context);
                var ordered = GetOrderedRatings(query, request);

                var rawData = await ordered
                    .AsNoTracking()
                    .Take(request.Take)
                    .ToListAsync(token);
                items = rawData.Select(s => s.ToPlayerRatingListItem()).ToList();
                memoryCache.Set(memKey, items, TimeSpan.FromHours(1));
                return items;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetRatings");
            }
        }
        return [];
    }

    private static IOrderedQueryable<PlayerRatingListItemRaw> GetOrderedRatings(
        IQueryable<PlayerRatingListItemRaw> query,
        PlayerRatingsRequest request)
    {
        if (request.Orders == null || request.Orders.Count == 0)
        {
            // Default sort: Rating descending, then Name ascending
            return query.OrderByDescending(x => x.Rating);
        }

        IOrderedQueryable<PlayerRatingListItemRaw>? orderedQuery = null;

        foreach (var order in request.Orders)
        {
            switch (order.Column)
            {
                case nameof(PlayerRatingListItemRaw.Pos):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Pos) : query.OrderByDescending(x => x.Pos))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Pos) : orderedQuery.ThenByDescending(x => x.Pos));
                    break;

                case nameof(PlayerRatingListItem.RegionId):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.ToonId.Region) : query.OrderByDescending(x => x.ToonId.Region))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.ToonId.Region) : orderedQuery.ThenByDescending(x => x.ToonId.Region));
                    break;

                case nameof(PlayerRatingListItemRaw.Name):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Name) : orderedQuery.ThenByDescending(x => x.Name));
                    break;

                case nameof(PlayerRatingListItemRaw.Main):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Main) : query.OrderByDescending(x => x.Main))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Main) : orderedQuery.ThenByDescending(x => x.Main));
                    break;

                case nameof(PlayerRatingListItemRaw.MainCount):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.MainCount / x.Games) : query.OrderByDescending(x => x.MainCount / x.Games))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.MainCount / x.Games) : orderedQuery.ThenByDescending(x => x.MainCount / x.Games));
                    break;

                case nameof(PlayerRatingListItemRaw.Games):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Games) : query.OrderByDescending(x => x.Games))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Games) : orderedQuery.ThenByDescending(x => x.Games));
                    break;

                case nameof(PlayerRatingListItemRaw.Wins):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Wins / x.Games) : query.OrderByDescending(x => x.Wins / x.Games))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Wins / x.Games) : orderedQuery.ThenByDescending(x => x.Wins / x.Games));
                    break;

                case nameof(PlayerRatingListItemRaw.Mvps):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Mvps / x.Games) : query.OrderByDescending(x => x.Mvps / x.Games))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Mvps / x.Games) : orderedQuery.ThenByDescending(x => x.Mvps / x.Games));
                    break;

                case nameof(PlayerRatingListItemRaw.Rating):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Rating) : query.OrderByDescending(x => x.Rating))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Rating) : orderedQuery.ThenByDescending(x => x.Rating));
                    break;

                case nameof(PlayerRatingListItemRaw.Change):
                    orderedQuery = orderedQuery == null
                        ? (order.Ascending ? query.OrderBy(x => x.Change) : query.OrderByDescending(x => x.Change))
                        : (order.Ascending ? orderedQuery.ThenBy(x => x.Change) : orderedQuery.ThenByDescending(x => x.Change));
                    break;
            }
        }

        // fallback: if nothing matched, order by Rating desc
        return orderedQuery ?? query.OrderByDescending(x => x.Rating);
    }


    private IQueryable<PlayerRatingListItemRaw> GetRatingsQueryable(PlayerRatingsRequest request, DsstatsContext context)
    {
        var query = context.PlayerRatings
            .Where(x => x.RatingType == request.RatingType
            )
            .Select(s => new PlayerRatingListItemRaw
            {
                PlayerId = s.PlayerId,
                ToonId = s.Player!.ToonId,
                Name = s.Player!.Name,
                Pos = s.Position,
                Games = s.Games,
                Wins = s.Wins,
                Mvps = s.Mvps,
                Main = s.Main,
                MainCount = s.MainCount,
                Change = s.Change,
                Rating = s.Rating,
            });

        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(x => x.Name.Contains(request.Name));
        }

        if (request.RegionId != 0)
        {
            query = query.Where(x => x.ToonId.Region == request.RegionId);
        }

        if (request.IsActive)
        {
            query = query.Where(x => x.Change != 0);
        }

        return query;
    }

    private record PlayerRatingListItemRaw
    {
        public RatingType RatingType { get; set; }
        public int PlayerId { get; set; }
        public ToonId ToonId { get; set; } = new();
        public int Pos { get; set; }
        public string Name { get; set; } = string.Empty;
        public Commander Main { get; set; }
        public int MainCount { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Mvps { get; set; }
        public double Rating { get; set; }
        public int Change { get; set; }
        public double Cons { get; set; }
        public double Conf { get; set; }

        public PlayerRatingListItem ToPlayerRatingListItem()
        {
            return new PlayerRatingListItem
            {
                RatingType = this.RatingType,
                PlayerId = this.PlayerId,
                RegionId = this.ToonId.Region,
                ToonIdString = Data.GetToonIdString(new() { Id = ToonId.Id, Realm = ToonId.Realm, Region = ToonId.Region }),
                Pos = this.Pos,
                Name = this.Name,
                Main = this.Main,
                MainCount = this.MainCount,
                Games = this.Games,
                Wins = this.Wins,
                Mvps = this.Mvps,
                Rating = this.Rating,
                Change = this.Change,
                Cons = this.Cons,
                Conf = this.Conf,
            };
        }
    }
}


