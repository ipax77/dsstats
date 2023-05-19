using pax.dsstats.shared.Arcade;

namespace pax.dsstats.shared;
public interface IDataService
{
    void SetFromServer(bool fromServer);
    bool GetFromServer();
    Task<ReplayDetailsDto?> GetDetailReplay(string replayHash, CancellationToken token = default);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default);
    Task<int> GetEventReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<ReplayListEventDto>> GetEventReplays(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<string>> GetReplayPaths();
    Task<List<EventListDto>> GetTournaments();
    Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default);
    Task<StatsResponse> GetTourneyStats(StatsRequest request, CancellationToken token = default);
    Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default);
    Task<CmdrStrengthResult> GetCmdrStrengthResults(CmdrStrengthRequest request, CancellationToken token);

    // ratings
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    Task<PlayerDetailsResult> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token);
    Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token);
    Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, int ratingType, CancellationToken token);
    Task<List<PlayerRatingReplayCalcDto>> GetPlayerIdCalcRatings(PlayerIdRatingRequest request, CancellationToken token);
    Task<ReplayRatingDto?> GetOnlineRating(ReplayDetailsDto replayDto);
    Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default);

    //Task<List<RavenPlayerDto>> GetPlayerRatings(int toonId);
    Task<List<RequestNames>> GetTopPlayers(bool std);
    Task<BuildRatingResponse> GetBuildByRating(BuildRatingRequest request, CancellationToken token = default);

    Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default);
    Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default);
    Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token);
    Task<ToonIdRatingResponse> GetPlayerIdRatings(PlayerIdRatingRequest request, CancellationToken token);
    Task<FunStats> GetFunStats(List<int> toonIds);
    Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token);
    Task<GameInfoResult> GetGameInfo(GameInfoRequest request, CancellationToken token);
    Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token);
    Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token);
    Task<PlayerDetailResponse> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token);
    Task<PlayerDetailSummary> GetPlayerSummary(int toonId, CancellationToken token = default);
    Task<PlayerRatingDetails> GetPlayerRatingDetails(int toonId, RatingType ratingType, CancellationToken token = default);
    Task<List<PlayerCmdrAvgGain>> GetPlayerCmdrAvgGain(int toonId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token = default);
    Task<FunStatsResult> GetFunStats(FunStatsRequest request, CancellationToken token);
    Task<int> GetCmdrReplayInfosCount(CmdrInfoRequest request, CancellationToken token = default);
    Task<List<ReplayCmdrInfo>> GetCmdrReplayInfos(CmdrInfoRequest request, CancellationToken token);
    Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default);
    Task<int> GetCmdrReplaysCount(CmdrInfosRequest request, CancellationToken token = default);
    Task<List<ReplayCmdrListDto>> GetCmdrReplays(CmdrInfosRequest request, CancellationToken token = default);
    Task<PlayerDetailResponse> GetIdPlayerDetails(PlayerDetailRequest request, CancellationToken token);
    Task<PlayerDetailSummary> GetIdPlayerSummary(PlayerId playerId, CancellationToken token = default);
    Task<PlayerRatingDetails> GetIdPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default);
    Task<List<PlayerCmdrAvgGain>> GetIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token = default);
    Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType);
}
