using pax.dsstats.shared.Calc;

namespace pax.dsstats.shared.Interfaces;

public interface ICalcRepository
{
    Task CreateDsstatsPlayerRatingCsv(Dictionary<int, Dictionary<PlayerId, Calc.CalcRating>> mmrIdRatings);
    (int, int) DsstatsCreateOrAppendReplayAndReplayPlayerRatingsCsv(List<Calc.ReplayRatingDto> replayRatingDtos, int replayAppendId, int replayPlayerAppendId);
    Task DsstatsPlayerRatingsFromCsv2MySql();
    Task DsstatsReplayPlayerRatingsFromCsv2MySql();
    Task DsstatsReplayRatingsFromCsv2MySql();
    Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request);
    Task<List<CalcDto>> GetSc2ArcadeCalcDtos(Sc2ArcadeRequest request);
    Task SetMainCmdr(int ratingType);
    Task SetPlayerRatingsPos();
    Task SetRatingChange();
    Task<List<CalcDto>> TestGetDsstatsCalcDtos(int playerId);
    Task<List<CalcDto>> TestGetSc2ArcadeCalcDtos(int playerId);
}