using dsstats.shared;
using dsstats.shared.Calc;

namespace dsstats.ratings.lib;

public static class Ratings
{
    public static shared.Calc.ReplayRatingDto? ProcessReplay(CalcDto calcDto, CalcRatingRequest request)
    {
        var calcData = GetCalcData(calcDto, request);

        if (calcData is null)
        {
            return null;
        }

        List<shared.Calc.RepPlayerRatingDto> playerRatings = new();

        foreach (var player in calcData.WinnerTeam)
        {
            var playerRating = ProcessPlayer(player, calcData, request, isWinner: true);
            playerRatings.Add(playerRating);
        }

        foreach (var player in calcData.LoserTeam)
        {
            var playerRating = ProcessPlayer(player, calcData, request, isWinner: false);
            playerRatings.Add(playerRating);
        }

        var replayRatingDto = new shared.Calc.ReplayRatingDto()
        {
            RatingType = calcData.RatingType,
            LeaverType = calcData.LeaverType,
            ExpectationToWin = MathF.Round((float)calcData.WinnerTeamExpecationToWin, 2),
            ReplayId = calcDto.ReplayId,
            RepPlayerRatings = playerRatings
        };

        return replayRatingDto;
    }

    private static shared.Calc.RepPlayerRatingDto ProcessPlayer(PlayerCalcDto player,
                                                CalcData calcData,
                                                CalcRatingRequest request,
                                                bool isWinner)
    {
        var teamConfidence = isWinner ? calcData.WinnerTeamConfidence : calcData.LoserTeamConfidence;
        var playerImpact = GetPlayerImpact(player.CalcRating, teamConfidence, request);

        var mmrDelta = 0.0;
        var consistencyDelta = 0.0;
        var confidenceDelta = 0.0;

        var exp2win = isWinner ? calcData.WinnerTeamExpecationToWin : 1.0 - calcData.WinnerTeamExpecationToWin;
        var result = isWinner ? 1 : 0;
        if (player.IsLeaver)
        {
            mmrDelta =
             -1 * CalculateMmrDelta(isWinner ? exp2win : 1.0 - exp2win, playerImpact, request.MmrOptions.EloK);
        }
        else
        {
            playerImpact *= calcData.LeaverImpact;
            mmrDelta = CalculateMmrDelta(calcData.WinnerTeamExpecationToWin, playerImpact, request.MmrOptions.EloK);
            consistencyDelta = Math.Abs(exp2win - result) < 0.50 ? 1.0 : 0.0;
            confidenceDelta = 1 - Math.Abs(exp2win - result);

            if (!isWinner)
            {
                mmrDelta *= -1;
            }
        }

        double mmrAfter = player.CalcRating.Mmr + mmrDelta;
        double consistencyAfter = ((player.CalcRating.Consistency * request.MmrOptions.consistencyBeforePercentage)
            + (consistencyDelta * (1 - request.MmrOptions.consistencyBeforePercentage)));
        double confidenceAfter = ((player.CalcRating.Confidence * request.MmrOptions.confidenceBeforePercentage)
            + (confidenceDelta * (1 - request.MmrOptions.confidenceBeforePercentage)));

        consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
        confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);

        player.CalcRating.Consistency = consistencyAfter;
        player.CalcRating.Confidence = confidenceAfter;
        player.CalcRating.Games++;

        if (!player.IsLeaver)
        {
            if (isWinner)
            {
                player.CalcRating.Wins++;
            }
            if (player.IsMvp)
            {
                player.CalcRating.Mvps++;
            }
        }

        SetCmdr(player.CalcRating, player.Race);

        var ratingChange = (float)(mmrAfter - player.CalcRating.Mmr);
        player.CalcRating.Mmr = mmrAfter;

        return new()
        {
            ReplayPlayerId = player.ReplayPlayerId,
            GamePos = player.GamePos,
            Rating = (float)mmrAfter,
            RatingChange = ratingChange,
            Games = player.CalcRating.Games,
            Consistency = (float)player.CalcRating.Consistency,
            Confidence = (float)player.CalcRating.Confidence,
        };
    }

    private static void SetCmdr(CalcRating calcRating, Commander cmdr)
    {
        if (calcRating.CmdrCounts.TryGetValue(cmdr, out int count))
        {
            calcRating.CmdrCounts[cmdr] = count + 1;
        }
        else
        {
            calcRating.CmdrCounts[cmdr] = 1;
        }
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetPlayerImpact(CalcRating calcRating, double teamConfidence, CalcRatingRequest request)
    {
        double factor_consistency =
            GetCorrectedRevConsistency(1 - calcRating.Consistency, request.MmrOptions.consistencyImpact);
        double factor_confidence = GetCorrectedConfidenceFactor(calcRating.Confidence,
                                                                teamConfidence,
                                                                request.MmrOptions.distributionMult,
                                                                request.MmrOptions.confidenceImpact);

        return 1
            * (request.MmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (request.MmrOptions.UseConfidence ? factor_confidence : 1.0);
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

    private static CalcData? GetCalcData(CalcDto calcDto, CalcRatingRequest request)
    {
        List<PlayerCalcDto> winnerTeam = new();
        List<PlayerCalcDto> loserTeam = new();

        var ratingType = calcDto.GetRatingType();

        foreach (var player in calcDto.Players)
        {
            bool isBanned = false;
            if (request.RatingCalcType == RatingCalcType.Dsstats
                && request.BannedPlayers.ContainsKey(player.PlayerId))
            {
                isBanned = true;
            }

            if (!request.MmrIdRatings[ratingType].TryGetValue(player.PlayerId, out var calcRating)
                || isBanned)
            {
                calcRating = request.MmrIdRatings[ratingType][player.PlayerId] = new()
                {
                    PlayerId = player.PlayerId,
                    Mmr = request.MmrOptions.StartMmr,
                    IsUploader = player.IsUploader
                };
            }
            player.CalcRating = calcRating;

            if (player.PlayerResult == 1)
            {
                winnerTeam.Add(player);
            }
            else
            {
                loserTeam.Add(player);
            }
        }

        if (winnerTeam.Count != 3 || loserTeam.Count != 3)
        {
            return null;
        }

        var expectationToWin = EloExpectationToWin(winnerTeam.Sum(s => s.CalcRating.Mmr) / 3,
            loserTeam.Sum(s => s.CalcRating.Mmr) / 3,
            request.MmrOptions.Clip);

        var leaverType = calcDto.GetLeaverTyp();

        return new()
        {
            RatingType = ratingType,
            LeaverType = leaverType,
            LeaverImpact = GetLeaverImpact(leaverType),
            WinnerTeam = winnerTeam,
            LoserTeam = loserTeam,
            WinnerTeamExpecationToWin = expectationToWin,
            WinnerTeamConfidence = winnerTeam.Sum(s => s.CalcRating.Confidence) / 3,
            LoserTeamConfidence = loserTeam.Sum(s => s.CalcRating.Confidence) / 3,
        };
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static double GetLeaverImpact(int leaverType)
    {
        return leaverType switch
        {
            0 => 1,
            1 => 0.5,
            2 => 0.5,
            _ => 0.25
        };
    }
}

public record CalcData
{
    public int RatingType { get; init; }
    public int LeaverType { get; init; }
    public double LeaverImpact { get; init; }
    public List<PlayerCalcDto> WinnerTeam { get; init; } = new();
    public List<PlayerCalcDto> LoserTeam { get; init; } = new();
    public double WinnerTeamExpecationToWin { get; init; }
    public double WinnerTeamConfidence { get; init; }
    public double LoserTeamConfidence { get; init; }
}
