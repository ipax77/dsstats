
using pax.dsstats.shared;

namespace dsstats.mmr;

public record ReplayProcessData
{
    public ReplayProcessData(ReplayDsRDto replay)
    {
        Duration = replay.Duration;

        WinnerTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam));
        LoserTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam));
    }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public double Uncertainty { get; set; }

    public int Duration { get; set; }
}
