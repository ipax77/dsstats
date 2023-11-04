using dsstats.shared;
using dsstats.shared.Calc;

namespace dsstats.ratings.lib;

public partial class CalcService
{
    public static CalcRatingResult GeneratePlayerRatings(CalcRatingRequest request)
    {
        CalcRatingResult result = new()
        {
            ReplayRatingAppendId = request.ReplayRatingAppendId,
            ReplayPlayerRatingAppendId = request.ReplayPlayerRatingAppendId
        };

        for (int i = 0; i < request.CalcDtos.Count; i++)
        {
            var calcDto = request.CalcDtos[i];
            int ratingType = calcDto.GetRatingType();

            if (ratingType == 0)
            {
                continue;
            }

            var replayRating = ProcessReplay(calcDto, request);
            if (replayRating is not null)
            {
                if (calcDto.DsstatsReplayId > 0)
                {
                    result.DsstatsRatingDtos.Add(replayRating with { ReplayId = calcDto.DsstatsReplayId });
                }

                if (calcDto.Sc2ArcadeReplayId > 0)
                {
                    result.Sc2ArcadeRatingDtos.Add(replayRating with { ReplayId = calcDto.Sc2ArcadeReplayId });
                }
            }
        }
        return result;
    }

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

        var ratingChange = MathF.Round((float)(mmrAfter - player.CalcRating.Mmr), 2);
        player.CalcRating.Mmr = mmrAfter;

        return new()
        {
            ReplayPlayerId = player.ReplayPlayerId,
            GamePos = player.GamePos,
            Rating = MathF.Round((float)mmrAfter, 2),
            RatingChange = ratingChange,
            Games = player.CalcRating.Games,
            Consistency = MathF.Round((float)player.CalcRating.Consistency, 2),
            Confidence = MathF.Round((float)player.CalcRating.Confidence, 2),
        };
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

    private static double GetLeaverImpact(int leaverType)
    {
        return leaverType switch
        {
            0 => 1,
            1 => 0.5,
            2 => 0.5,
            3 => 0.25,
            4 => 0.25,
            _ => 1
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

