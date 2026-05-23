
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
        var rating = await dbService.GetReplayRatingAsync(replayHash);
        return new ReplayDetails
        {
            ReplayHash = replayHash,
            Replay = raw,
            ReplayRatings = rating is not null ? [rating] : [],
        };
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

    public async Task<byte[]?> GetReplaySpawnPlayback(string replayHash, CancellationToken token = default)
    {
        return await dbService.GetReplaySpawnPlaybackAsync(replayHash);
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

    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash)
        => await dbService.GetReplayRatingAsync(replayHash);

    public async Task SaveReplayRatingAll(string replayHash, ReplayRatingDto rating)
        => await dbService.SaveReplayRatingAsync(replayHash, rating);

    public Task<ReplayDetails?> GetLatestReplay()
        => GetReplayAtIndex(0);

    public async Task<ReplayDetails?> GetNextReplay(bool after, string replayHash)
    {
        var replays = await GetOrderedReplayListAsync();
        var currentIndex = replays.FindIndex(replay => replay.ReplayHash == replayHash);
        if (currentIndex < 0)
        {
            return null;
        }

        var nextIndex = after ? currentIndex - 1 : currentIndex + 1;
        return await GetReplayAtIndex(nextIndex, replays);
    }

    private async Task<ReplayDetails?> GetReplayAtIndex(int index, List<ReplayListDto>? orderedReplays = null)
    {
        var replays = orderedReplays ?? await GetOrderedReplayListAsync();
        if (index < 0 || index >= replays.Count)
        {
            return null;
        }

        return await GetReplayDetails(replays[index].ReplayHash);
    }

    private async Task<List<ReplayListDto>> GetOrderedReplayListAsync()
    {
        var request = new ReplaysRequest
        {
            Skip = 0,
            Take = 20_000,
        };

        return await dbService.GetFilteredReplayListsAsync(new ReplayFilter(request));
    }
}


