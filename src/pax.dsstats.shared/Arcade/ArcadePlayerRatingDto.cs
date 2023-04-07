namespace pax.dsstats.shared.Arcade;

public record ArcadePlayerRatingDto
{
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public ArcadePlayerRatingPlayerDto ArcadePlayer { get; init; } = null!;
    public ArcadePlayerRatingChangeDto? ArcadePlayerRatingChange { get; init; }
}

public record ArcadePlayerRatingPlayerDto
{
    public string Name { get; set; } = null!;
    public int ProfileId { get; set; }
    public int RegionId { get; set; }
}

public record ArcadePlayerRatingChangeDto
{
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
}