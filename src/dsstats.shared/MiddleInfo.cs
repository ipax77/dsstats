namespace dsstats.shared;

public record MiddleInfo
{
    public int Duration { get; set; }
    public int Cannon { get; set; }
    public int Bunker { get; set; }
    public int WinnerTeam { get; set; }
    public int StartTeam { get; set; }
    public List<double> MiddleChanges { get; set; } = new();
    public int Team1Income { get; set; }
    public int Team2Income { get; set; }
    public double Team1Percentage { get; set; }
    public double Team2Percentage { get; set; }
}