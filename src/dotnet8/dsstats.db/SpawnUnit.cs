using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class SpawnUnit
{
    public int SpawnUnitId { get; set; }

    public byte Count { get; set; }

    public string Poss { get; set; } = null!;

    public int UnitId { get; set; }

    public int SpawnId { get; set; }

    public virtual Spawn Spawn { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
