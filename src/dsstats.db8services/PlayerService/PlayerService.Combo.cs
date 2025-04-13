using dsstats.shared;
using dsstats.shared.Extensions;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    private async Task<int> GetComboRatingsListCount(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetComboListQuery(request);
        ratings = FilterComboList(ratings, request);
        return await ratings.CountAsync(token);
    }

    private async Task<List<ComboPlayerRatingDto>> GetComboRatingsList(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetComboListQuery(request);
        ratings = FilterComboList(ratings, request);
        ratings = SortComboList(ratings, request);

        var lratings = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);

        return lratings;
    }

    private IQueryable<ComboPlayerRatingDto> FilterComboList(IQueryable<ComboPlayerRatingDto> ratings,
                                                             RatingsRequest request)
    {
        if (request.Region > 0)
        {
            ratings = ratings.Where(x => x.Player.RegionId == request.Region);
        }

        if (request.Active)
        {
            ratings = ratings.Where(x => x.IsActive);
        }

        if (string.IsNullOrEmpty(request.Search))
        {
            return ratings;
        }

        var searchStrings = request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var predicate = PredicateBuilder.New<ComboPlayerRatingDto>();

        foreach (var search in searchStrings)
        {
            predicate = predicate.Or(x => x.Player.Name.Contains(search));
        }

        ratings = ratings.Where(predicate);

        return ratings;
    }

    private IQueryable<ComboPlayerRatingDto> SortComboList(IQueryable<ComboPlayerRatingDto> ratings, RatingsRequest request)
    {
        return SortComboListByDs(ratings, request);
    }

    private IQueryable<ComboPlayerRatingDto> SortComboListByDs(IQueryable<ComboPlayerRatingDto> ratings, RatingsRequest request)
    {
        if (request.Orders.Count == 0)
        {
            return ratings.OrderByDescending(o => o.PlayerRating.Rating);
        }

        foreach (var order in request.Orders)
        {
            if (order.Property == "Wins")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.PlayerRating.Games == 0 ? 0 : o.PlayerRating.Wins * 100.0 / o.PlayerRating.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.PlayerRating.Games == 0 ? 0 : o.PlayerRating.Wins * 100.0 / o.PlayerRating.Games);
                }
            }
            else if (order.Property == "Region")
            {
                if (order.Ascending)
                {
                    ratings = ratings.AppendOrderBy("Player.RegionId");
                }
                else
                {
                    ratings = ratings.AppendOrderByDescending("Player.RegionId");
                }
            }
            else
            {
                if (order.Ascending)
                {
                    ratings = ratings.AppendOrderBy($"PlayerRating.{order.Property}");
                }
                else
                {
                    ratings = ratings.AppendOrderByDescending($"PlayerRating.{order.Property}");
                }
            }
        }
        return ratings;
    }

    private IQueryable<ComboPlayerRatingDto> GetComboListQuery(RatingsRequest request)
    {
        var query = from cpr in context.PlayerRatings
                    join p in context.Players on cpr.PlayerId equals p.PlayerId
                    // join pr in context.PlayerRatings on cpr.PlayerId equals pr.PlayerId
                    // join prc in context.PlayerRatingChanges on pr.PlayerRatingId equals prc.PlayerRatingId into grouping
                    // from prc in grouping.DefaultIfEmpty()
                    where cpr.Games > 19 && cpr.RatingType == request.Type 
                        // && pr.RatingType == request.Type
                    // && (!request.Uploaders || cpr.Player.UploaderId != null)
                    select new ComboPlayerRatingDto()
                    {
                        Player = new PlayerRatingPlayerDto()
                        {
                            Name = p.Name,
                            ToonId = p.ToonId,
                            RegionId = p.RegionId,
                            RealmId = p.RealmId,
                            // IsUploader = cpr.Player.UploaderId != null,
                        },
                        PlayerRating = new PlayerRatingDto()
                        {
                            Rating = cpr.Rating,
                            Pos = cpr.Pos,
                            Games = cpr.Games,
                            Wins = cpr.Wins,
                            Mvp = cpr.Mvp,
                            MainCount = cpr.MainCount,
                            Main = cpr.Main,
                            //IsUploader = pr.IsUploader,
                            //PlayerRatingChange = prc == null ? null : new()
                            //{
                            //    Change24h = prc.Change24h,
                            //    Change10d = prc.Change10d,
                            //    Change30d = prc.Change30d
                            //}
                        },
                        IsActive = true
                    };
        return query;
    }
}

