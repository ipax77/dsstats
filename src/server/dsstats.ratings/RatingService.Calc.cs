
using dsstats.db;
using dsstats.shared;

namespace dsstats.ratings;

public partial class RatingService
{
    private readonly MmrOptions mmrOptions = new();

    public ReplayRating? ProcessReplay(ReplayCalcDto calcDto, RatingType ratingType, PlayerRatingsStore playerRatingsStore, bool applyChanges = true)
    {
        var calcData = GetCalcData(calcDto, ratingType, playerRatingsStore, applyChanges);

        if (calcData is null)
        {
            return null;
        }

        List<ReplayPlayerRating> playerRatings = [];

        foreach (var player in calcData.WinnerTeam)
        {
            var playerRating = ProcessPlayer(player, calcData, isWinner: true);
            playerRatings.Add(playerRating);
        }

        foreach (var player in calcData.LoserTeam)
        {
            var playerRating = ProcessPlayer(player, calcData, isWinner: false);
            playerRatings.Add(playerRating);
        }

        var replayRating = new ReplayRating()
        {
            ReplayId = calcDto.ReplayId,
            RatingType = calcData.RatingType,
            LeaverType = calcData.LeaverType,
            ExpectedWinProbability = MathF.Round((float)calcData.WinnerTeamExpecationToWin, 2),
            AvgRating = (int)playerRatings.Average(a => a.RatingBefore),
            ReplayPlayerRatings = playerRatings
        };

        return replayRating;
    }

    private ReplayPlayerRating ProcessPlayer(PlayerCalcDto player,
                                                CalcData calcData,
                                                bool isWinner)
    {
        var ratingBefore = player.Rating.Rating;
        var teamConfidence = isWinner ? calcData.WinnerTeamConfidence : calcData.LoserTeamConfidence;
        var playerImpact = GetPlayerImpact(player.Rating, teamConfidence);

        var mmrDelta = 0.0;
        var consistencyDelta = 0.0;
        var confidenceDelta = 0.0;

        var exp2win = isWinner ? calcData.WinnerTeamExpecationToWin : 1.0 - calcData.WinnerTeamExpecationToWin;
        var result = isWinner ? 1 : 0;
        if (player.IsLeaver)
        {
            // a leaver is always treated as a loss, their result is 0
            mmrDelta = mmrOptions.EloK * (0 - exp2win) * playerImpact;
        }
        else
        {
            playerImpact *= calcData.LeaverImpact;
            mmrDelta = CalculateMmrDelta(calcData.WinnerTeamExpecationToWin, playerImpact, mmrOptions.EloK);
            consistencyDelta = Math.Abs(exp2win - result) < 0.50 ? 1.0 : 0.0;
            confidenceDelta = 1 - Math.Abs(exp2win - result);

            if (!isWinner)
            {
                mmrDelta *= -1;
            }
        }

        double mmrAfter = player.Rating.Rating + mmrDelta;
        double consistencyAfter = ((player.Rating.Consistency * mmrOptions.consistencyBeforePercentage)
            + (consistencyDelta * (1 - mmrOptions.consistencyBeforePercentage)));
        double confidenceAfter = ((player.Rating.Confidence * mmrOptions.confidenceBeforePercentage)
            + (confidenceDelta * (1 - mmrOptions.confidenceBeforePercentage)));

        consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
        confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);

        player.Rating.Consistency = consistencyAfter;
        player.Rating.Confidence = confidenceAfter;
        player.Rating.Games++;

        if (!player.IsLeaver)
        {
            if (isWinner)
            {
                player.Rating.Wins++;
            }
            if (player.IsMvp)
            {
                player.Rating.Mvps++;
            }
        }

        player.Rating.Rating = mmrAfter;
        player.Rating.LastGame = calcData.GameTime;
        player.Rating.CmdrCounts[player.Race] = (player.Rating.CmdrCounts.TryGetValue(player.Race, out var count) ? count : 0) + 1;

        if (calcData.CountsForChange)
        {
            player.Rating.Change += mmrDelta;
        }

        //var expectedDelta = isWinner
        //    ? CalculateExpectedDelta(exp2win, playerImpact, mmrOptions.EloK)
        //    : CalculateExpectedDelta(1.0 - exp2win, playerImpact, mmrOptions.EloK);

        var expectedDelta = (result - exp2win) * mmrOptions.EloK;

