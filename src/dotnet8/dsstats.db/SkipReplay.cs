using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class SkipReplay
{
    public int SkipReplayId { get; set; }

    public string Path { get; set; } = null!;
}
