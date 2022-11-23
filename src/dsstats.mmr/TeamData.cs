
using pax.dsstats.shared;

namespace dsstats.mmr;

internal record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers, bool isWinner)
    {
        IsWinner = isWinner;
        ActualResult = isWinner ? 1 : 0;

        Players = replayPlayers.Select(p => new PlayerData(replay, p)).ToArray();
        Std = replayPlayers.Any(a => (int)a.Race <= 3);
    }
    public bool Std { get; init; }
    public PlayerData[] Players { get; init; }

    public bool IsWinner { get; init; }
    public int ActualResult { get; init; }

    public double Mmr { get; set; }
    public double Confidence { get; set; }
    public double CmdrComboMmr { get; set; }

    public double ExpectedResult { get; set; }
}
