using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class CommanderMmr
{
    public int CommanderMmrId { get; set; }
    public Commander Commander { get; set; }
    public Commander SynCommander { get; set; }
    public double Synergy { get; set; }
    public double AntiSynergy { get; set; }
}
