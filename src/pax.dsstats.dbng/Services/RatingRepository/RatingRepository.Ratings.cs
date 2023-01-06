
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        return await ratings.CountAsync(token);
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetRequestRatingsQueirable(context, request);
        ratings = SortPlayerRatings(request, ratings);

        return new()
        {
            Players = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token)
        };
    }

    private IQueryable<PlayerRating> SortPlayerRatings(RatingsRequest request, IQueryable<PlayerRating> ratings)
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
            else if (order.Property == "Mvp")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.Mvp * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.Mvp * 100.0 / o.Games);
                }
            }
            else if (order.Property == "MainCount")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Games == 0 ? 0 : o.MainCount * 100.0 / o.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Games == 0 ? 0 : o.MainCount * 100.0 / o.Games);
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

    private IQueryable<PlayerRating> GetRequestRatingsQueirable(ReplayContext context, RatingsRequest request)
    {
        var ratings = context.PlayerRatings
            .Include(i => i.Player)
            .Where(x => x.Games > 20 && x.RatingType == request.Type);

        if (request.Uploaders && !Data.IsMaui)
        {
            ratings = ratings.Where(x => x.IsUploader);
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => x.Player.Name.ToUpper().Contains(request.Search.ToUpper()));
        }

        return ratings;
    }

    public async Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = context.PlayerRatings
            .Where(x => request.ToonIds.Contains(x.Player.ToonId));

        if (request.RatingType != shared.RatingType.None)
        {
            ratings = ratings.Where(x => x.RatingType == request.RatingType);
        }

        return new ToonIdRatingResponse()
        {
            Ratings = await ratings
                .OrderByDescending(o => o.Rating)
                .Take(10)
                .ProjectTo<PlayerRatingDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync(token)
        };
    }
}
