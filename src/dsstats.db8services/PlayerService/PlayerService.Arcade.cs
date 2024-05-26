
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class PlayerService
{
    private async Task<int> GetArcadeRatingsListCount(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetArcadeListQuery(request);
        ratings = FilterComboList(ratings, request);
        return await ratings.CountAsync(token);
    }

    private async Task<List<ComboPlayerRatingDto>> GetArcadeRatingsList(RatingsRequest request, CancellationToken token)
    {
        var ratings = GetArcadeListQuery(request);
        ratings = FilterComboList(ratings, request);
        ratings = SortComboList(ratings, request);

        var lratings = await ratings
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);

        return lratings;
    }

    private IQueryable<ComboPlayerRatingDto> GetArcadeListQuery(RatingsRequest request)
    {
        var query = from cpr in context.ArcadePlayerRatings
                    from pr in context.PlayerRatings.Where(x => x.Player!.ToonId == cpr.ArcadePlayer!.ProfileId
                        && x.Player.RealmId == cpr.ArcadePlayer.RealmId
                        && x.Player.RegionId == cpr.ArcadePlayer.RegionId).DefaultIfEmpty()
                    where cpr.Games > 19 && cpr.RatingType == request.Type && pr.RatingType == request.Type
                    select new ComboPlayerRatingDto()
                    {
                        ComboPlayerRating = new()
                        {
                            Rating = cpr.Rating,
                            Pos = cpr.Pos,
                            Games = cpr.Games,
                            Wins = cpr.Wins,
                        },
                        Player = new PlayerRatingPlayerDto()
                        {
                            Name = cpr.ArcadePlayer!.Name,
                            ToonId = cpr.ArcadePlayer.ProfileId,
                            RegionId = cpr.ArcadePlayer.RegionId,
                            RealmId = cpr.ArcadePlayer.RealmId
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
                            PlayerRatingChange = cpr.ArcadePlayerRatingChange == null ? null : new()
                            {
                                Change24h = cpr.ArcadePlayerRatingChange!.Change24h,
                                Change10d = cpr.ArcadePlayerRatingChange.Change10d,
                                Change30d = cpr.ArcadePlayerRatingChange.Change30d
                            }
                        },
                        IsActive = cpr.ArcadePlayerRatingChange != null,
                    };
        return query;
    }
}
