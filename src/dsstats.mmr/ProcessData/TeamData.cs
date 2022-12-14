using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        IsWinner = isWinner;
        ActualResult = isWinner ? 1 : 0;

        Players = replayPlayers.Select(p => new PlayerData(replay, p)).ToArray();
    }
    public TeamData(bool isWinner,
                    double mmr,
                    double confidence,
                    double cmdrComboMmr,
                    double expectedResult,
                    PlayerData[] players)
    {
        IsWinner = isWinner;
        ActualResult = isWinner ? 1 : 0;
        Mmr = mmr;
        Confidence = confidence;
        CmdrComboMmr = cmdrComboMmr;
        ExpectedResult = expectedResult;
        Players = players;
    }

    public PlayerData[] Players { get; init; }

    public bool IsWinner { get; init; }
    public int ActualResult { get; init; }

    public double Mmr { get; set; }
    public double Confidence { get; set; }
    public double CmdrComboMmr { get; set; }

    public double ExpectedResult { get; set; }
}
