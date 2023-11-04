using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class ReplayDownloadCount
{
    public int ReplayDownloadCountId { get; set; }

    public string ReplayHash { get; set; } = null!;
}
