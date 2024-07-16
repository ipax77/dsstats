using dsstats.shared.Calc;

namespace dsstats.shared.Interfaces;

public interface IRatingService
{
    Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request);
    Task ProduceRatings(RatingCalcType ratingCalcType, bool recalc = false);
}