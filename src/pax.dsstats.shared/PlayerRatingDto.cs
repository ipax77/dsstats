using System.ComponentModel.DataAnnotations.Schema;

namespace pax.dsstats.shared;

public record PlayerRatingDto
{
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public PlayerRatingPlayerDto Player { get; init; } = null!;
    public PlayerRatingChangeDto? PlayerRatingChange { get; init; }
}

public record PlayerRatingPlayerDto
{
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
}

public record PlayerRatingDetailDto
{
    public RatingType RatingType { get; init; }
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public string MmrOverTime { get; set; } = "";
    public PlayerRatingPlayerDto Player { get; init; } = null!;
    public PlayerRatingChangeDto? PlayerRatingChange { get; init; }
    [NotMapped]
    public double MmrChange { get; set; }
    [NotMapped]
    public double FakeDiff { get; set; }
}

public record PlayerRatingInfoDto
{
    public RatingType RatingType { get; init; }
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
}

public record PlayerRatingChangeDto
{
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
}

public record PlayerRatingReplayCalcDto
{
    public double Rating { get; init; }
    public int Games { get; init; }
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public PlayerReplayCalcDto Player { get; init; } = null!;
}

public record PlayerReplayCalcDto
{
    public int ToonId { get; init; }
}