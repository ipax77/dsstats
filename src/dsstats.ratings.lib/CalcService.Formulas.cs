

using pax.dsstats.shared.Calc;

namespace dsstats.ratings.lib;

public partial class CalcService

{
    private static double GetPlayerImpact(CalcRating calcRating, double teamConfidence, CalcRatingRequest request)
    {
        double factor_consistency = GetCorrectedRevConsistency(1 - calcRating.Consistency, request.MmrOptions.consistencyImpact);
        double factor_confidence = GetCorrectedConfidenceFactor(calcRating.Confidence,
                                                                teamConfidence,
                                                                request.MmrOptions.distributionMult,
                                                                request.MmrOptions.confidenceImpact);

        return 1
            * (request.MmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (request.MmrOptions.UseConfidence ? factor_confidence : 1.0);
    }

    private static double GetCorrectedConfidenceFactor(double playerConfidence,
                                                       double replayConfidence,
                                                       double distributionMult,
                                                       double confidenceImpact)
    {
        double totalConfidenceFactor = (0.5 * (1 - GetConfidenceFactor(playerConfidence, distributionMult))) + (0.5 * GetConfidenceFactor(replayConfidence, distributionMult));
        return 1 + confidenceImpact * (totalConfidenceFactor - 1);
    }

    private static double GetConfidenceFactor(double confidence, double distributionMult)
    {
        double variance = ((distributionMult * 0.4) + (1 - confidence));

        return distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency, double consistencyImpact)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }
}