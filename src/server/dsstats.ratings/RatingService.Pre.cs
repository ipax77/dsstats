
using dsstats.db;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

public partial class RatingService
{
    public async Task PreRatings(List<int> replayIds)
    {
        await ratingLock.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            var replays = await GetReplayCalcDtos(replayIds, context);
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
                        replayRating.IsPreRating = true;
                        replayRatingsToInsert.Add(replayRating);
                    }
                }
            }
            await context.AddRangeAsync(replayRatingsToInsert);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating pre ratings: {error}", ex.Message);
        }
        finally
        {
            ratingLock.Release();
        }
    }

    public async Task PreRatings(List<ReplayCalcDto> replays)
    {
        await ratingLock.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

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
                        replayRating.IsPreRating = true;
                        replayRatingsToInsert.Add(replayRating);
                    }
                }
            }
            await context.AddRangeAsync(replayRatingsToInsert);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating pre ratings: {error}", ex.Message);
        }
        finally
        {
            ratingLock.Release();
        }
    }
}