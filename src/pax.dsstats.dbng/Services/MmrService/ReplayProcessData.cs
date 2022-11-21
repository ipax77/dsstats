using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

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

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public double Confidence { get; set; }

    public int Duration { get; set; }
    public DateTime ReplayGameTime { get; init; }
}

internal record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        Players = replayPlayers.Select(p => new PlayerData(replay, p)).ToArray();
        IsWinner = isWinner;

        ActualResult = isWinner ? 1 : 0;
    }

    public PlayerData[] Players { get; init; }

    public bool IsWinner { get; init; }
    public int ActualResult { get; init; }

    public double ExpectedResult { get; set; }

    public double PlayersAvgMmr { get; set; }
    public double CmdrComboMmr { get; set; }
    public double Confidence { get; set; }
    
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
    public double Confidence { get; set; }

    public double PlayerMmrDelta { get; set; }
    public double PlayerConsistencyDelta { get; set; }
    public double PlayerConfidenceDelta { get; set; }
    public double CommanderMmrDelta { get; set; }
}