        return new()
        {
            ReplayPlayerId = player.ReplayPlayerId,
            PlayerId = player.PlayerId,
            RatingType = calcData.RatingType,
            RatingBefore = Math.Round(ratingBefore, 2),
            RatingDelta = Math.Round(mmrAfter - ratingBefore, 2),
            ExpectedDelta = expectedDelta,
            Games = player.Rating.Games,
        };
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private double GetPlayerImpact(PlayerRatingCalcDto calcRating, double teamConfidence)
    {
        double factor_consistency =
            GetCorrectedRevConsistency(1 - calcRating.Consistency, mmrOptions.consistencyImpact);
        double factor_confidence = GetCorrectedConfidenceFactor(calcRating.Confidence,
                                                                teamConfidence,
                                                                mmrOptions.distributionMult,
                                                                mmrOptions.confidenceImpact);

        return 1
            * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (mmrOptions.UseConfidence ? factor_confidence : 1.0);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency, double consistencyImpact)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
    }

    private static double GetCorrectedConfidenceFactor(double playerConfidence,
                                                   double replayConfidence,
                                                   double distributionMult,
                                                   double confidenceImpact)
    {
        double totalConfidenceFactor =
            (0.5 * (1 - GetConfidenceFactor(playerConfidence, distributionMult)))
            + (0.5 * GetConfidenceFactor(replayConfidence, distributionMult));

        return 1 + confidenceImpact * (totalConfidenceFactor - 1);
    }

    private static double GetConfidenceFactor(double confidence, double distributionMult)
    {
        double variance = ((distributionMult * 0.4) + (1 - confidence));

        return distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private CalcData? GetCalcData(ReplayCalcDto calcDto, RatingType ratingType, PlayerRatingsStore playerRatingsStore, bool applyChanges)
    {
        List<PlayerCalcDto> winnerTeam = [];
        List<PlayerCalcDto> loserTeam = [];


        foreach (var player in calcDto.Players)
        {
            player.Rating = playerRatingsStore.GetOrCreate(player.PlayerId, ratingType, mmrOptions.StartMmr, applyChanges);

            if (player.Team == calcDto.WinnerTeam)
            {
                winnerTeam.Add(player);
            }
            else
            {
                loserTeam.Add(player);
            }
        }

        if (winnerTeam.Count == 0 || loserTeam.Count == 0)
        {
            return null;
        }

        var expectationToWin = EloExpectationToWin(winnerTeam.Sum(s => s.Rating.Rating) / winnerTeam.Count,
            loserTeam.Sum(s => s.Rating.Rating) / loserTeam.Count,
            mmrOptions.Clip);

        var leaverType = calcDto.GetLeaverType();

        return new()
        {
            GameTime = calcDto.Gametime,
            RatingType = ratingType,
            LeaverType = leaverType,
            LeaverImpact = GetLeaverImpact(leaverType),
            WinnerTeam = winnerTeam,
            LoserTeam = loserTeam,
            WinnerTeamExpecationToWin = expectationToWin,
            WinnerTeamConfidence = winnerTeam.Sum(s => s.Rating.Confidence) / winnerTeam.Count,
            LoserTeamConfidence = loserTeam.Sum(s => s.Rating.Confidence) / loserTeam.Count,
            CountsForChange = DateTime.Today - calcDto.Gametime < TimeSpan.FromDays(30)
        };
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static double GetLeaverImpact(LeaverType leaverType)
    {
        return leaverType switch
        {
            LeaverType.None => 1,
            LeaverType.OneLeaver => 0.5,
            LeaverType.OneEachTeam => 0.5,
            _ => 0.25
        };
    }
}

internal sealed class MmrOptions
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

internal sealed class CalcData
{
    public DateTime GameTime { get; init; }
    public RatingType RatingType { get; init; }
    public LeaverType LeaverType { get; init; }
    public double LeaverImpact { get; init; }
    public List<PlayerCalcDto> WinnerTeam { get; init; } = [];
    public List<PlayerCalcDto> LoserTeam { get; init; } = [];
    public double WinnerTeamExpecationToWin { get; init; }
    public double WinnerTeamConfidence { get; init; }
    public double LoserTeamConfidence { get; init; }
    public bool CountsForChange { get; init; }
}

internal static class ClacDtoExtensions
{
    public static LeaverType GetLeaverType(this ReplayCalcDto calcDto)
    {
        int leavers = calcDto.Players.Count(c => c.IsLeaver);

        if (leavers == 0)
        {
            return LeaverType.None;
        }

        if (leavers == 1)
        {
            return LeaverType.OneLeaver;
        }

        if (leavers > 2)
        {
            return LeaverType.MoreThanTwo;
        }

        var leaverPlayers = calcDto.Players.Where(x => x.IsLeaver);
        var teamsCount = leaverPlayers.Select(s => s.Team).Distinct().Count();

        if (teamsCount == 1)
        {
            return LeaverType.TwoSameTeam;
        }
        else
        {
            return LeaverType.OneEachTeam;
        }
    }
}