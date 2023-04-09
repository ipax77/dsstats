using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services;

public partial class ArcadeService : IArcadeService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMapper mapper;
    private readonly ILogger<ArcadeService> logger;

    public ArcadeService(IServiceScopeFactory scopeFactory, IMapper mapper, ILogger<ArcadeService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        return await ratings.CountAsync(token);
    }

    public async Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        ratings = SortPlayerRatings(request, ratings);

        return await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<ArcadePlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    private IQueryable<ArcadePlayerRating> SortPlayerRatings(ArcadeRatingsRequest request, IQueryable<ArcadePlayerRating> ratings)
    {
        foreach (var order in request.Orders)
        {
            if (order.Property == "Wins")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.Wins * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.Wins * 100.0 / o.Games);
                }
            }
            else
            {
                if (order.Ascending)
                {
                    ratings = ratings.AppendOrderBy(order.Property);
                }
                else
                {
                    ratings = ratings.AppendOrderByDescending(order.Property);
                }
            }
        }
        return ratings;
    }

    private IQueryable<ArcadePlayerRating> GetRequestRatingsQueirable(ReplayContext context, ArcadeRatingsRequest request)
    {
        var ratings = context.ArcadePlayerRatings
            .Include(i => i.ArcadePlayer)
            .Where(x => x.Games > 20 && x.RatingType == request.Type);

        if (request.RegionId > 0)
        {
            ratings = ratings.Where(x => x.ArcadePlayer.RegionId == request.RegionId);
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => x.ArcadePlayer.Name.ToUpper().Contains(request.Search.ToUpper()));
        }

        return ratings;
    }

    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        memKey = "Arcade" + memKey;

        using var scope = scopeFactory.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        if (!memoryCache.TryGetValue(memKey, out DistributionResponse response))
        {
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            List<MmrDevDto> mmrDevs;
            mmrDevs = await context.ArcadePlayerRatings
                .Where(x => x.RatingType == request.RatingType)
                .GroupBy(g => Math.Floor((g.Rating / 10) + 1) * 10)
                .Select(s => new MmrDevDto
                {
                    Count = s.Count(),
                    Mmr = s.Key
                })
                .OrderBy(o => o.Mmr)
                .ToListAsync();

            response = new()
            {
                MmrDevs = mmrDevs
            };

            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }
}

