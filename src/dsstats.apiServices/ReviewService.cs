using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class ReviewService(HttpClient httpClient, ILogger<ReviewService> logger) : IReviewService
{
    private readonly string playerController = "api8/v1/player";

    public async Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{playerController}/review", request, token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<ReviewResponse>();
            return content ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting review: {error}", ex.Message);
        }
        return new();
    }
}
