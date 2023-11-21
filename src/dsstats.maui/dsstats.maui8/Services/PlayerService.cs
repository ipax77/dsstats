using dsstats.shared.Interfaces;
using dsstats.shared;

namespace dsstats.maui8.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerService localPlayerService;
    private readonly IPlayerService remotePlayerService;
    private readonly IRemoteToggleService remoteToggleService;

    public PlayerService([FromKeyedServices("local")] IPlayerService localPlayerService,
                          [FromKeyedServices("remote")] IPlayerService remotePlayerService,
                          IRemoteToggleService remoteToggleService)
    {
        this.localPlayerService = localPlayerService;
        this.remotePlayerService = remotePlayerService;
        this.remoteToggleService = remoteToggleService;
    }

    public async Task<string?> GetPlayerIdName(PlayerId playerId)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerIdName(playerId);
        }
        else
        {
            return await localPlayerService.GetPlayerIdName(playerId);
        }
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetRatingsCount(request, token);
        }
        else
        {
            return await localPlayerService.GetRatingsCount(request, token);
        }
    }

    public async Task<List<ComboPlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetRatings(request, token);
        }
        else
        {
            return await localPlayerService.GetRatings(request, token);
        }
    }

    public async Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId,
                                                                    RatingType ratingType,
                                                                    RatingCalcType ratingCalcType,
                                                                    CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerPlayerIdSummary(playerId, ratingType, ratingCalcType, token);
        }
        else
        {
            return await localPlayerService.GetPlayerPlayerIdSummary(playerId, ratingType, ratingCalcType, token);
        }
    }

    public async Task<PlayerRatingDetails> GetPlayerIdPlayerRatingDetails(PlayerId playerId,
                                                                          RatingType ratingType,
                                                                          RatingCalcType ratingCalcType,
                                                                          CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerIdPlayerRatingDetails(playerId, ratingType, ratingCalcType, token);
        }
        else
        {
            return await localPlayerService.GetPlayerIdPlayerRatingDetails(playerId, ratingType, ratingCalcType, token);
        }
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerIdPlayerCmdrAvgGain(playerId, ratingType, timePeriod, token);
        }
        else
        {
            return await localPlayerService.GetPlayerIdPlayerCmdrAvgGain(playerId, ratingType, timePeriod, token);
        }
    }

    public async Task<PlayerDetailResponse> GetPlayerIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerIdPlayerDetails(request, token);
        }
        else
        {
            return await localPlayerService.GetPlayerIdPlayerDetails(request, token);
        }
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerRatingChartData(playerId, ratingType, token);
        }
        else
        {
            return await localPlayerService.GetPlayerRatingChartData(playerId, ratingType, token);
        }
    }

    public async Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        if (remoteToggleService.FromServer)
        {
            return await remotePlayerService.GetPlayerIdCommandersPlayed(playerId, ratingType, token);
        }
        else
        {
            return await localPlayerService.GetPlayerIdCommandersPlayed(playerId, ratingType, token);
        }
    }

    public Task<DistributionResponse> GetDistribution(DistributionRequest request)
    {
        throw new NotImplementedException();
    }
}
