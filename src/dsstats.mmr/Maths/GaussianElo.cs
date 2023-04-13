namespace dsstats.mmr.Maths;

public static class GaussianElo
{
    public static Gaussian GetRatingAfter(Gaussian rating,
                                          int actualResult,
                                          double prediction,
                                          Gaussian matchDist,
                                          double standardPlayerDeviation,
                                          double standardMatchDeviation,
                                          double playerImpact)
    {
        double delta = (actualResult - prediction) * standardMatchDeviation * playerImpact;// * rating.Deviation * (standardMatchDeviation / (matchDist.Deviation - standardPlayerDeviation));

        var info = Gaussian.ByMeanDeviation(rating.Mean + delta, matchDist.Deviation);
        var ratingAfter = rating * info;

        //ratingAfter = Gaussian.ByMeanDeviation(ratingAfter.Mean, System.Math.Min(standardPlayerDeviation, ratingAfter.Deviation));

        return ratingAfter;
    }

    public static (double, Gaussian) PredictMatch(Gaussian a, Gaussian b, double deviationOffset = 0)
    {
        var subtraction = b - a;
        var match = Gaussian.ByMeanDeviation(subtraction.Mean, subtraction.Deviation + deviationOffset);

        return (match.CDF(0), match);
    }
}
