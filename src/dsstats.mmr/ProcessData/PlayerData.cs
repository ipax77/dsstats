using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;
using Maths;

public record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Deltas = new();

        MmrId = MmrService.GetMmrId(replayPlayer.Player);

        IsLeaver = replayPlayer.Duration < replay.Duration - 90 || (replayPlayer.IsUploader && replay.ResultCorrected);
    }

    public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public int MmrId { get; init; }
    public bool IsLeaver { get; init; }

    public TimeSpan TimeSinceLastGame { get; set; }

    public double Mmr { get; set; }
    public double Deviation { get; set; }
    public double Confidence => Gaussian.GetPrecision(Deviation);

    public Gaussian Distribution => Gaussian.ByMeanDeviation(Mmr, Deviation);

    public PlayerDeltas Deltas { get; init; }
}

public record PlayerDeltas
{
    public double Mmr { get; set; }
    public double Deviation { get; set; }
    public double CommanderMmr { get; set; }
}