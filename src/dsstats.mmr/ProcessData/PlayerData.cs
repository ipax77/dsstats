using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        ReplayPlayer = replayPlayer;
        Deltas = new();

        //ReplayPlayer = replayPlayer;
        MmrId = MmrService.GetMmrId(replayPlayer.Player);

        IsLeaver = replayPlayer.Duration < replay.Duration - 90 || (replayPlayer.IsUploader && replay.ResultCorrected);
    }
    public PlayerData(Commander race,
                      Commander oppRace,
                      int duration,
                      bool isLeaver,
                      double mmr,
                      double consistency,
                      double confidence,
                      int mmrId,
                      int gamePos,
                      int replayPlayerId,
                      int playerId,
                      bool isUploader,
                      int kills,
                      PlayerResult playerResult)
    {
        Deltas = new();

        Race = race;
        OppRace = oppRace;
        Duration = duration;
        IsLeaver = isLeaver;
        Mmr = mmr;
        Consistency = consistency;
        Confidence = confidence;

        MmrId = mmrId;
        GamePos = gamePos;
        ReplayPlayerId = replayPlayerId;
        PlayerId = playerId;
        IsUploader = isUploader;
        Kills = kills;
        PlayerResult = playerResult;
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