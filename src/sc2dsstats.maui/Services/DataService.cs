﻿using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace sc2dsstats.maui.Services;

public class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly IStatsRepository statsRepository;
    private readonly BuildService buildService;
    private readonly IStatsService statsService;
    private readonly MmrService mmrService;

    public DataService(IReplayRepository replayRepository,
                       IStatsRepository statsRepository,
                       BuildService buildService,
                       IStatsService statsService,
                       MmrService mmrService)
    {
        this.replayRepository = replayRepository;
        this.statsRepository = statsRepository;
        this.buildService = buildService;
        this.statsService = statsService;
        this.mmrService = mmrService;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        var replayDto = await replayRepository.GetReplay(replayHash, true, token);
        if (replayDto == null)
        {
            return null;
        }
        return replayDto;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplaysCount(request, token);
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplays(request, token);
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        return await replayRepository.GetReplayPaths();
    }

    public async Task<List<string>> GetTournaments()
    {
        return await replayRepository.GetTournaments();
    }

    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        return await statsService.GetStatsResponse(request);
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        return await buildService.GetBuild(request, token);
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        return await mmrService.GetRatingsCount(request, token);
    }

    public async Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        return await mmrService.GetRatings(request, token);
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        return await mmrService.GetPlayerRatings(toonId);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        return await mmrService.GetRatingsDeviation();
    }

    public async Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId)
    {
        return await statsService.GetPlayerDetailInfo(toonId);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await mmrService.GetRatingsDeviationStd();
    }

    public async Task<PlayerRatingDto?> GetPlayerRating(int toonId)
    {
        return await mmrService.GetPlayerRating(toonId);
    }

    public async Task<List<RequestNames>> GetTopPlayers(bool std)
    {
        if (std)
        {
            return await Task.FromResult(buildService.GetTopPlayersStd(100));
        }
        else
        {
            return await Task.FromResult(buildService.GetTopPlayersCmdr(100));
        }
    }

    public async Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new CmdrResult());
    }

}
