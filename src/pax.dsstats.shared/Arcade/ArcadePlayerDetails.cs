namespace pax.dsstats.shared.Arcade;

public record ArcadePlayerDetails
{
    public ArcadePlayerDto? ArcadePlayer { get; set; }
    public List<ArcadePlayerRatingDetailDto> PlayerRatings { get; set; } = new();
}

public record ArcadePlayerMoreDetails
{
    public List<PlayerTeamResult> Teammates { get; set; } = new();
    public List<PlayerTeamResult> Opponents { get; set; } = new();
    public double AvgTeamRating { get; set; }
}