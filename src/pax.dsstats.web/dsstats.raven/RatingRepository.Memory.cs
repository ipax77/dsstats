using dsstats.raven.Extensions;
using pax.dsstats.shared;

namespace dsstats.raven;

public partial class RatingRepository
{
    private static Dictionary<int, RatingMemory> RatingMemory = new();

    private List<RavenPlayerDto> GetPlayerRatingFromMemory(int toonId)
    {
        List<RavenPlayerDto> players = new();

        if (RatingMemory.ContainsKey(toonId))
        {
            var rm = RatingMemory[toonId];
            if (rm.CmdrPlayer != null)
            {
                players.Add(rm.CmdrPlayer);
            }
            if (rm.StdPlayer != null)
            {
                players.Add(rm.StdPlayer);
            }
        }
        return players;
    }

    private string? GetToonIdNameFromMemory(int toonId)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            var rating = RatingMemory[toonId];
            if (rating.CmdrPlayer != null)
            {
                return rating.CmdrPlayer.Name;
            }
            else if (rating.StdPlayer != null)
            {
                return rating.StdPlayer.Name;
            }
            else return null;
        }
        return null;
    }

    private List<RequestNames> GetTopPlayersFromMemory(RatingType ratingType, int minGames)
    {
        IQueryable<RavenPlayerDto> ratings;

        if (ratingType == RatingType.Cmdr)
        {
            ratings = RatingMemory.Values
                .Where(x => x.CmdrPlayer != null && x.CmdrPlayer.Rating.Games >= minGames)
                .Select(s => s.CmdrPlayer ?? new()).AsQueryable();
        }
        else if (ratingType == RatingType.Std)
        {
            ratings = RatingMemory.Values
                .Where(x => x.StdPlayer != null && x.StdPlayer.Rating.Games >= minGames)
                .Select(s => s.StdPlayer ?? new()).AsQueryable();
        }
        else
        {
            throw new NotImplementedException();
        }

        return ratings.OrderByDescending(o => o.Rating.Wins * 100.0 / o.Rating.Games)
            .Take(5)
            .Select(s => new RequestNames() { Name = s.Name, ToonId = s.ToonId })
            .ToList();
    }

    private static RatingsResult GetRatingsFromMemory(RatingsRequest request)
    {
        IQueryable<RavenPlayerDto> ratings;

        if (request.Type == RatingType.Cmdr)
        {
            ratings = RatingMemory.Values
                .Where(x => x.CmdrPlayer != null && x.CmdrPlayer.Rating.Games >= 20)
                .Select(s => s.CmdrPlayer ?? new()).AsQueryable();
        }
        else if (request.Type == RatingType.Std)
        {
            ratings = RatingMemory.Values
                .Where(x => x.StdPlayer != null && x.StdPlayer.Rating.Games >= 20)
                .Select(s => s.StdPlayer ?? new()).AsQueryable();
        }
        else
        {
            throw new NotImplementedException();
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => x.Name.ToUpper().Contains(request.Search.ToUpper()));
        }

        foreach (var order in request.Orders)
        {
            if (order.Property == "Rating.Wins")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Rating.Games == 0 ? 0 : o.Rating.Wins * 100.0 / o.Rating.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Rating.Games == 0 ? 0 : o.Rating.Wins * 100.0 / o.Rating.Games);
                }
            }
            else if (order.Property == "Rating.Mvp")
            {
                if (order.Ascending)
                {
                    ratings = ratings.OrderBy(o => o.Rating.Games == 0 ? 0 : o.Rating.Mvp * 100.0 / o.Rating.Games);
                }
                else
                {
                    ratings = ratings.OrderByDescending(o => o.Rating.Games == 0 ? 0 : o.Rating.Mvp * 100.0 / o.Rating.Games);
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

        //return new RatingsResult
        //{
        //    Count = ratings.Count(),
        //    Players = ratings.Skip(request.Skip).Take(request.Take).ToList()
        //};
        return new();
    }

    private void StoreRating(RavenPlayer player, RavenRating rating)
    {
        RavenPlayerDto dto = new()
        {
            Name = player.Name,
            ToonId = player.ToonId,
            RegionId = player.RegionId,
            Rating = new()
            {
                Games = rating.Games,
                Wins = rating.Wins,
                Mvp = rating.Mvp,
                TeamGames = rating.TeamGames,
                Main = rating.Main,
                MainPercentage = rating.MainPercentage,
                Mmr = rating.Mmr,
            }
        };

        if (RatingMemory.ContainsKey(player.ToonId))
        {
            if (rating.Type == RatingType.Cmdr)
            {
                RatingMemory[player.ToonId].CmdrPlayer = dto;
            }
            else if (rating.Type == RatingType.Std)
            {
                RatingMemory[player.ToonId].StdPlayer = dto;
            }
        }
        else
        {
            if (rating.Type == RatingType.Cmdr)
            {
                RatingMemory rm = new()
                {
                    CmdrPlayer = dto
                };
                RatingMemory[player.ToonId] = rm;
            }
            else if (rating.Type == RatingType.Std)
            {
                RatingMemory rm = new()
                {
                    StdPlayer = dto
                };
                RatingMemory[player.ToonId] = rm;
            }
        }
    }
}


internal record RatingMemory
{
    public RavenPlayerDto? CmdrPlayer { get; set; }
    public RavenPlayerDto? StdPlayer { get; set; }
}