using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace pax.dsstats.dbng;

public class CommanderMmr
{
    public int CommanderMmrId { get; set; }

    public double SynergyMmr { get; set; }

    public Commander Commander_1 { get; set; }
    public Commander Commander_2 { get; set; }

    public double AntiSynergyMmr_1 { get; set; }
    public double AntiSynergyMmr_2 { get; set; }

    public double AntiSynergyElo_1 { get; set; }//=> FireMmrService.ELO(this.AntiSynergyMmr_1, this.AntiSynergyMmr_2);
    public double AntiSynergyElo_2 { get; set; }//=> 1 - this.AntiSynergyElo_1;
}
