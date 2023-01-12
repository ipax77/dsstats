using pax.dsstats.shared;

namespace dsstats.mmr;

public partial class MmrService
{
    private static double GetCorrectedConfidenceFactor(double playerConfidence, double replayConfidence)
    {
        double totalConfidenceFactor = (0.5 * (1 - GetConfidenceFactor(playerConfidence))) + (0.5 * GetConfidenceFactor(replayConfidence));
        return 1 + MmrOptions.confidenceImpact * (totalConfidenceFactor - 1);
    }

    private static double GetConfidenceFactor(double confidence)
    {
        double variance = ((MmrOptions.distributionMult * 0.4) + (1 - confidence));

        return MmrOptions.distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK = 32)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + MmrOptions.consistencyImpact * (raw_revConsistency - 1);
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    public static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip = 400)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }
}
