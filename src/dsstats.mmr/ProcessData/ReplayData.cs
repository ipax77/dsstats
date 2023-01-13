using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record ReplayData
{
    public ReplayData(ReplayDsRDto replay)
    {
        ReplayId = replay.ReplayId;
        GameTime = replay.GameTime;
        Duration = replay.Duration;
        Maxleaver = replay.Maxleaver;
        Maxkillsum = replay.Maxkillsum;
        LeaverType = MmrService.GetLeaverType(replay);
        LeaverImpact = LeaverType switch
        {
            LeaverType.OneLeaver => 0.5,
            LeaverType.OneEachTeam => MmrService.GetLeaverImpactForOneEachTeam(replay),
            LeaverType.TwoSameTeam => 0.25,
            LeaverType.MoreThanTwo => 0.25,
            _ => 1
        };

        WinnerTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam), true);
        LoserTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam), false);

        IsStd = WinnerTeamData.Players.All(a => (int)a.Race <= 3);
        IsInvalid = WinnerTeamData.Players.Any(a => (int)a.Race <= 3 || (int)a.OppRace <= 3) || LoserTeamData.Players.Any(a => (int)a.Race <= 3 || (int)a.OppRace <= 3);
    }
    public ReplayData(DateTime gameTime,
                      int duration,
                      int maxLeaver,
                      int maxkillsum,
                      double confidence,
                      LeaverType leaverType,
                      TeamData winnerTeamData,
                      TeamData loserTeamData)
    {
        GameTime = gameTime;
        Duration = duration;
        Maxleaver = maxLeaver;
        Maxkillsum = maxkillsum;
        Confidence = confidence;
        WinnerTeamData = winnerTeamData;
        LoserTeamData = loserTeamData;

        LeaverType = leaverType;
        LeaverImpact = 1;

        IsStd = WinnerTeamData.Players.All(a => (int)a.Race <= 3) && LoserTeamData.Players.All(a => (int)a.Race <= 3);
        IsInvalid = WinnerTeamData.Players.Any(a => (int)a.Race <= 3 || (int)a.OppRace <= 3) || LoserTeamData.Players.Any(a => (int)a.Race <= 3 || (int)a.OppRace <= 3);
    }

    public LeaverType LeaverType { get; init; }
    public double LeaverImpact { get; init; }
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int Maxleaver { get; init; }
    public int Maxkillsum { get; init; }
    public bool IsStd { get; init; }
    public bool IsInvalid { get; init; }
    public int ReplayId { get; init; }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double Confidence { get; set; }

    public bool CorrectPrediction => (WinnerTeamData.ExpectedResult > 0.50);
}