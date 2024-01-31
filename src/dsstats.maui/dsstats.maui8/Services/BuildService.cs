using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.maui8.Services;

public class BuildService : IBuildService
{
    private readonly IRemoteToggleService remoteToggleService;
    private readonly IBuildService localBuildService;
    private readonly IBuildService remoteBuildService;
    private readonly ConfigService configService;

    public BuildService(IRemoteToggleService remoteToggleService,
                        [FromKeyedServices("local")] IBuildService localBuildService,
                        [FromKeyedServices("remote")] IBuildService remoteBuildService,
                        ConfigService configService)
    {
        this.remoteToggleService = remoteToggleService;
        this.localBuildService = localBuildService;
        this.remoteBuildService = remoteBuildService;
        this.configService = configService;
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetBuild(request, token);
        }
        else
        {
            return await localBuildService.GetBuild(request, token); ;
        }
    }

    public async Task<List<RequestNames>> GetDefaultPlayers()
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetDefaultPlayers();
        }
        else
        {
            return configService.GetRequestNames();
        }
    }

    public async Task<BuildMapResponse> GetReplayBuildMap(BuildRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetReplayBuildMap(request, token);
        }
        else
        {
            return await localBuildService.GetReplayBuildMap(request, token);
        }
    }

    public async Task<List<ReplayListDto>> GetReplays(BuildRequest request, int skip, int take, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetReplays(request, skip, take, token);
        }
        else
        {
            return await localBuildService.GetReplays(request, skip, take, token);
        }
    }

    public async Task<int> GetReplaysCount(BuildRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetReplaysCount(request, token);
        }
        else
        {
            return await localBuildService.GetReplaysCount(request, token);
        }
    }

    public async Task<List<RequestNames>> GetTopPlayers(RatingType ratingType)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteBuildService.GetTopPlayers(ratingType);
        }
        else
        {
            return await localBuildService.GetTopPlayers(ratingType);
        }
    }
}
