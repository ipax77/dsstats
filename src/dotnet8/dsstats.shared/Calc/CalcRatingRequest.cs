using System.Collections.Frozen;

namespace dsstats.shared.Calc;

public record CalcRatingRequest
{
    public RatingCalcType RatingCalcType { get; init; }
    public bool Continue { get; set; }
    public DateTime StarTime { get; set; }
    public MmrOptions MmrOptions { get; set; } = new();
    public List<CalcDto> CalcDtos { get; set; } = new();
    public int ReplayRatingAppendId { get; set; }
    public int ReplayPlayerRatingAppendId { get; set; }
    public Dictionary<int, Dictionary<PlayerId, CalcRating>> MmrIdRatings { get; set; } = new();
    public FrozenDictionary<PlayerId, bool> BannedPlayers { get; init; } = new Dictionary<PlayerId, bool>()
            {
                { new(466786, 2, 2), true }, // SabreWolf
                { new(9774911, 1, 2), true } // Baka
            }.ToFrozenDictionary();
}

public record MmrOptions
{
    public readonly double consistencyImpact = 0.50;
    public readonly double consistencyDeltaMult = 0.15;

    public readonly double confidenceImpact = 1.0;
    public readonly double consistencyBeforePercentage = 0.99;
    public readonly double confidenceBeforePercentage = 0.99;
    public readonly double distributionMult = 1.0;

    public readonly double antiSynergyPercentage = 0.50;
    public readonly double synergyPercentage;
    public readonly double ownMatchupPercentage = 1.0 / 3;
    public readonly double matesMatchupsPercentage;
    public readonly bool UseConsistency;
    public readonly bool UseConfidence;
    public readonly double StartMmr;
    public readonly double EloK;
    public readonly double Clip;
    public readonly bool ReCalc;

    public MmrOptions(bool reCalc = true, double eloK = 168, double clip = 1600)
    {
        synergyPercentage = 1 - antiSynergyPercentage;
        matesMatchupsPercentage = (1 - ownMatchupPercentage) / 2;
        UseConsistency = true;
        UseConfidence = true;
        StartMmr = 1000;
        ReCalc = reCalc;
        EloK = eloK;
        Clip = clip;
    }
}

public record CalcRatingResult
{
    public int ReplayRatingAppendId { get; set; }
    public int ReplayPlayerRatingAppendId { get; set; }
    public List<shared.Calc.ReplayRatingDto> DsstatsRatingDtos { get; set; } = new();
    public List<shared.Calc.ReplayRatingDto> Sc2ArcadeRatingDtos { get; set; } = new();
    public Dictionary<int, Dictionary<PlayerId, CalcRating>> MmrIdRatings { get; set; } = new();
    public bool Continue { get; set; }
    public bool NothingToDo { get; set; }
}