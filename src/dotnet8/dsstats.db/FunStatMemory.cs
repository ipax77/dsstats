using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class FunStatMemory
{
    public int FunStatsMemoryId { get; set; }

    public DateTime Created { get; set; }

    public int RatingType { get; set; }

    public int TimePeriod { get; set; }

    public long TotalTimePlayed { get; set; }

    public int AvgGameDuration { get; set; }

    public string UnitNameMost { get; set; } = null!;

    public int UnitCountMost { get; set; }

    public string UnitNameLeast { get; set; } = null!;

    public int UnitCountLeast { get; set; }

    public string? FirstReplay { get; set; }

    public string? GreatestArmyReplay { get; set; }

    public string? MostUpgradesReplay { get; set; }

    public string? MostCompetitiveReplay { get; set; }

    public string? GreatestComebackReplay { get; set; }
}
