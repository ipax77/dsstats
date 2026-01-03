using dsstats.indexedDb.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.pwa.Services;

public class PlayerService(IndexedDbService dbService) : IPlayerService
{
    public Task<CmdrStrenghtResponse> GetCmdrPlayerInfos(CmdrStrenghtRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<CmdrAvgGainResponse> GetCommandersPerformance(PlayerStatsRequest request, CancellationToken token)
    {
        return Task.FromResult(new CmdrAvgGainResponse());
    }

    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request, CancellationToken token = default)
    {
        return await dbService.GetPlayerStats(request.Player);
    }

    public Task<RatingDetails> GetRatingDetails(PlayerStatsRequest request, CancellationToken token = default)
    {
        return Task.FromResult(new RatingDetails());
    }

    public Task<DistributionResponse> GetRatingDistribution(DistributionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<List<PlayerRatingListItem>> GetRatings(PlayerRatingsRequest request, CancellationToken token = default)
    {
        return Task.FromResult(new List<PlayerRatingListItem>());
    }

    public Task<int> GetRatingsCount(PlayerRatingsRequest request, CancellationToken token = default)
    {
        return Task.FromResult(0);
    }
}
