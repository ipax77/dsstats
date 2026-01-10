
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace dsstats.ratings;

public partial class RatingService
{
    public async Task ContinueRatings()
    {
        await ratingLock.WaitAsync();
        Stopwatch sw = Stopwatch.StartNew();
        int count = 0;
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            await ClearPreRatings(context);
            var fromTime = DateTime.UtcNow.AddHours(-3);

            var replays = await GetContinueReplayCalcDtos(fromTime, 1000);
            count = replays.Count;
            if (count > 0)
            {
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
                if (replayRatingsToInsert.Count > 0)
                {
                    await context.AddRangeAsync(replayRatingsToInsert);
                    await context.SaveChangesAsync();
                    await ContinuePlayerRatings(playerRatingsStore.GetAll(), context);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating continue ratings: {error}", ex.Message);
        }
        finally
        {
            ratingLock.Release();
        }
        sw.Stop();
        logger.LogWarning("continue ratings completed in {time} ms ({count})", sw.ElapsedMilliseconds, count);
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
        await context.ReplayPlayerRatings
            .Where(rpr => rpr.ReplayRating!.IsPreRating)
            .ExecuteDeleteAsync();

        await context.ReplayRatings
            .Where(rr => rr.IsPreRating)
            .ExecuteDeleteAsync();
    }
}

