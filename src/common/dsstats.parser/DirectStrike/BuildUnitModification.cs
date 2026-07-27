using System.Collections.ObjectModel;

namespace Sc2DirectStrike.Parser;

public enum BuildUnitModificationType
{
    None = 0,
    Biomass = 1,
    PowerOverwhelming = 2,
    GuardianShell = 3,
    OrbitalStrikeBeacon = 4,
    DarkPylon = 5,
}

public enum BuildUnitModificationAnalysisStatus
{
    None = 0,
    Analyzed = 1,
    RequiredEventsUnavailable = 2,
    UnsupportedDataBuild = 3,
}

/// <summary>A detected build-unit modification event.</summary>
/// <param name="Type">Modification type.</param>
/// <param name="Gameloop">Event gameloop.</param>
/// <param name="TargetUnitTag">Target tracker unit tag.</param>
/// <param name="TargetUnitName">
/// Canonical parser unit identity with commander affixes and lightweight/starlight suffixes removed.
/// </param>
/// <param name="Amount">Modification amount represented by the event.</param>
/// <param name="SourceUnitTag">Optional source tracker unit tag.</param>
/// <param name="SourceUnitName">
/// Canonical parser unit identity with commander affixes and lightweight/starlight suffixes removed.
/// </param>
public sealed record DirectStrikeBuildUnitModification(
    BuildUnitModificationType Type,
    int Gameloop,
    int TargetUnitTag,
    string TargetUnitName,
    int Amount,
    int? SourceUnitTag,
    string? SourceUnitName);

public sealed record BuildUnitModificationAnalysis(
    BuildUnitModificationType Type,
    BuildUnitModificationAnalysisStatus Status);

/// <summary>A cumulative modification count for a canonical unit identity.</summary>
/// <param name="Type">Modification type.</param>
/// <param name="TargetUnitName">
/// Canonical parser unit identity with commander affixes and lightweight/starlight suffixes removed.
/// </param>
/// <param name="Count">Number of modified units.</param>
public sealed record BuildUnitModificationCountDto(
    BuildUnitModificationType Type,
    string TargetUnitName,
    int Count);

internal sealed class DirectStrikeBuildAreaUnit(
    DirectStrikePlayerContext context,
    int unitTagIndex,
    int unitTagRecycle,
    int unitTag,
    string rawUnitName,
    string canonicalUnitName,
    string displayUnitName,
    string? creatorAbilityName,
    int gameloop)
{
    public DirectStrikePlayerContext Context { get; } = context;
    public int UnitTagIndex { get; } = unitTagIndex;
    public int UnitTagRecycle { get; } = unitTagRecycle;
    public int UnitTag { get; } = unitTag;
    public string RawUnitName { get; } = rawUnitName;
    public string CanonicalUnitName { get; } = canonicalUnitName;
    public string DisplayUnitName { get; } = displayUnitName;
    public string? CreatorAbilityName { get; } = creatorAbilityName;
    public int Gameloop { get; } = gameloop;
    public bool IsPlaced => CreatorAbilityName?.EndsWith("Place", StringComparison.Ordinal) is true;
}

internal sealed record PendingBiomassItem(int Gameloop, DirectStrikeBuildAreaUnit Target);

internal sealed record DirectStrikePlayerContext(DirectStrikePlayer Player, int DetailsIndex, int? MetadataPlayerId)
{
    public List<DirectStrikePlayerRefinery> Refineries { get; } = [];
    public Dictionary<int, DirectStrikeBuildAreaUnit> BuildAreaUnitsByTag { get; } = [];
    public Dictionary<string, List<DirectStrikeBuildAreaUnit>> BuildAreaUnitsByDisplayName { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string>? CanonicalBuildUnitNamesByRawName { get; set; }
    public List<PendingBiomassItem> PendingBiomassItems { get; } = [];
    public List<DirectStrikeBuildUnitModification> BuildUnitModifications { get; } = [];
    public HashSet<(BuildUnitModificationType Type, int TargetUnitTag)> ModifiedTargets { get; } = [];
    public BuildUnitModificationType BuildUnitModificationType { get; set; }
    public BuildUnitModificationAnalysisStatus BuildUnitModificationAnalysisStatus { get; set; }
}

internal sealed class DirectStrikePlayerRefinery
{
    public int UnitTagIndex { get; set; }
    public int UnitTagRecycle { get; set; }
    public int Gameloop { get; set; }
    public bool Taken { get; set; }
}

internal static class BuildUnitModificationCollections
{
    public static readonly ReadOnlyCollection<DirectStrikeBuildUnitModification> EmptyModifications =
        Array.AsReadOnly(Array.Empty<DirectStrikeBuildUnitModification>());

    public static readonly ReadOnlyCollection<BuildUnitModificationAnalysis> EmptyAnalysis =
        Array.AsReadOnly(Array.Empty<BuildUnitModificationAnalysis>());

    public static readonly IReadOnlyCollection<BuildUnitModificationCountDto> EmptyCounts =
        [];
}
