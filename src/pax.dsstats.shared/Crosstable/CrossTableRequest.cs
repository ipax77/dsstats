namespace pax.dsstats.shared;

public record CrossTableRequest
{
    public string Mode { get; set; } = "Standard";
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Past90Days;
    public bool TeMaps { get; set; }
}

public record CrossTableReplaysRequest
{
    public string Mode { get; set; } = "Standard";
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Past90Days;
    public bool TeMaps { get; set; }
    public TeamCmdrs TeamCmdrs { get; set; } = null!;
    public TeamCmdrs? TeamCmdrsVs { get; set; }
}

public record TeamCompRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public bool WithLeavers { get; set; }
    public string? Interest { get; set; }
}

public record TeamCompResponse
{
    public string? Team { get; set; }
    public List<TeamResponseItem> Items { get; set; } = new();
    public List<TeamReplayInfo> Replays { get; set; } = new();
}

public record TeamResponseItem
{
    public string Team { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
}

public record TeamReplayInfo
{
    public DateTime GameTime { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public string CommandersTeam1 { get; set; } = string.Empty;
    public string CommandersTeam2 { get; set; } = string.Empty;
}
