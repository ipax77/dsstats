
using pax.dsstats.shared;

namespace dsstats.mmr;

public record PlayerData
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