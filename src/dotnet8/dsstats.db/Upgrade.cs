using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Upgrade
{
    public int UpgradeId { get; set; }

    public string Name { get; set; } = null!;

    public int Cost { get; set; }

    public virtual ICollection<PlayerUpgrade> PlayerUpgrades { get; set; } = new List<PlayerUpgrade>();

    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; } = new List<ReplayPlayer>();
}
