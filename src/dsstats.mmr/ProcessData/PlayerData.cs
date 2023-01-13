using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record PlayerData
{
    public PlayerData(ReplayDsRDto replay, ReplayPlayerDsRDto replayPlayer)
    {
        Deltas = new();

        //ReplayPlayer = replayPlayer;
        MmrId = MmrService.GetMmrId(replayPlayer.Player);
        GamePos = replayPlayer.GamePos;
        ReplayPlayerId = replayPlayer.ReplayPlayerId;
        PlayerId = replayPlayer.Player.PlayerId;
        IsUploader = replayPlayer.IsUploader;
        Kills = replayPlayer.Kills;
        PlayerResult = replayPlayer.PlayerResult;

        Race = replayPlayer.Race;
        OppRace = replayPlayer.OppRace;
        Duration = replayPlayer.Duration;

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

    //public ReplayPlayerDsRDto ReplayPlayer { get; init; }
    public int MmrId { get; init; }
    public int GamePos { get; init; }
    public int ReplayPlayerId { get; init; }
    public int PlayerId { get; init; }
    public bool IsUploader { get; init; }
    public int Kills { get; init; }
    public PlayerResult PlayerResult { get; init; }

    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Duration { get; init; }
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