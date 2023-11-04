using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Event
{
    public int EventId { get; set; }

    public string Name { get; set; } = null!;

    public Guid EventGuid { get; set; }

    public DateTime EventStart { get; set; }

    public int GameMode { get; set; }

    public string? WinnerTeam { get; set; }

    public virtual ICollection<ReplayEvent> ReplayEvents { get; set; } = new List<ReplayEvent>();
}
