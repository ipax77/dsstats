using pax.dsstats.shared;

namespace dsstats.mmr;
using Maths;
using ProcessData;

using TeamData = ProcessData.TeamData;

public partial class MmrService
{
    public static void SetReplayData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                      MmrOptions mmrOptions)
    {
        SetTeamData(mmrIdRatings, replayData, replayData.WinnerTeamData, cmdrDic, mmrOptions);
        SetTeamData(mmrIdRatings, replayData, replayData.LoserTeamData, cmdrDic, mmrOptions);
        SetExpectationsToWin(replayData, mmrOptions);

        replayData.Confidence = Gaussian.GetPrecision(replayData.WinnerTeamData.Deviation + replayData.LoserTeamData.Deviation);
    }

    private static void SetTeamData(Dictionary<int, CalcRating> mmrIdRatings,
                                    ReplayData replayData,
                                    TeamData teamData,
                                    Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        Gaussian summedDistributions = Gaussian.ByMeanDeviation(0, 0);
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, replayData, playerData, mmrOptions);

            summedDistributions += playerData.Distribution;
        }

        teamData.Distribution = Gaussian.ByMeanDeviation(summedDistributions.Mean, summedDistributions.Deviation);
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        var (winnerPlayersExpectationToWin, match) = GaussianElo.PredictMatch(
            replayData.WinnerTeamData.Distribution,
            replayData.LoserTeamData.Distribution,
            mmrOptions.BalanceDeviationOffset);
        
        replayData.WinnerTeamData.Prediction = match;
        replayData.LoserTeamData.Prediction = Gaussian.ByMeanDeviation(-match.Mean, match.Deviation);

        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;
        replayData.LoserTeamData.ExpectedResult = (1 - winnerPlayersExpectationToWin);
    }

    private static void SetPlayerData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      PlayerData playerData,
                                      MmrOptions mmrOptions)
    {
        if (!mmrIdRatings.TryGetValue(playerData.MmrId, out var plRating))
        {
            plRating = mmrIdRatings[playerData.MmrId] = new CalcRating()
            {
                PlayerId = playerData.ReplayPlayer.Player.PlayerId,
                Mmr = mmrOptions.StartMmr,
                Deviation = mmrOptions.StandardMatchDeviation,
                Games = 0,
            };
        }

        if (mmrOptions.InjectDic.Any() 
            && mmrOptions.ReCalc 
            && !playerData.ReplayPlayer.IsUploader 
            && (replayData.RatingType == RatingType.Cmdr || replayData.RatingType == RatingType.Std))
        {
            plRating.Mmr = mmrOptions.GetInjectRating(replayData.RatingType,
                                                        replayData.ReplayDsRDto.GameTime,
                                                        new(playerData.ReplayPlayer.Player.ToonId,
                                                            playerData.ReplayPlayer.Player.RealmId,
                                                            playerData.ReplayPlayer.Player.RegionId));
        }

        if (plRating.MmrOverTime.Any())
        {
            var lastGameString = plRating.MmrOverTime.Last().Date;

            int year = int.Parse(lastGameString.Substring(0, 4));
            int month = int.Parse(lastGameString.Substring(4, 2));
            int day = int.Parse(lastGameString.Substring(6, 2));

            var lastGame = new DateTime(year, month, day);
            playerData.TimeSinceLastGame = replayData.ReplayDsRDto.GameTime - lastGame;
        }
        else
        {
            playerData.TimeSinceLastGame = new TimeSpan(0);
        }

        playerData.Mmr = plRating.Mmr;
        playerData.Deviation = plRating.Deviation;

        var decayFactor = GetDecayFactor(playerData.TimeSinceLastGame, mmrOptions);
        playerData.Deviation = Math.Min(mmrOptions.StandardPlayerDeviation, playerData.Deviation * decayFactor);
    }
}
