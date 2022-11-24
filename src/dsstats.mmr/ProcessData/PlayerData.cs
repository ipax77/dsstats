using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

internal record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        Deltas = new();

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

    public PlayerDeltas Deltas { get; init; }
}

internal record PlayerDeltas
{
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public double CommanderMmr { get; set; }
}