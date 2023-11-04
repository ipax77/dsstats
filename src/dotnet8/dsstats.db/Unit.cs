using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Unit
{
    public int UnitId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<SpawnUnit> SpawnUnits { get; set; } = new List<SpawnUnit>();
}
