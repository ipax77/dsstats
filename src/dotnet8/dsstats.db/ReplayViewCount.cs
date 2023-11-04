using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class ReplayViewCount
{
    public int ReplayViewCountId { get; set; }

    public string ReplayHash { get; set; } = null!;
}
