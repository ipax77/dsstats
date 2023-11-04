using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.services;

public partial class PlayerService
{
    private async Task<int> GetDsstatsRatingsListCount(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetDsstatsListQuery(request);
        ratings = FilterComboList(ratings, request);
        return await ratings.CountAsync(token);
    }

    private async Task<List<ComboPlayerRatingDto>> GetDsstatsRatingsList(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetDsstatsListQuery(request);
        ratings = FilterComboList(ratings, request);
        ratings = SortComboList(ratings, request);

        var lratings = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);

        return lratings;
    }

    private IQueryable<ComboPlayerRatingDto> GetDsstatsListQuery(RatingsRequest request)
    {
        var query = from pr in context.PlayerRatings
                    where pr.Games > 19 && pr.RatingType == request.Type
                    select new ComboPlayerRatingDto()
                    {
                        ComboPlayerRating = new()
                        {
                            Rating = pr.Rating,
                            Pos = pr.Pos,
                            Games = pr.Games,
                            Wins = pr.Wins,
                        },
                        Player = new PlayerRatingPlayerDto()
                        {
                            Name = pr.Player!.Name,
                            ToonId = pr.Player.ToonId,
                            RegionId = pr.Player.RegionId,
                            RealmId = pr.Player.RealmId
                        },
                        PlayerRating = new PlayerRatingDto()
                        {
                            Rating = pr.Rating,
                            Pos = pr.Pos,
                            Games = pr.Games,
                            Wins = pr.Wins,
                            Mvp = pr.Mvp,
                            MainCount = pr.MainCount,
                            Main = (Commander)pr.Main,
                            IsUploader = pr.IsUploader,
                            PlayerRatingChange = pr.PlayerRatingChange == null ? null : new()
                            {
                                Change24h = pr.PlayerRatingChange.Change24h,
                                Change10d = pr.PlayerRatingChange.Change10d,
                                Change30d = pr.PlayerRatingChange.Change30d
                            }
                        }
                    };
        return query;
    }
}
