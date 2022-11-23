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

    public double Confidence { get; set; }

    public int Duration { get; init; }
    public DateTime ReplayGameTime { get; init; }
}

internal record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        IsWinner = isWinner;
        ActualResult = isWinner ? 1 : 0;

        Players = replayPlayers.Select(p => new PlayerData(replay, p)).ToArray();
    }

    public PlayerData[] Players { get; init; }

    public bool IsWinner { get; init; }
    public int ActualResult { get; init; }

    public double Mmr { get; set; }
    public double Confidence { get; set; }
    public double CmdrComboMmr { get; set; }

    public double ExpectedResult { get; set; }
}

internal record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Commander = replayPlayer.Race;
        Duration = replayPlayer.Duration;

        IsLeaver = replayPlayer.Duration < replay.Duration - 90;
    }

    public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public Commander Commander { get; init; }
    public int Duration { get; init; }
    public bool IsLeaver { get; init; }

    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }

    public double DeltaPlayerMmr { get; set; }
    public double DeltaPlayerConsistency { get; set; }
    public double DeltaPlayerConfidence { get; set; }
    public double DeltaCommanderMmr { get; set; }
}