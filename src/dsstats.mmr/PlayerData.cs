
using pax.dsstats.shared;

namespace dsstats.mmr;

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