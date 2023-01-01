
using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;

public interface IRatingRepository
{
    private const string csvBasePath = "/data/mysqlfiles";

    Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId, string csvBasePath = csvBasePath);
    Task<UpdateResult> UpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings, bool continueCalc, string csvBasePath = csvBasePath);
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default);
    Task SetReplayListMmrChanges(List<ReplayListDto> replays, string? searchPlayer = null, CancellationToken token = default);
    Task SetReplayListMmrChanges(List<ReplayListDto> replays, int toonId, CancellationToken token = default);
    Task<List<RequestNames>> GetTopPlayers(RatingType ratingType, int minGames);
    Task<string?> GetToonIdName(int toonId);
    Task<RequestNames?> GetRequestNames(int toonId);
    Task<List<RequestNames>> GetRequestNames(string name);
    List<int> GetNameToonIds(string name);
    Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos, MmrOptions mmrOptions);
    Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token);
}
