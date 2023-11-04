using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class PlayerUpgrade
{
    public int PlayerUpgradeId { get; set; }

    public int Gameloop { get; set; }

    public int UpgradeId { get; set; }

    public int ReplayPlayerId { get; set; }

    public virtual ReplayPlayer ReplayPlayer { get; set; } = null!;

    public virtual Upgrade Upgrade { get; set; } = null!;
}
