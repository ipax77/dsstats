using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class CommanderMmr
{
    public int CommanderMmrId { get; set; }

    public Commander Race { get; set; }
    public Commander OppRace { get; set; }

    public double SynergyMmr { get; set; }
    public double AntiSynergyMmr { get; set; }

    //public double AntiSynergyElo { get; set; }//=> FireMmrService.ELO(this.AntiSynergyMmr_1, this.AntiSynergyMmr_2);
}
