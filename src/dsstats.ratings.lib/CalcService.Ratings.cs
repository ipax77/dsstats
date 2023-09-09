using pax.dsstats.shared.Calc;

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
            int ratingType = GetRatingType(calcDto);

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

    public static ReplayRatingDto? ProcessReplay(CalcDto calcDto, CalcRatingRequest request)
    {
        var calcData = GetCalcData(calcDto, request);

        if (calcData is null)
        {
            return null;
        }

        List<RepPlayerRatingDto> playerRatings = new();

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

        var replayRatingDto = new ReplayRatingDto()
        {
            RatingType = calcData.RatingType,
            LeaverType = calcData.LeaverType,
            ExpectationToWin = MathF.Round((float)calcData.WinnerTeamExpecationToWin, 2),
            RepPlayerRatings = playerRatings
        };

        return replayRatingDto;
    }

    private static RepPlayerRatingDto ProcessPlayer(CalcPlayerData player,
                                                    CalcData calcData,
                                                    CalcRatingRequest request,
                                                    bool isWinner)
    {
        var playerImpact = GetPlayerImpact(player.CalcRating, calcData.WinnerTeamConfidence, request);

        var mmrDelta = 0.0;
        var consistencyDelta = 0.0;
        var confidenceDelta = 0.0;

        var exp2win = isWinner ? calcData.WinnerTeamExpecationToWin : 1.0 - calcData.WinnerTeamExpecationToWin;
        var result = isWinner ? 1 : 0;
        if (player.IsLeaver)
        {
            mmrDelta = -1 * CalculateMmrDelta(exp2win, playerImpact, request.MmrOptions.EloK);
        }
        else
        {
            mmrDelta = CalculateMmrDelta(calcData.WinnerTeamExpecationToWin, playerImpact, request.MmrOptions.EloK);
            consistencyDelta = Math.Abs(exp2win - result) < 0.50 ? 1.0 : 0.0;
            confidenceDelta = Math.Abs(exp2win - result);

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
        if (isWinner)
        {
            player.CalcRating.Wins++;
        }
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
        List<CalcPlayerData> winnerTeam = new();
        List<CalcPlayerData> loserTeam = new();

        var ratingType = GetRatingType(calcDto);

        foreach (var player in calcDto.Players)
        {
            PlayerId playerId = new() { ProfileId = player.ProfileId, RegionId = player.RegionId, RealmId = player.RealmId };
            if (!request.MmrIdRatings[ratingType].TryGetValue(playerId, out var calcRating))
            {
                calcRating = request.MmrIdRatings[ratingType][playerId] = new()
                {
                    PlayerId = playerId,
                    Mmr = request.MmrOptions.StartMmr
                };
            }

            if (player.PlayerResult == 1)
            {
                winnerTeam.Add(new(player, playerId, calcRating));
            }
            else
            {
                loserTeam.Add(new(player, playerId, calcRating));
            }
        }

        if (winnerTeam.Count != 3 || loserTeam.Count != 3)
        {
            return null;
        }

        var expectationToWin = EloExpectationToWin(winnerTeam.Sum(s => s.CalcRating.Mmr) / 3,
            loserTeam.Sum(s => s.CalcRating.Mmr) / 3,
            request.MmrOptions.Clip);

        var leaverType = GetLeaverType(calcDto);

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

    public static int GetRatingType(CalcDto calcDto)
    {
        if (calcDto.TournamentEdition && calcDto.GameMode == 3)
        {
            return 3;
        }
        else if (calcDto.TournamentEdition && calcDto.GameMode == 7)
        {
            return 4;
        }
        else if (calcDto.GameMode == 3 || calcDto.GameMode == 4)
        {
            return 1;
        }
        else if (calcDto.GameMode == 7)
        {
            return 2;
        }
        else
        {
            return 0;
        }
    }

    private static int GetLeaverType(CalcDto calcDto)
    {
        int leavers = calcDto.Players.Count(c => c.IsLeaver);

        if (leavers == 0)
        {
            return 0;
        }

        if (leavers == 1)
        {
            return 1;
        }

        if (leavers > 2)
        {
            return 4;
        }

        var leaverPlayers = calcDto.Players.Where(x => x.IsLeaver);
        var teamsCount = leaverPlayers.Select(s => s.Team).Distinct().Count();

        if (teamsCount == 1)
        {
            return 3;
        }
        else
        {
            return 2;
        }
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
    public List<CalcPlayerData> WinnerTeam { get; init; } = new();
    public List<CalcPlayerData> LoserTeam { get; init; } = new();
    public double WinnerTeamExpecationToWin { get; init; }
    public double WinnerTeamConfidence { get; init; }
    public double LoserTeamConfidence { get; init; }
}

public record CalcPlayerData
{
    public CalcPlayerData(PlayerCalcDto player, PlayerId playerId, CalcRating calcRating)
    {
        ReplayPlayerId = player.ReplayPlayerId;
        Team = player.Team;
        GamePos = player.GamePos;
        IsLeaver = player.IsLeaver;
        PlayerId = playerId;
        CalcRating = calcRating;
    }
    public int ReplayPlayerId { get; init; }
    public int Team { get; init; }
    public int GamePos { get; init; }
    public bool IsLeaver { get; init; }
    public PlayerId PlayerId { get; init; }
    public CalcRating CalcRating { get; init; }
}

public record CalcRatingRequest
{
    public MmrOptions MmrOptions { get; set; } = new();
    public List<CalcDto> CalcDtos { get; set; } = new();
    public int ReplayRatingAppendId { get; set; }
    public int ReplayPlayerRatingAppendId { get; set; }
    public Dictionary<int, Dictionary<PlayerId, CalcRating>> MmrIdRatings { get; set; } = new();
}

public record CalcRatingResult
{
    public int ReplayRatingAppendId { get; set; }
    public int ReplayPlayerRatingAppendId { get; set; }
    public List<ReplayRatingDto> DsstatsRatingDtos { get; set; } = new();
    public List<ReplayRatingDto> Sc2ArcadeRatingDtos { get; set; } = new();
    public Dictionary<int, Dictionary<PlayerId, CalcRating>> MmrIdRatings { get; set; } = new();
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
