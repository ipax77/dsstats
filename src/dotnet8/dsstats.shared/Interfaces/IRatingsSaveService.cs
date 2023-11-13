using dsstats.shared.Calc;

namespace dsstats.shared.Interfaces;

public interface IRatingsSaveService
{
    Task SaveArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings);
    Task<(int, int)> SaveArcadeStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
    Task SaveComboPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings);
    Task<(int, int)> SaveComboStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
    Task SaveDsstatsPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, bool isContinue);
    Task<(int, int)> SaveDsstatsStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
}