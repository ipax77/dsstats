using dsstats.shared.Interfaces;
using dsstats.shared;

namespace dsstats.maui8.Services;

public class ReplaysService : IReplaysService
{
    private readonly IReplaysService localReplaysService;
    private readonly IReplaysService remoteReplaysService;
    private readonly DsstatsService dsstatsService;
    private readonly IRemoteToggleService remoteToggleService;

    public ReplaysService([FromKeyedServices("local")] IReplaysService localReplaysService,
                          [FromKeyedServices("remote")] IReplaysService remoteReplaysService,
                          DsstatsService dsstatsService,
                          IRemoteToggleService remoteToggleService)
    {
        this.localReplaysService = localReplaysService;
        this.remoteReplaysService = remoteReplaysService;
        this.dsstatsService = dsstatsService;
        this.remoteToggleService = remoteToggleService;
    }

    public async Task<ReplaysResponse> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteReplaysService.GetReplays(request, token);
        }
        else
        {
            return await localReplaysService.GetReplays(request, token);
        }
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteReplaysService.GetReplaysCount(request, token);
        }
        else
        {
            return await localReplaysService.GetReplaysCount(request, token);
        }
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, bool dry = false, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteReplaysService.GetReplay(replayHash, dry, token);
        }
        else
        {
            return await localReplaysService.GetReplay(replayHash, dry, token);
        }
    }

    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash, bool comboRating)
    {
        if (remoteToggleService.FromServer || comboRating)
        {
            var rating = await remoteReplaysService.GetReplayRating(replayHash, comboRating);
            dsstatsService.AddRemoteRating(replayHash, rating);
            return rating;
        }
        else
        {
            return await localReplaysService.GetReplayRating(replayHash, comboRating);
        }
    }

    public async Task<ArcadeReplayDto?> GetArcadeReplay(string hash, CancellationToken token = default)
    {
        return await remoteReplaysService.GetArcadeReplay(hash, token);
    }
}
