using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

internal record ReplayProcessData
{
    public ReplayProcessData(ReplayDsRDto replay)
    {
        ReplayGameTime = replay.GameTime;
        Duration = replay.Duration;

        WinnerTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam), true);
        LoserTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam), false);
    }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public double Uncertainty { get; set; }

    public int Duration { get; set; }
    public DateTime ReplayGameTime { get; init; }
}

internal record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        Players = replayPlayers.Select(x => new PlayerData(replay, x)).ToArray();
        this.IsWinner = isWinner;
    }

    public PlayerData[] Players { get; init; }
    
    public double PlayersMeanMmr { get; set; }
    public double CmdrComboMmr { get; set; }

    public double UncertaintyDelta { get; set; }

    public bool IsWinner { get; init; }
}

internal record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Commander = ReplayPlayer.Race;

        IsLeaver = Duration < replay.Duration - 90;
    }

    public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public int Duration => ReplayPlayer.Duration;
    public Commander Commander { get; init; }
    public bool IsLeaver { get; init; }

    public double PlayerMmrDelta { get; set; }
    public double PlayerConsistencyDelta { get; set; }
    public double CommanderMmrDelta { get; set; }
}