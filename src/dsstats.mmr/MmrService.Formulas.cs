using pax.dsstats.shared;

namespace dsstats.mmr;

public partial class MmrService
{
    private static double GetDecayFactor(TimeSpan timeSpan, MmrOptions mmrOptions)
    {
        return 1 + (timeSpan.TotalDays / mmrOptions.DoubleAtDays);
    }
}
