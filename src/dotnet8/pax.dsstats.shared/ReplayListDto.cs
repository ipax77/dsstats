using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record ReplayListDto
{
    public int ReplayId { get; set; }
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int PlayerPos { get; set; }
    public GameMode GameMode { get; set; }
    public string ReplayHash { get; set; } = null!;
    public bool DefaultFilter { get; set; }
    public string CommandersTeam1 { get; set; } = null!;
    public string CommandersTeam2 { get; set; } = null!;
    public int MaxLeaver { get; init; }
    public double MmrChange { get; set; }
    public Commander Commander { get; set; }
    [JsonIgnore]
    public Commander[] Cmdrs1 => CommandersTeam1.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
    [JsonIgnore]
    public Commander[] Cmdrs2 => CommandersTeam2.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
}

public record ReplayListEventDto : ReplayListDto
{
    public ReplayEventListDto? ReplayEvent { get; set; }
}

public record ReplayEventListDto
{
    public string Round { get; set; } = null!;
    public string WinnerTeam { get; set; } = null!;
    public string RunnerTeam { get; set; } = null!;
    public EventDto Event { get; init; } = null!;
}

public record ReplayListRatingDto : ReplayListDto
{
    public ReplayRatingListDto? ReplayRatingInfo { get; set; }
    public List<ReplayPlayerListDto> ReplayPlayers { get; set; } = new();
}

public record ReplayPlayerListDto
{
    public string Name { get; set; } = string.Empty;
    public int GamePos { get; set; }
    public Commander Race { get; set; }
    public PlayerListDto Player { get; set; } = null!;
}

public record PlayerListDto
{
    public int ToonId { get; set; }
}

public record ReplayRatingListDto
{
    public List<RepPlayerRatingListDto> RepPlayerRatings { get; set; } = new();
}

public record RepPlayerRatingListDto
{
    public int GamePos { get; set; }
    public float RatingChange { get; set; }
}