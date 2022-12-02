namespace pax.dsstats.shared;

public record PlayerRatingDto
{
    public double Rating { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public PlayerRatingPlayerDto Player { get; init; } = null!;
}

public record PlayerRatingPlayerDto
{
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
}

