
namespace dsstats.shared;

public record TimelineResponse
{
    public List<TimelineEnt> TimeLineEnts { get; set; } = new();
}

public record TimelineEnt
{
    public Commander Commander { get; set; }
    public DateTime Time { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgRating { get; set; }
    public double AvgGain { get; set; }
    public double Strength { get; set; }
}
