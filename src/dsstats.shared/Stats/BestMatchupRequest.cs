namespace dsstats.shared.Stats;

public record MatchupRequest
{
    public TimePeriod TimePeriod { get; set; }
    public Commander Commander1 { get; set; }
    public Commander Commander2 { get; set; }
}

public record MatchupResponse
{
    public MatchupRequest Request { get; set; } = new();
    public int Count { get; set; }
    public List<MatchupCmdrResult> Results { get; set; } = [];
}

public record MatchupCmdrResult
{
    public Commander Commander { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
}