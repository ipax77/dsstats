using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;
public interface IDataService
{
    Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default);
    Task<ICollection<string>> GetReplayPaths();
    Task<List<string>> GetTournaments();
    Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default);
    Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default);

    // ratings
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    //Task<List<RavenPlayerDto>> GetPlayerRatings(int toonId);
    Task<List<RequestNames>> GetTopPlayers(bool std);

    Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default);
    Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default);
}
