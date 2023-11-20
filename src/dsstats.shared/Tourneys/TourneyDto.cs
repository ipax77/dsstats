namespace dsstats.shared;

public record TourneyDto
{
    public string Name { get; set; } = string.Empty;
    public Guid EventGuid { get; set; }
    public DateTime EventStart { get; set; }
    public GameMode GameMode { get; set; }
    public string? WinnerTeam { get; set; }
}
