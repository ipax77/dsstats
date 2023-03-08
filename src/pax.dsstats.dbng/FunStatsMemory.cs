
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class FunStatsMemory
{
    public int FunStatsMemoryId { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; }
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public long TotalTimePlayed { get; set; }
    public int AvgGameDuration { get; set; }
    public string UnitNameMost { get; set; } = string.Empty;
    public int UnitCountMost { get; set; }
    public string UnitNameLeast { get; set; } = string.Empty;
    public int UnitCountLeast { get; set; }
    public string? FirstReplay { get; set; }
    public string? GreatestArmyReplay { get; set; }
    public string? MostUpgradesReplay { get; set; }
    public string? MostCompetitiveReplay { get; set; }
    public string? GreatestComebackReplay { get; set; }
}