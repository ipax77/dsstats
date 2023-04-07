using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class ArcadeService : IArcadeService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<ArcadeService> logger;
    private readonly string arcadeController = "api/v1/arcade/";

    public ArcadeService(HttpClient httpClient, ILogger<ArcadeService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}ratings", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ArcadePlayerRatingDto>>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting ratings: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}count", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting ratings count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings count: {e.Message}");
        }
        return 0;
    }

    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}distribution", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DistributionResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting distrubution: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting distrubution: {e.Message}");
        }
        return new();
    }
}
