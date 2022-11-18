namespace pax.dsstats.shared;

public record CmdrResult
{
    public Commander Cmdr { get; init; }
    public CmdrPlayed Played { get; init; } = new();
    public float Winrate { get; init; }
    public int AvgDuration { get; init; }
    public StatsResponseItem? BestMatchup { get; init; }
    public StatsResponseItem? WorstMatchup { get; init; }
    public CmdrSynergy? BestSynergy { get; init; }
    public CmdrSynergy? WorstSynergy { get; init; }
    public CmdrDuration BestDuration { get; init; } = new();
    public List<CmdrTopPlayer> TopPlayers { get; init; } = new();
}

public record CmdrTopPlayer
{
    public int ToonId { get; init; }
    public string Name { get; set; } = "";
    public int Count { get; init; }
    public int Wins { get; init; }
}

public record CmdrPlayed
{
    public int Matchups { get; init; }
    public float Per { get; init; }
}

public record CmdrDuration
{
    public string Dur { get; init; } = "";
    public float Wr { get; init; }
}

public record CmdrSynergy
{
    public Commander Cmdr { get; init; }
    public float Wr { get; init; }
}