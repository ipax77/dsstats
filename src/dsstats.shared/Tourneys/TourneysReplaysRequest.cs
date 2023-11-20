namespace dsstats.shared;

public record TourneysReplaysRequest
{
    public int Skip { get; set; }
    public int Take { get; set; }
    public Guid EventGuid { get; set; }
    public string Tournament { get; set; } = string.Empty;
    public List<TableOrder> Orders { get; set; } = new();
}

public record TourneysReplayListDto
{
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public GameMode GameMode { get; init; }
    public bool TournamentEdition { get; init; }
    public string ReplayHash { get; init; } = string.Empty;
    public bool DefaultFilter { get; init; }
    public string CommandersTeam1 { get; init; } = string.Empty;
    public string CommandersTeam2 { get; init; } = string.Empty;
    public int MaxLeaver { get; init; }
    public double? Exp2Win { get; init; }
    public int AvgRating { get; init; }
    public ReplayEventDto? ReplayEvent { get; init; }
    public ReplayPlayerInfo? PlayerInfo { get; set; }
}