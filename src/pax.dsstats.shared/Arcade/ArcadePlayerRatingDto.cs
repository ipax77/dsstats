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
    public int ArcadePlayerId { get; set; }
    public string Name { get; set; } = null!;
    public int ProfileId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
}

public record ArcadePlayerRatingChangeDto
{
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
}

public record ArcadeReplayListDto
{
    public int ArcadeReplayId { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public int WinnerTeam { get; set; }
    public int Duration { get; set; }
}


public record ArcadeReplayDto
{
    public int ArcadeReplayId { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public int WinnerTeam { get; set; }
    public int Duration { get; set; }
    public ArcadeReplayRatingDto? ArcadeReplayRating { get; set; }
    public List<ArcadeReplayPlayerDto> ArcadeReplayPlayers { get; set; } = new();
}

public record ArcadeReplayPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int ArcadePlayerId { get; set; }
}

public record ArcadeReplayRatingDto
{
    public float ExpectationToWin { get; set; }
    public List<ArcadeReplayPlayerRatingDto> ArcadeReplayPlayerRatings { get; set; } = new();
}

public record ArcadeReplayPlayerRatingDto
{
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
}

public record ArcadePlayerDto
{
    public int ArcadePlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public List<ArcadePlayerRatingDetailDto> ArcadePlayerRatings { get; set; } = new();
}

public record ArcadePlayerRatingDetailDto
{
    public RatingType RatingType { get; set; }
    public double Rating { get; set; }
    public int Pos { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public ArcadePlayerRatingChangeDto? ArcadePlayerRatingChange { get; set; }
}