using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

internal record ReplayProcessData
{
    public ReplayProcessData(ReplayDsRDto replay)
    {
        WinnerTeamData = new(replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam));
        LoserTeamData = new(replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam));

        ReplayGameTime = replay.GameTime;
    }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public DateTime ReplayGameTime { get; init; }
}