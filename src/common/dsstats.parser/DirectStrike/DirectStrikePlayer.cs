using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Sc2DirectStrike.Parser;

public sealed class DirectStrikePlayer
{
    public double APM { get; set; }
    public TimeSpan Duration { get; set; }
    public int DurationGameloop { get; set; }
    public int GamePos { get; set; }
    public int TeamId { get; set; }
    public int SlotId { get; set; }
    public int? ScanCount { get; set; }
    public Commander Commander { get; set; }
    public PlayerResult Result { get; set; }
    public Race SelectedRace { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Clan { get; set; }
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Refinery timings are intentionally exposed as an array by the parser API contract.")]
    public TimeSpan[] RefineryTimes { get; set; } = [];
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Tier upgrade timings are intentionally exposed as an array by the parser API contract.")]
    public TimeSpan[] TierUpgrades { get; set; } = [];
    public IReadOnlyDictionary<string, TimeSpan> Upgrades { get; set; } = new ReadOnlyDictionary<string, TimeSpan>(new Dictionary<string, TimeSpan>(StringComparer.Ordinal));
    public int Id { get; set; }
    public int Region { get; set; }
    public int Realm { get; set; }
    public ReadOnlyCollection<string> BuildUnitNames { get; set; } = [];
    public ReadOnlyCollection<DirectStrikePlayerSpawn> Spawns { get; set; } = [];
    public ReadOnlyCollection<DirectStrikePlayerStats> Stats { get; set; } = [];
    public ReadOnlyCollection<DirectStrikeBuildUnitModification> BuildUnitModifications { get; set; } =
        BuildUnitModificationCollections.EmptyModifications;
    public ReadOnlyCollection<BuildUnitModificationAnalysis> BuildUnitModificationAnalysis { get; set; } =
        BuildUnitModificationCollections.EmptyAnalysis;
}
