using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class ReplayEvent
{
    public int ReplayEventId { get; set; }

    public string Round { get; set; } = null!;

    public string WinnerTeam { get; set; } = null!;

    public string RunnerTeam { get; set; } = null!;

    public int Ban1 { get; set; }

    public int Ban2 { get; set; }

    public int Ban3 { get; set; }

    public int Ban4 { get; set; }

    public int Ban5 { get; set; }

    public int EventId { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<Replay> Replays { get; set; } = new List<Replay>();
}
