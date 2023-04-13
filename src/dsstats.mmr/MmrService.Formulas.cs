using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

namespace dsstats.mmr;

public partial class MmrService
{
    private static double GetDecayFactor(TimeSpan timeSpan)
    {
        double doubleAtDays = 180; // after x days without playing, the decayFactor = * 2.0

        return 1 + (timeSpan.TotalDays / doubleAtDays);
    }
}
