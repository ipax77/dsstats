using dsstats.shared.Interfaces;
using dsstats.shared;

namespace dsstats.maui.Services;

public class ReplaysService : IReplaysService
{
    private readonly IReplaysService localReplaysService;
    private readonly IReplaysService remoteReplaysService;
    private readonly IRemoteToggleService remoteToggleService;

    public ReplaysService([FromKeyedServices("local")] IReplaysService localReplaysService,
                          [FromKeyedServices("remote")] IReplaysService remoteReplaysService,
                          IRemoteToggleService remoteToggleService)
    {
        this.localReplaysService = localReplaysService;
        this.remoteReplaysService = remoteReplaysService;
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
            return await remoteReplaysService.GetReplayRating(replayHash, comboRating);
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
