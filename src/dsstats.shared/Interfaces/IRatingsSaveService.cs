using dsstats.shared.Calc;
using System.Collections.Frozen;

namespace dsstats.shared.Interfaces;

public interface IRatingsSaveService
{
    Task SaveArcadePlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans);
    Task<(int, int)> SaveArcadeStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
    Task SaveComboPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans);
    Task<(int, int)> SaveComboStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
    Task SaveDsstatsPlayerRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, FrozenDictionary<PlayerId, bool> softbans, bool isContinue);
    Task<(int, int)> SaveDsstatsStepResult(List<shared.Calc.ReplayRatingDto> ratings, int replayRatingId = 0, int replayPlayerRatingId = 0);
    Task SaveContinueComboRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings, List<shared.Calc.ReplayRatingDto> replayRatings);
    Task SaveContinueArcadeRatings(Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings,
                                            List<shared.Calc.ReplayRatingDto> replayRatings);
}