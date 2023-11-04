namespace dsstats.shared;

public record CountResponse
{
    public int Replays { get; set; }
    public int Matchups { get; set; }
    public int LeaverReplays { get; set; }
    public int Quits { get; set; }
    public int Duration { get; set; }
    public List<CountEnt> CountEnts { get; set; } = new();
}

public record CountEnt
{
    public Commander Commander { get; set; }
    public int Matchups { get; set; }
    public int Replays { get; set; }
    public double AvgRating { get; set; }
}

