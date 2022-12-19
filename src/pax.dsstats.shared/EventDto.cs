using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.shared;

public record EventDto
{
    [MaxLength(200)]
    public string Name { get; set; } = "unknown";
}

public record ReplayEventDto
{
    [MaxLength(200)]
    public string Round { get; set; } = "GroupA";
    public string WinnerTeam { get; set; } = "Team1";
    public string RunnerTeam { get; set; } = "Team2";
    public Commander Ban1 { get; set; }
    public Commander Ban2 { get; set; }
    public Commander Ban3 { get; set; }
    public Commander Ban4 { get; set; }
    public Commander Ban5 { get; set; }
    public EventDto Event { get; set; } = new();
}

public record EventListDto
{
    public string Name { get; init; } = "";
    public string? WinnerTeam { get; init; }
    public GameMode GameMode { get; init; }
}