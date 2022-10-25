namespace sc2dsstats.shared;
public record PlayerNameResponse
{
    public string Name { get; init; } = "PAX";
    public int Games { get; init; }
    public int Wins { get; init; }
    public PlayerNameStatsResponse? Stats { get; set; }
}

public record PlayerNameStatsResponse
{
    public int TeamGames { get; init; }
    public int TeamWins { get; init; }
    public int OppGames { get; init; }
    public int OppWins { get; init; }
}
