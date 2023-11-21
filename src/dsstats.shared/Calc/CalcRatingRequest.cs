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
                { new(9774911, 1, 2), true }, // Baka
                { new(3768192, 1, 1), true } // Henz
            }.ToFrozenDictionary();
    public FrozenDictionary<PlayerId, bool> SoftBannedPlayers { get; init; } = new Dictionary<PlayerId, bool>()
            {
                { new PlayerId(4408073, 1, 2), true}, // MemoriLuvLow
                { new PlayerId(10195430, 1, 2), true }, //HenyaMyWaifu
                { new PlayerId(5310262, 1, 1), true }, //SunayStinks
                { new PlayerId(10392393, 1, 2), true }, //EnTaroGura
                { new PlayerId(12788234, 1, 1), true }, //Amemiya
                { new PlayerId(9846569, 1, 2), true }, //Zergling
                { new PlayerId(9207965, 1, 2), true }, //kun
                { new PlayerId(12967800, 1, 1), true }, //AAAAAAAAAAAA
                { new PlayerId(1608587, 2, 3), true }, //Amemiya
                { new PlayerId(10570273, 1, 2), true }, //holymackerel
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