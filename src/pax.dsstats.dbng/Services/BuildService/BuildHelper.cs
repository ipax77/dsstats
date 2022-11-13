using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public record BuildHelper
{
    public int Id { get; init; }
    public string Hash { get; init; } = null!;
    public DateTime Gametime { get; init; }
    public List<KeyValuePair<int, int>> Units { get; init; } = new();
    public PlayerResult Result { get; init; }
    public int UpgradeSpending { get; init; }
    public int GasCount { get; init; }
    public int Gameloop { get; init; }
    public int Duration { get; init; }
}

