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

    // ratings
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    Task<PlayerDetailsResult> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token);
    Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token);
    Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, int ratingType, CancellationToken token);
    //Task<List<RavenPlayerDto>> GetPlayerRatings(int toonId);
    Task<List<RequestNames>> GetTopPlayers(bool std);

    Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default);
    Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default);
    Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token);
    Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token);
    Task<FunStats> GetFunStats(List<int> toonIds);
    Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token);
    Task<GameInfoResult> GetGameInfo(GameInfoRequest request, CancellationToken token);
    Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token);
    Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token);
}
