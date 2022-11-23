
using pax.dsstats.shared;

namespace dsstats.mmr;

internal record ReplayData
{
    public ReplayData(ReplayDsRDto replay)
    {
        ReplayGameTime = replay.GameTime;
        Duration = replay.Duration;

        WinnerTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam), true);
        LoserTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam), false);
    }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }
    public double Confidence { get; set; }
    public int Duration { get; set; }
    public DateTime ReplayGameTime { get; init; }
}
