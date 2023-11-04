using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class CommanderMmr
{
    public int CommanderMmrId { get; set; }

    public int Race { get; set; }

    public int OppRace { get; set; }

    public double SynergyMmr { get; set; }

    public double AntiSynergyMmr { get; set; }
}
