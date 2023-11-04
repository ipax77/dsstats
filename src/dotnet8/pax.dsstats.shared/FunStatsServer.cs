
namespace pax.dsstats.shared;

public record FunStatsRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    
}

public record FunStatsResult
{
    public DateTime Created { get; init; }
    public long TotalTimePlayed { get; init; }
    public int AvgGameDuration { get; init; }
    public KeyValuePair<UnitInfo, UnitInfo> MostLeastBuildUnit { get; init; }
    public ReplayDetailsDto? FirstReplay { get; init; }
    public ReplayDetailsDto? GreatestArmyReplay { get; init; }
    public ReplayDetailsDto? MostUpgradesReplay { get; init; }
    public ReplayDetailsDto? MostCompetitiveReplay { get; init; }
    public ReplayDetailsDto? GreatestComebackReplay { get; init; }
}
