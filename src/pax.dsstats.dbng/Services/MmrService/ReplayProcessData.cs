using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

internal record ReplayProcessData
{
    public ReplayProcessData(ReplayDsRDto replay)
    {
        ReplayGameTime = replay.GameTime;
        Duration = replay.Duration;

        WinnerTeamData = new(replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam));
        LoserTeamData = new(replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam));
    }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public int Duration { get; set; }
    public DateTime ReplayGameTime { get; init; }
}

internal record TeamData
{
    public TeamData(IEnumerable<ReplayPlayerDsRDto> replayPlayers)
    {
        Players = replayPlayers.Select(x => new PlayerData(x)).ToArray();
    }

    public PlayerData[] Players { get; init; }
    
    public double PlayersMeanMmr { get; set; }
    public double CmdrComboMmr { get; set; }
}

internal record PlayerData
{
    public PlayerData(ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Commander = ReplayPlayer.Race;
    }

    public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public Commander Commander { get; init; }
    
    public double PlayerMmrDelta { get; set; }
    public double PlayerConsistencyDelta { get; set; }
    public double CommanderMmrDelta { get; set; }
}