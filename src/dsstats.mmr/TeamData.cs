
using pax.dsstats.shared;

namespace dsstats.mmr;

public record TeamData
{
    public TeamData(ReplayDsRDto replay, IEnumerable<ReplayPlayerDsRDto> replayPlayers)
    {
        Players = replayPlayers.Select(x => new PlayerData(replay, x)).ToArray();
        Std = replay.ReplayPlayers.Any(a => (int)a.Race <= 3);
    }
    public bool Std { get; init; }
    public PlayerData[] Players { get; init; }

    public double PlayersMeanMmr { get; set; }
    public double CmdrComboMmr { get; set; }

    public double UncertaintyDelta { get; set; }
}
