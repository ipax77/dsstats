
using dsstats.indexedDb.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.pwa.Services;

public class ReplayRepository(IndexedDbService dbService, ILogger<ReplayRepository> logger) : IReplayRepository
{
    public async Task<ReplayDetails?> GetReplayDetails(string replayHash)
    {
        var raw = await dbService.GetReplayByHashAsync(replayHash);

        if (raw is null) return null;

        return new ReplayDetails { Replay = raw };
    }

    public async Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            return await dbService.GetFilteredReplayListsAsync(new(request));
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting replays: {error}", ex.Message);
        }
        return [];
    }


    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await dbService.GetFilteredReplayListsCountAsync(new(request));
    }
    public Task<List<ReplayListDto>> GetReplays2(ReplaysRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<ReplayDetails?> GetArcadeReplayDetails(string replayHash)
    {
        throw new NotImplementedException();
    }

    public Task<List<ReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetArcadeReplaysCount(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<ReplayRatingDto?> GetReplayRating(string replayHash)
    {
        throw new NotImplementedException();
    }

    public Task SaveReplayRatingAll(string replayHash, ReplayRatingDto rating)
    {
        throw new NotImplementedException();
    }

    public Task<ReplayDetails?> GetLatestReplay()
    {
        throw new NotImplementedException();
    }

    public Task<ReplayDetails?> GetNextReplay(bool after, string replayHash)
    {
        throw new NotImplementedException();
    }
}



