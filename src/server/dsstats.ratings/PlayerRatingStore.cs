using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.ratings;

public class PlayerRatingsStore
{
    private readonly Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> _ratings = [];

    public PlayerRatingCalcDto GetOrCreate(int playerId, RatingType ratingType, double startMmr, bool applyChanges = true)
    {
        if (!_ratings.TryGetValue(playerId, out var ratingMap))
        {
            ratingMap = _ratings[playerId] = [];
        }

        if (!ratingMap.TryGetValue(ratingType, out var playerRating))
        {
            playerRating = ratingMap[ratingType] = new PlayerRatingCalcDto
            {
                Rating = startMmr
            };
        }

        if (!applyChanges)
        {
            return playerRating.ShallowCopy();
        }

        return playerRating;
    }

    public bool TryGet(int playerId, RatingType ratingType, out PlayerRatingCalcDto? rating)
    {
        rating = null;
        if (_ratings.TryGetValue(playerId, out var map))
        {
            return map.TryGetValue(ratingType, out rating);
        }
        return false;
    }

    public Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> GetAll() => _ratings;

    public void AddOrUpdate(int playerId, RatingType ratingType, PlayerRatingCalcDto dto)
    {
        if (!_ratings.TryGetValue(playerId, out var ratingMap))
        {
            ratingMap = _ratings[playerId] = [];
        }

        ratingMap[ratingType] = dto;
    }

    /// <summary>
    /// Loads player ratings for a given list of replays directly from the database.
    /// </summary>
    public static async Task<PlayerRatingsStore> LoadFromDatabaseAsync(List<ReplayCalcDto> replays, DsstatsContext context)
    {
        var store = new PlayerRatingsStore();

        var playerIds = replays
            .SelectMany(s => s.Players)
            .Select(s => s.PlayerId)
            .ToHashSet();

        var players = await context.Players
            .Include(i => i.Ratings)
            .Where(x => playerIds.Contains(x.PlayerId))
            .ToListAsync();

        foreach (var player in players)
        {
            foreach (var rating in player.Ratings)
            {
                var dto = new PlayerRatingCalcDto
                {
                    Games = rating.Games,
                    Wins = rating.Wins,
                    Mvps = rating.Mvps,
                    Change = rating.Change,
                    Rating = rating.Rating,
                    Consistency = rating.Consistency,
                    Confidence = rating.Confidence,
                    LastGame = rating.LastGame,
                    CmdrCounts = GetFakeCmdrDict(rating),
                };

                store.AddOrUpdate(player.PlayerId, rating.RatingType, dto);
            }
        }

        return store;
    }

    private static Dictionary<Commander, int> GetFakeCmdrDict(PlayerRating rating)
    {
        if (rating.MainCount == 0)
        {
            return [];
        }
        var dict = new Dictionary<Commander, int>
        {
            { rating.Main, rating.MainCount }
        };
        return dict;
    }
}

