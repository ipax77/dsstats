namespace pax.dsstats.shared;

public record ReplayDetailsDto
{
    public string FileName { get; set; } = null!;
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public PlayerResult PlayerResult { get; set; }
    public int PlayerPos { get; set; }
    public bool ResultCorrected { get; set; }
    public GameMode GameMode { get; init; }
    public int Objective { get; init; }
    public int Bunker { get; init; }
    public int Cannon { get; init; }
    public int Minkillsum { get; init; }
    public int Maxkillsum { get; init; }
    public int Minarmy { get; init; }
    public int Minincome { get; init; }
    public int Maxleaver { get; init; }
    public byte Playercount { get; init; }
    public string ReplayHash { get; set; } = "";
    public bool DefaultFilter { get; set; }
    public int Views { get; init; }
    public int Downloads { get; init; }
    public string Middle { get; init; } = null!;
    public string CommandersTeam1 { get; init; } = null!;
    public string CommandersTeam2 { get; init; } = null!;
    public bool TournamentEdition { get; init; }
    public ReplayEventDto? ReplayEvent { get; set; }
    public ReplayRatingDto? ReplayRatingInfo { get; set; }
    public ICollection<ReplayPlayerDto> ReplayPlayers { get; init; } = new HashSet<ReplayPlayerDto>();
}
