
namespace pax.dsstats.shared.Raven;

public record RavenPlayerDto
{
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public RavenRatingDto Rating { get; init; } = new();
}

public record RavenRatingDto
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public Commander Main { get; set; }
    public double MainPercentage { get; set; }
    public double Mmr { get; set; }
}

public record MmrChange
{
    public string Hash { get; set; } = null!;
    public List<PlChange> Changes { get; set; } = new();
}

public record RavenMmrChange
{
    public string Id { get; set; } = null!;
    public List<PlChange> Changes { get; set; } = new();
}

public record PlChange
{
    public int Pos { get; set; }
    public double Change { get; set; }
}

public record RavenPlayer
{
    public string Id { get; set; } = null!;
    public int ToonId { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = null!;
    public int RegionId { get; set; }
    public bool IsUploader { get; set; }
}

public record RavenRating
{
    public string Id { get; set; } = null!;
    public int ToonId { get; set; }
    public RatingType Type { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public Commander Main { get; set; }
    public double MainPercentage { get; set; }
    public double Mmr { get; set; }
    public string? MmrOverTime { get; set; } = null!;
    public double Consistency { get; set; }
    public double Uncertainty { get; set; }
}

public record CalcRating
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }

    public double Mmr { get; set; }
    public List<TimeRating> MmrOverTime { get; set; } = new();
    public double Consistency { get; set; }
    public double Uncertainty { get; set; }
    public bool IsUploader { get; set; }
    public Dictionary<Commander, int> CmdrCounts { get; set; } = new();
}

public record TimeRating
{
    public string Date { get; set; } = "";
    public double Mmr { get; set; }
}

public record UpdateResult
{
    public int Total { get; set; }
    public int Update { get; set; }
    public int New { get; set; }
}

public enum RatingType
{
    None = 0,
    Cmdr = 1,
    Std = 2
}