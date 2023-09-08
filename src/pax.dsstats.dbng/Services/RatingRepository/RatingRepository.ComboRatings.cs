using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task<int> GetComboRatingsCount(RatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetComboRequestRatingsQueirable(context, request);
        return await ratings.CountAsync(token);
    }

    public async Task<RatingsResult> GetComboRatings(RatingsRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratings = GetComboRequestRatingsQueirable(context, request);
        ratings = SortComboPlayerRatings(request, ratings);

        var lratings = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(s => new PlayerRatingDto() 
            {
                Rating = s.Rating,
                Games = s.Games,
                Wins = s.Wins,
                Player = new PlayerRatingPlayerDto() 
                {
                    Name = s.Player.Name,
                    ToonId = s.Player.ToonId,
                    RegionId = s.Player.RegionId,
                    RealmId = s.Player.RealmId
                }
            })
            .ToListAsync(token);

        return new()
        {
            Players = lratings
        };
    }

    private IQueryable<ComboPlayerRating> SortComboPlayerRatings(RatingsRequest request, IQueryable<ComboPlayerRating> ratings)
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
            else if (order.Property == "Main" || order.Property == "Mvp" || order.Property == "MainCount" || order.Property.StartsWith("PlayerRatingChange"))
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Rating);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Rating);
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

    private IQueryable<ComboPlayerRating> GetComboRequestRatingsQueirable(ReplayContext context, RatingsRequest request)
    {
        var ratings = context.ComboPlayerRatings
            .Include(i => i.Player)
            .Where(x => x.Games > 20 && x.RatingType == request.Type);

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => x.Player.Name.ToUpper().Contains(request.Search.Trim().ToUpper()));
        }
        return ratings;
    }
}
