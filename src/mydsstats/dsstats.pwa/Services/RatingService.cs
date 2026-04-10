using dsstats.indexedDb.Services;
using dsstats.shared;
using System.Net.Http.Json;

namespace dsstats.pwa.Services;

public class RatingService(IHttpClientFactory httpClientFactory, IndexedDbService dbService)
{
    public async Task<ReplayRatingDto?> FetchAndSaveRating(string replayHash)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ApiClient");
            var rating = await client.GetFromJsonAsync<ReplayRatingDto>($"api10/Replays/rating/{replayHash}");
            if (rating is null) return null;
            await dbService.SaveReplayRatingAsync(replayHash, rating);
            return rating;
        }
        catch
        {
            return null;
        }
    }
}
