using pax.dsstats.shared;

namespace dsstats.mmr.Maths;

public static class GaussianElo
{
    public static Gaussian GetRatingAfter(Gaussian rating,
                                          int actualResult,
                                          double prediction,
                                          Gaussian matchDist,
                                          double playerImpact,
                                          MmrOptions mmrOptions)
    {
        double delta = (actualResult - prediction) * mmrOptions.EloK * playerImpact;

        var info = Gaussian.ByMeanDeviation(rating.Mean + delta, matchDist.Deviation);
        var ratingAfter = rating * info;

        return ratingAfter;
    }

    public static (double, Gaussian) PredictMatch(Gaussian a, Gaussian b, double deviationOffset = 0)
    {
        var subtraction = b - a;
        var match = Gaussian.ByMeanDeviation(subtraction.Mean, subtraction.Deviation + deviationOffset);

        return (match.CDF(0), match);
    }
}
