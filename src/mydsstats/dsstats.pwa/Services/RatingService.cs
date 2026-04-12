using dsstats.indexedDb.Services;
using dsstats.shared;
using System.Net.Http.Json;

namespace dsstats.pwa.Services;

public class RatingService(IHttpClientFactory httpClientFactory, IndexedDbService dbService)
{
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private DateTimeOffset _nextAllowedRequestTime = DateTimeOffset.MinValue;

    public async Task<ReplayRatingDto?> FetchAndSaveRating(string replayHash, bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var cached = await dbService.GetReplayRatingAsync(replayHash);
            if (cached is not null)
            {
                return cached;
            }
        }

        await _fetchLock.WaitAsync();
        try
        {
            if (!forceRefresh)
            {
                var cached = await dbService.GetReplayRatingAsync(replayHash);
                if (cached is not null)
                {
                    return cached;
                }
            }

            var now = DateTimeOffset.UtcNow;
            var delay = _nextAllowedRequestTime - now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            var client = httpClientFactory.CreateClient("ApiClient");
            var rating = await client.GetFromJsonAsync<ReplayRatingDto>($"api10/Replays/rating/{replayHash}");
            _nextAllowedRequestTime = DateTimeOffset.UtcNow.AddSeconds(3);
            if (rating is null) return null;
            await dbService.SaveReplayRatingAsync(replayHash, rating);
            return rating;
        }
        catch
        {
            _nextAllowedRequestTime = DateTimeOffset.UtcNow.AddSeconds(3);
            return null;
        }
        finally
        {
            _fetchLock.Release();
        }
    }
}
