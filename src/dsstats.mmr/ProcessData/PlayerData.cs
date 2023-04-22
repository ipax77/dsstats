using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Deltas = new();

        MmrId = MmrService.GetMmrId(replayPlayer.Player);

        IsLeaver = replayPlayer.Duration < replay.Duration - 90
            || (replayPlayer.IsUploader && replay.ResultCorrected)
            // || replayPlayer.PlayerResult == PlayerResult.None
            ;
    }

    public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public int MmrId { get; init; }
    public bool IsLeaver { get; init; }

    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }

    public PlayerDeltas Deltas { get; init; }
}

public record PlayerDeltas
{
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public double CommanderMmr { get; set; }
}