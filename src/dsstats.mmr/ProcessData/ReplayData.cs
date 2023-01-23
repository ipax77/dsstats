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

        RatingType = MmrService.GetRatingType(replay);
    }

    public LeaverType LeaverType { get; init; }
    public RatingType RatingType { get; init; }
    public double LeaverImpact { get; init; }
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int Maxleaver { get; init; }
    public int Maxkillsum { get; init; }
    public int ReplayId { get; init; }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double Confidence { get; set; }

    public bool CorrectPrediction => (WinnerTeamData.ExpectedResult > 0.50);
}