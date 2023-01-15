
namespace pax.dsstats.shared;

public interface IRatingRepository
{
    private const string csvBasePath = "/data/mysqlfiles";

    Task<(int, int)> UpdateMmrChanges(List<ReplayRatingDto> replayRatingDtos, int replayAppendId, int playerAppendId, string csvBasePath = csvBasePath);
    Task<UpdateResult> UpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings, bool continueCalc, string csvBasePath = csvBasePath);
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<RequestNames>> GetTopPlayers(RatingType ratingType, int minGames);
    Task<string?> GetToonIdName(int toonId);
    Task<RequestNames?> GetRequestNames(int toonId);
    Task<List<RequestNames>> GetRequestNames(string name);
    List<int> GetNameToonIds(string name);
    Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token);
    Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token);
    Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token);
    Task SeedRatingChanges();
}
