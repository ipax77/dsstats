using dsstats.shared.Calc;
namespace dsstats.shared.Interfaces;

public interface ICalcRepository
{
    Task CreatePlayerRatingCsv(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, RatingCalcType ratingCalcType);
    (int, int) CreateOrAppendReplayAndReplayPlayerRatingsCsv(List<shared.Calc.ReplayRatingDto> replayRatingDtos, int replayAppendId, int replayPlayerAppendId, RatingCalcType ratingCalcType);
    Task PlayerRatingsFromCsv2MySql(RatingCalcType ratingCalcType);
    Task ReplayPlayerRatingsFromCsv2MySql(RatingCalcType ratingCalcType);
    Task ReplayRatingsFromCsv2MySql(RatingCalcType ratingCalcType);
    Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request);
    Task<List<CalcDto>> GetSc2ArcadeCalcDtos(Sc2ArcadeRequest request);
    Task SetMainCmdr(int ratingType);
    Task SetPlayerRatingsPos(RatingCalcType ratingCalcType);
    Task SetRatingChange(RatingCalcType ratingCalcType);
    Task WriteRatingsToSqlite(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                           List<Calc.ReplayRatingDto> replayRatingDtos,
                                           int replayAppendId,
                                           int replayPlayerAppendId,
                                           bool isRecalc);
    Task<CalcRatingRequest?> GetDsstatsCalcRatingRequest();
    Task StoreDsstatsResult(CalcRatingResult result, bool isContinue);
    Task CleanupComboPreRatings();
    Task DebugDeleteContinueTest();
}