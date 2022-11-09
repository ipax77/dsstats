using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

internal record TeamData
{
    public TeamData(IEnumerable<ReplayPlayerDsRDto> replayPlayers)
    {
        Players = replayPlayers.ToArray();
        Cmdrs = Players.Select(x => x.Race).ToArray();

        PlayersMmrDelta = new double[Players.Length];
        PlayersConsistencyDelta = new double[Players.Length];
        CmdrMmrDelta = new double[Players.Length];
    }

    public ReplayPlayerDsRDto[] Players { get; init; }
    public Commander[] Cmdrs { get; init; }

    public double CmdrComboMmr { get; set; }
    public double PlayersMmr { get; set; }

    public double WinnerPlayersExpectationToWin { get; set; }
    public double WinnerCmdrExpectationToWin { get; set; }

    public double[] PlayersMmrDelta { get; init; }
    public double[] PlayersConsistencyDelta { get; init; }
    public double[] CmdrMmrDelta { get; init; }
}