
namespace pax.dsstats.shared;

public class PlayerRating
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public Commander Main { get; set; }
    public float MainPercentage { get; set; }

    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }

    public int MmrGames { get; set; }
    public float Mmr { get; set; }
    public string? MmrOverTime { get; set; }
    public float Consistency { get; set; }
    public float Uncertainty { get; set; }
    public float Winrate => Games == 0 ? 0 : MathF.Round(Wins * 100.0f / Games, 2);
    public float Mvprate => Games == 0 ? 0 : MathF.Round(Mvp * 100.0f / Games, 2);
}

public class CalcRating
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }

    public float Mmr { get; set; }
    public List<TimeRating> MmrOverTime { get; set; } = new();
    public float Consistency { get; set; }
    public float Uncertainty { get; set; }
}

public record PlayerInfo
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public List<Rating> Ratings { get; set; } = new();
}

public record Rating
{
    public int PlayerId { get; init; }
    public RatingType Type { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public float Mmr { get; set; }
    public List<TimeRating> MmrOverTime { get; set; } = new();
    public float Consistency { get; set; }
    public float Uncertainty { get; set; }
    public float Winrate => Games == 0 ? 0 : MathF.Round(Wins * 100.0f / Games, 2);
    public float Mvprate => Games == 0 ? 0 : MathF.Round(Mvp * 100.0f / Games, 2);
}

public record TimeRating
{
    public DateOnly Date { get; set; }
    public float Mmr { get; set; }
}

public enum RatingType
{
    None = 0,
    Cmdr = 1,
    Std = 2
}

public record MmrOptions
{
    public bool UseCommanderMmr {get; set; } = false;
    public bool UseConsistency{get; set; } = true;
    public bool UseUncertanity{get; set; } = true;
    public bool UseFactorToTeamMates {get; set; } = false;
}

public static class PlayerRatingExtensions
{
    public static void SetMmr(this CalcRating calcRating, float mmr, DateTime gametime)
    {
        calcRating.Mmr = mmr;
        calcRating.MmrOverTime.Add(new()
        {
            Date = DateOnly.FromDateTime(gametime),
            Mmr = mmr
        });
    }
}

