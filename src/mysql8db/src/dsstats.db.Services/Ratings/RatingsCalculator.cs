using dsstats.shared;
using dsstats.shared8;
using dsstats.shared.Calc;

namespace dsstats.db.Services.Ratings;

public static class Ratings
{
    private readonly static MmrOptions mmrOptions = new();

    public static ReplayCalcResult? ProcessReplay(ReplayCalcDto calcDto, PlayerRatingStore ratingStore)
    {
        var calcDatas = GetCalcDatas(calcDto, ratingStore);

        if (calcDatas.Count == 0)
        {
            return null;
        }

        ReplayCalcResult result = new();

        foreach (var calcData in calcDatas)
        {
            foreach (var player in calcData.WinnerTeam)
            {
                var playerRating = ProcessPlayer(player, calcData, ratingStore, isWinner: true);
                result.ReplayPlayerRatings.Add(playerRating);
            }

            foreach (var player in calcData.LoserTeam)
            {
                var playerRating = ProcessPlayer(player, calcData, ratingStore, isWinner: false);
                result.ReplayPlayerRatings.Add(playerRating);
            }

            var replayRating = new ReplayRating()
            {
                RatingType = calcData.RatingType,
                LeaverType = calcData.LeaverType,
                ExpectationToWin = (decimal)Math.Round(calcData.WinnerTeamExpecationToWin, 2),
                AvgRating = Convert.ToInt32(calcData.WinnerTeam.Concat(calcData.LoserTeam)
                    .Average(a => a.PlayerRating.Rating)),
                ReplayId = calcDto.ReplayId,
            };
            result.ReplayRatings.Add(replayRating);
        }
        return result;
    }

    private static ReplayPlayerRating ProcessPlayer(ReplayPlayerCalcDto player,
                                                CalcData calcData,
                                                PlayerRatingStore ratingStore,
                                                bool isWinner)
    {
        var teamConfidence = isWinner ? calcData.WinnerTeamConfidence : calcData.LoserTeamConfidence;
        var playerImpact = GetPlayerImpact(player.PlayerRating, teamConfidence);

        var mmrDelta = 0.0;
        var consistencyDelta = 0.0;
        var confidenceDelta = 0.0;

        var exp2win = isWinner ? calcData.WinnerTeamExpecationToWin : 1.0 - calcData.WinnerTeamExpecationToWin;
        var result = isWinner ? 1 : 0;
        if (player.IsLeaver)
        {
            mmrDelta =
             -1 * CalculateMmrDelta(isWinner ? exp2win : 1.0 - exp2win, playerImpact, mmrOptions.EloK);
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

        double mmrAfter = player.PlayerRating.Rating + mmrDelta;
        double consistencyAfter = (player.PlayerRating.Consistency * mmrOptions.consistencyBeforePercentage)
            + (consistencyDelta * (1 - mmrOptions.consistencyBeforePercentage));
        double confidenceAfter = (player.PlayerRating.Confidence * mmrOptions.confidenceBeforePercentage)
            + (confidenceDelta * (1 - mmrOptions.confidenceBeforePercentage));

        consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
        confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);

        player.PlayerRating.Consistency = consistencyAfter;
        player.PlayerRating.Confidence = confidenceAfter;
        player.PlayerRating.Games++;

        if (calcData.IsArcade)
        {
            player.PlayerRating.ArcadeGames++;
        }
        else
        {
            player.PlayerRating.DsstatsGames++;
        }

        if (!player.IsLeaver)
        {
            if (isWinner)
            {
                player.PlayerRating.Wins++;
            }
            if (player.IsMvp)
            {
                player.PlayerRating.Mvp++;
            }
        }

        ratingStore.SetCmdr(player.PlayerId, player.Race);

        var ratingChange = (float)(mmrAfter - player.PlayerRating.Rating);
        player.PlayerRating.Rating = mmrAfter;

        return new()
        {
            RatingType = calcData.RatingType,
            ReplayPlayerId = player.ReplayPlayerId,
            GamePos = player.GamePos,
            Rating = Convert.ToInt32(mmrAfter),
            Change = (decimal)Math.Round(ratingChange, 2),
            Games = player.PlayerRating.Games,
            Consistency = (decimal)Math.Round(player.PlayerRating.Consistency, 2),
            Confidence = (decimal)Math.Round(player.PlayerRating.Confidence, 2),
        };
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetPlayerImpact(PlayerRating calcRating, double teamConfidence)
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

    private static List<CalcData> GetCalcDatas(ReplayCalcDto calcDto, PlayerRatingStore ratingStore)
    {
        var ratingTypes = calcDto.GetRatingTypes();
        var leaverType = calcDto.GetLeaverType();

        List<CalcData> calcDatas = [];

        foreach (var ratingType in ratingTypes)
        {
            List<ReplayPlayerCalcDto> winnerTeam = [];
            List<ReplayPlayerCalcDto> loserTeam = [];


            foreach (var player in calcDto.ReplayPlayers)
            {
                var playerRating = ratingStore.GetPlayerRating(ratingType, player.PlayerId);

                if (player.PlayerResult == PlayerResult.Win)
                {
                    winnerTeam.Add(player with { PlayerRating = playerRating });
                }
                else
                {
                    loserTeam.Add(player with { PlayerRating = playerRating });
                }
            }

            if (winnerTeam.Count != 3 || loserTeam.Count != 3)
            {
                continue;
            }

            var expectationToWin = EloExpectationToWin(winnerTeam.Sum(s => s.PlayerRating.Rating) / winnerTeam.Count,
                loserTeam.Sum(s => s.PlayerRating.Rating) / loserTeam.Count,
                mmrOptions.Clip);


            calcDatas.Add(new()
            {
                RatingType = ratingType,
                LeaverType = leaverType,
                LeaverImpact = GetLeaverImpact(leaverType),
                WinnerTeam = winnerTeam,
                LoserTeam = loserTeam,
                WinnerTeamExpecationToWin = expectationToWin,
                WinnerTeamConfidence = winnerTeam.Sum(s => s.PlayerRating.Confidence) / winnerTeam.Count,
                LoserTeamConfidence = loserTeam.Sum(s => s.PlayerRating.Confidence) / loserTeam.Count,
                IsArcade = calcDto.IsArcade
            });
        }
        return calcDatas;
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

public record CalcData
{
    public RatingNgType RatingType { get; init; }
    public LeaverType LeaverType { get; init; }
    public double LeaverImpact { get; init; }
    public List<ReplayPlayerCalcDto> WinnerTeam { get; init; } = [];
    public List<ReplayPlayerCalcDto> LoserTeam { get; init; } = [];
    public double WinnerTeamExpecationToWin { get; init; }
    public double WinnerTeamConfidence { get; init; }
    public double LoserTeamConfidence { get; init; }
    public bool IsArcade { get; init; }
}

public record ReplayCalcResult
{
    public List<ReplayRating> ReplayRatings { get; init; } = [];
    public List<ReplayPlayerRating> ReplayPlayerRatings { get; init; } = [];
}