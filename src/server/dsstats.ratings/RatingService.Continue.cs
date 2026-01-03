
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

public partial class RatingService
{
    public async Task ContinueRatings()
    {
        await ratingLock.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            await ClearPreRatings(context);

            var latest = await context.Replays
                .Where(x => x.Ratings.Count > 0)
                .OrderByDescending(o => o.Gametime)
                .Select(s => new { s.Gametime, s.ReplayId })
                .FirstOrDefaultAsync();
            if (latest is null)
            {
                return;
            }

            var replays = await GetReplayCalcDtos(latest.Gametime, 1000);
            var playerRatingsStore = await PlayerRatingsStore.LoadFromDatabaseAsync(replays, context);
            List<ReplayRating> replayRatingsToInsert = [];

            foreach (var replay in replays)
            {
                var ratingTypes = GetRatingTypes(replay);
                foreach (var ratingType in ratingTypes)
                {
                    var replayRating = ProcessReplay(replay, ratingType, playerRatingsStore);
                    if (replayRating != null)
                    {
                        replayRatingsToInsert.Add(replayRating);
                    }
                }
            }
            await context.AddRangeAsync(replayRatingsToInsert);
            await context.SaveChangesAsync();
            await ContinuePlayerRatings(playerRatingsStore.GetAll(), context);
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating continue ratings: {error}", ex.Message);
        }
        finally
        {
            ratingLock.Release();
        }
    }

    private static async Task ContinuePlayerRatings(
        Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> ratings,
        DsstatsContext context)
    {
        if (ratings.Count == 0) return;

        var playerIds = ratings.Keys.ToList();

        var existingRatings = await context.PlayerRatings
            .Where(r => playerIds.Contains(r.PlayerId))
            .ToListAsync();

        var existingDict = existingRatings
            .GroupBy(r => r.PlayerId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(r => r.RatingType, r => r)
            );

        var newRatings = new List<PlayerRating>();

        foreach (var (playerId, ratingMap) in ratings)
        {
            foreach (var (ratingType, calcDto) in ratingMap)
            {
                Commander mainCmdr = Commander.None;
                int mainCount = 0;

                if (calcDto.CmdrCounts.Count > 0)
                {
                    var main = calcDto.CmdrCounts
                        .Where(w => w.Key != Commander.None)
                        .OrderByDescending(o => o.Value).FirstOrDefault();
                    if (main.Key != Commander.None)
                    {
                        mainCmdr = main.Key;
                        mainCount = main.Value;
                    }
                }

                if (existingDict.TryGetValue(playerId, out var byType) &&
                    byType.TryGetValue(ratingType, out var existing))
                {
                    existing.Games = calcDto.Games;
                    existing.Wins = calcDto.Wins;
                    existing.Mvps = calcDto.Mvps;
                    existing.Main = mainCmdr;
                    existing.MainCount = mainCount;
                    existing.Change = (int)calcDto.Change;
                    existing.Rating = calcDto.Rating;
                    existing.Consistency = calcDto.Consistency;
                    existing.Confidence = calcDto.Confidence;
                    existing.LastGame = calcDto.LastGame;
                }
                else
                {
                    newRatings.Add(new PlayerRating
                    {
                        PlayerId = playerId,
                        RatingType = ratingType,
                        Games = calcDto.Games,
                        Wins = calcDto.Wins,
                        Mvps = calcDto.Mvps,
                        Main = mainCmdr,
                        MainCount = mainCount,
                        Change = (int)calcDto.Change,
                        Rating = calcDto.Rating,
                        Consistency = calcDto.Consistency,
                        Confidence = calcDto.Confidence,
                        LastGame = calcDto.LastGame,
                    });
                }
            }
        }

        if (newRatings.Count > 0)
        {
            await context.PlayerRatings.AddRangeAsync(newRatings);
        }

        await context.SaveChangesAsync();
    }

    private static async Task ClearPreRatings(DsstatsContext context)
    {
        var replayRatings = await context.ReplayRatings
            .Include(i => i.ReplayPlayerRatings)
            .Where(x => x.IsPreRating)
            .ToListAsync();
        if (replayRatings.Count > 0)
        {
            context.ReplayRatings.RemoveRange(replayRatings);
            await context.SaveChangesAsync();
        }
    }
}

