using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private const int GuardianShellMinimumDataBuild = 96883;
    private const int OrbitalStrikeBeaconMinimumDataBuild = 97425;
    private const int DarkPylonMinimumDataBuild = 97425;
    private const int BiomassMinimumDataBuild = 97563;
    private const int PowerOverwhelmingMinimumDataBuild = 97563;
    private const int BiomassAbilityLink = 1124;
    private const int OrbitalStrikeBeaconAbilityLink = 2014;

    private static void InitializeBuildUnitModificationAnalysis(
        Sc2Replay replay,
        DirectStrikePlayerContext[] playerContexts)
    {
        int dataBuild = GetReplayDataBuild(replay);
        bool trackerEventsAvailable = replay.TrackerEvents is not null;
        bool gameEventsAvailable = replay.GameEvents is not null;

        foreach (DirectStrikePlayerContext context in playerContexts)
        {
            BuildUnitModificationType type = context.Player.Commander switch
            {
                Commander.Abathur => BuildUnitModificationType.Biomass,
                Commander.Alarak => BuildUnitModificationType.PowerOverwhelming,
                Commander.Artanis => BuildUnitModificationType.GuardianShell,
                Commander.Karax => BuildUnitModificationType.OrbitalStrikeBeacon,
                Commander.Vorazun => BuildUnitModificationType.DarkPylon,
                _ => BuildUnitModificationType.None,
            };
            context.BuildUnitModificationType = type;
            if (type == BuildUnitModificationType.None)
            {
                continue;
            }

            if (!IsSupportedBuildUnitModification(type, dataBuild))
            {
                context.BuildUnitModificationAnalysisStatus = BuildUnitModificationAnalysisStatus.UnsupportedDataBuild;
                continue;
            }

            bool requiresGameEvents = type is BuildUnitModificationType.Biomass
                or BuildUnitModificationType.GuardianShell
                or BuildUnitModificationType.OrbitalStrikeBeacon;
            context.BuildUnitModificationAnalysisStatus =
                trackerEventsAvailable && (!requiresGameEvents || gameEventsAvailable)
                    ? BuildUnitModificationAnalysisStatus.Analyzed
                    : BuildUnitModificationAnalysisStatus.RequiredEventsUnavailable;
        }
    }

    private static bool IsSupportedBuildUnitModification(BuildUnitModificationType type, int dataBuild)
    {
        return type switch
        {
            BuildUnitModificationType.Biomass => dataBuild >= BiomassMinimumDataBuild,
            BuildUnitModificationType.PowerOverwhelming => dataBuild >= PowerOverwhelmingMinimumDataBuild,
            BuildUnitModificationType.GuardianShell => dataBuild >= GuardianShellMinimumDataBuild,
            BuildUnitModificationType.OrbitalStrikeBeacon => dataBuild >= OrbitalStrikeBeaconMinimumDataBuild,
            BuildUnitModificationType.DarkPylon => dataBuild >= DarkPylonMinimumDataBuild,
            _ => false,
        };
    }

    private static int GetReplayDataBuild(Sc2Replay replay)
    {
        int dataBuild = replay.Header?.DataBuildNum ?? 0;
        if (dataBuild == 0)
        {
            _ = int.TryParse(replay.Metadata?.DataBuild, out dataBuild);
        }

        return dataBuild;
    }

    private static DirectStrikeBuildAreaUnit AddBuildAreaUnit(
        DirectStrikePlayerContext context,
        SUnitBornEvent bornEvent,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag)
    {
        string canonicalUnitName = GetCanonicalBuildUnitName(context, bornEvent.UnitTypeName);
        string displayUnitName = GetBuildUnitDisplayName(bornEvent.UnitTypeName);
        DirectStrikeBuildAreaUnit unit = new(
            context,
            bornEvent.UnitTagIndex,
            bornEvent.UnitTagRecycle,
            bornEvent.UnitIndex,
            bornEvent.UnitTypeName,
            canonicalUnitName,
            displayUnitName,
            bornEvent.CreatorAbilityName,
            bornEvent.Gameloop);
        buildAreaUnitsByTag[(bornEvent.UnitTagIndex, bornEvent.UnitTagRecycle)] = unit;
        if (!unit.IsPlaced)
        {
            return unit;
        }

        context.BuildAreaUnitsByTag[bornEvent.UnitIndex] = unit;
        if (!context.BuildAreaUnitsByDisplayName.TryGetValue(displayUnitName, out List<DirectStrikeBuildAreaUnit>? units))
        {
            units = [];
            context.BuildAreaUnitsByDisplayName.Add(displayUnitName, units);
        }

        units.Add(unit);
        return unit;
    }

    private static string GetCanonicalBuildUnitName(DirectStrikePlayerContext context, string rawUnitName)
    {
        if (context.CanonicalBuildUnitNamesByRawName?.TryGetValue(rawUnitName, out string? cachedName) is true)
        {
            return cachedName;
        }

        ReadOnlySpan<char> canonicalName = GetCanonicalSpawnUnitName(rawUnitName, context.Player.Commander);
        string result = canonicalName.SequenceEqual(rawUnitName)
            ? rawUnitName
            : canonicalName.ToString();
        context.CanonicalBuildUnitNamesByRawName ??= new(StringComparer.Ordinal);
        context.CanonicalBuildUnitNamesByRawName.Add(rawUnitName, result);
        return result;
    }

    private static void TrackTrackerBuildUnitModificationCandidate(
        SUnitBornEvent bornEvent,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag)
    {
        if (bornEvent.CreatorUnitTagIndex is not int creatorIndex
            || bornEvent.CreatorUnitTagRecycle is not int creatorRecycle
            || !buildAreaUnitsByTag.TryGetValue((creatorIndex, creatorRecycle), out DirectStrikeBuildAreaUnit? target)
            || !target.IsPlaced)
        {
            return;
        }

        DirectStrikePlayerContext context = target.Context;
        if (context.BuildUnitModificationAnalysisStatus != BuildUnitModificationAnalysisStatus.Analyzed)
        {
            return;
        }

        if (context.BuildUnitModificationType == BuildUnitModificationType.Biomass
            && bornEvent.UnitTypeName == "BiomassItem"
            && bornEvent.CreatorAbilityName == "InventoryUnit")
        {
            context.PendingBiomassItems.Add(new(bornEvent.Gameloop, target));
            return;
        }

        if (context.BuildUnitModificationType == BuildUnitModificationType.DarkPylon
            && bornEvent.UnitTypeName == "VorazunDarkPylon"
            && bornEvent.CreatorAbilityName == "InventoryUnit")
        {
            AddBuildUnitModification(
                context,
                BuildUnitModificationType.DarkPylon,
                bornEvent.Gameloop,
                target,
                1,
                null,
                rejectDuplicate: true);
        }
    }

    private static void DetectAlarakPowerOverwhelming(
        Sc2Replay replay,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag)
    {
        foreach (SUnitDiedEvent diedEvent in replay.TrackerEvents?.SUnitDiedEvents ?? [])
        {
            if (!buildAreaUnitsByTag.TryGetValue(
                    (diedEvent.UnitTagIndex, diedEvent.UnitTagRecycle),
                    out DirectStrikeBuildAreaUnit? source)
                || diedEvent.KillerUnitTagIndex is not int killerIndex
                || diedEvent.KillerUnitTagRecycle is not int killerRecycle
                || !buildAreaUnitsByTag.TryGetValue((killerIndex, killerRecycle), out DirectStrikeBuildAreaUnit? target)
                || source.Context != target.Context
                || target.Context.BuildUnitModificationType != BuildUnitModificationType.PowerOverwhelming
                || target.Context.BuildUnitModificationAnalysisStatus != BuildUnitModificationAnalysisStatus.Analyzed
                || source.RawUnitName != "SupplicantStarlight"
                || source.CreatorAbilityName != "SupplicantPlace"
                || target.RawUnitName != "AscendantStarlight"
                || target.CreatorAbilityName != "AscendantPlace")
            {
                continue;
            }

            AddBuildUnitModification(
                target.Context,
                BuildUnitModificationType.PowerOverwhelming,
                diedEvent.Gameloop,
                target,
                1,
                source,
                rejectDuplicate: false);
        }
    }

    private static void TrackBuildUnitModificationCommand(
        DirectStrikePlayerContext context,
        SCmdEvent command)
    {
        if (context.BuildUnitModificationAnalysisStatus != BuildUnitModificationAnalysisStatus.Analyzed)
        {
            return;
        }

        switch (context.BuildUnitModificationType)
        {
            case BuildUnitModificationType.Biomass:
                TrackBiomassCommand(context, command);
                break;
            case BuildUnitModificationType.GuardianShell:
                TrackGuardianShellCommand(context, command);
                break;
            case BuildUnitModificationType.OrbitalStrikeBeacon:
                TrackOrbitalStrikeBeaconCommand(context, command);
                break;
        }
    }

    private static void TrackBiomassCommand(DirectStrikePlayerContext context, SCmdEvent command)
    {
        if (command.AbilLink != BiomassAbilityLink || command.AbilCmdIndex is < 2 or > 4)
        {
            return;
        }

        for (int i = 0; i < context.PendingBiomassItems.Count; i++)
        {
            PendingBiomassItem item = context.PendingBiomassItems[i];
            int delta = item.Gameloop - command.Gameloop;
            if (delta is not (1 or 2))
            {
                continue;
            }

            context.PendingBiomassItems.RemoveAt(i);
            AddBuildUnitModification(
                context,
                BuildUnitModificationType.Biomass,
                command.Gameloop,
                item.Target,
                command.AbilCmdIndex + 1,
                null,
                rejectDuplicate: false);
            return;
        }
    }

    private static void TrackGuardianShellCommand(DirectStrikePlayerContext context, SCmdEvent command)
    {
        string? displayUnitName = GetGuardianShellDisplayUnitName(command);
        context.PendingGuardianShellTarget = null;
        if (displayUnitName is null)
        {
            context.PendingGuardianShellCommand = null;
            return;
        }

        context.PendingGuardianShellCommand = new(displayUnitName, command.Sequence);
        if (!context.BuildAreaUnitsByDisplayName.TryGetValue(
                displayUnitName,
                out List<DirectStrikeBuildAreaUnit>? units))
        {
            return;
        }

        DirectStrikeBuildAreaUnit? target = context.SelectedBuildAreaUnit;
        if (target is null
            || !IsEligibleGuardianShellTarget(context, target, displayUnitName, command.Gameloop))
        {
            target = null;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                DirectStrikeBuildAreaUnit unit = units[i];
                if (IsEligibleGuardianShellTarget(context, unit, displayUnitName, command.Gameloop))
                {
                    target = unit;
                    break;
                }
            }
        }

        if (target is DirectStrikeBuildAreaUnit resolvedTarget)
        {
            AddBuildUnitModification(
                context,
                BuildUnitModificationType.GuardianShell,
                command.Gameloop,
                resolvedTarget,
                1,
                null,
                rejectDuplicate: true);
        }
    }

    private static string? GetGuardianShellDisplayUnitName(SCmdEvent command)
    {
        return command switch
        {
            { AbilLink: 1114, AbilCmdIndex: 0 } => "Honor Guard",
            { AbilLink: 1115, AbilCmdIndex: 0 } => "Dragoon",
            { AbilLink: 1116, AbilCmdIndex: 0 } => "High Templar",
            { AbilLink: 1117, AbilCmdIndex: 0 } => "High Archon",
            { AbilLink: 1118, AbilCmdIndex: 0 } => "Phoenix",
            { AbilLink: 1119, AbilCmdIndex: 0 } => "Observer",
            { AbilLink: 1120, AbilCmdIndex: 0 } => "Immortal",
            { AbilLink: 1121, AbilCmdIndex: 0 } => "Reaver",
            { AbilLink: 1122, AbilCmdIndex: 0 } => "Purifier Tempest",
            _ => null,
        };
    }

    private static void TrackBuildUnitSelection(
        DirectStrikePlayerContext context,
        SSelectionDeltaEvent selection)
    {
        if (context.BuildUnitModificationType != BuildUnitModificationType.GuardianShell
            || context.BuildUnitModificationAnalysisStatus != BuildUnitModificationAnalysisStatus.Analyzed
            || selection.ControlGroupId != 10)
        {
            return;
        }

        context.SelectedBuildAreaUnit = null;
        context.PendingGuardianShellTarget = null;
        int? selectedUnitTag = GetSingleSelectedUnitTag(selection.Delta.AddUnitTags);
        if (selectedUnitTag is not int unitTag
            || !context.BuildAreaUnitsByTag.TryGetValue(unitTag, out DirectStrikeBuildAreaUnit? target)
            || !target.IsPlaced)
        {
            context.PendingGuardianShellCommand = null;
            return;
        }

        context.SelectedBuildAreaUnit = target;
        if (context.PendingGuardianShellCommand is not PendingGuardianShellCommand pending)
        {
            return;
        }

        if (IsEligibleGuardianShellTarget(
                context,
                target,
                pending.DisplayUnitName,
                selection.Gameloop))
        {
            context.PendingGuardianShellTarget = target;
        }
        else
        {
            context.PendingGuardianShellCommand = null;
        }
    }

    private static int? GetSingleSelectedUnitTag(ICollection<int> unitTags)
    {
        if (unitTags.Count != 1)
        {
            return null;
        }

        if (unitTags is List<int> list)
        {
            return list[0];
        }

        if (unitTags is int[] array)
        {
            return array[0];
        }

        foreach (int unitTag in unitTags)
        {
            return unitTag;
        }

        return null;
    }

    private static void TrackBuildUnitModificationCommandState(
        DirectStrikePlayerContext context,
        SCommandManagerStateEvent commandState)
    {
        if (context.BuildUnitModificationType != BuildUnitModificationType.GuardianShell
            || context.BuildUnitModificationAnalysisStatus != BuildUnitModificationAnalysisStatus.Analyzed
            || context.PendingGuardianShellCommand is not PendingGuardianShellCommand pending
            || commandState.State != 1
            || commandState.Sequence is not int sequence
            || sequence != pending.Sequence + 1)
        {
            context.PendingGuardianShellCommand = null;
            context.PendingGuardianShellTarget = null;
            return;
        }

        if (context.PendingGuardianShellTarget is DirectStrikeBuildAreaUnit target
            && IsEligibleGuardianShellTarget(
                context,
                target,
                pending.DisplayUnitName,
                commandState.Gameloop))
        {
            AddBuildUnitModification(
                context,
                BuildUnitModificationType.GuardianShell,
                commandState.Gameloop,
                target,
                1,
                null,
                rejectDuplicate: true);
        }

        context.PendingGuardianShellCommand = new(pending.DisplayUnitName, sequence);
        context.PendingGuardianShellTarget = null;
    }

    private static bool IsEligibleGuardianShellTarget(
        DirectStrikePlayerContext context,
        DirectStrikeBuildAreaUnit target,
        string displayUnitName,
        int gameloop)
    {
        return target.IsPlaced
            && target.Gameloop <= gameloop
            && string.Equals(target.DisplayUnitName, displayUnitName, StringComparison.Ordinal)
            && !context.ModifiedTargets.Contains((
                BuildUnitModificationType.GuardianShell,
                target.UnitTag));
    }

    private static void TrackOrbitalStrikeBeaconCommand(DirectStrikePlayerContext context, SCmdEvent command)
    {
        if (command is not { AbilLink: OrbitalStrikeBeaconAbilityLink, AbilCmdIndex: 0, TargetUnitTag: int targetUnitTag }
            || !context.BuildAreaUnitsByTag.TryGetValue(targetUnitTag, out DirectStrikeBuildAreaUnit? target)
            || !target.IsPlaced)
        {
            return;
        }

        AddBuildUnitModification(
            context,
            BuildUnitModificationType.OrbitalStrikeBeacon,
            command.Gameloop,
            target,
            1,
            null,
            rejectDuplicate: true);
    }

    private static void AddBuildUnitModification(
        DirectStrikePlayerContext context,
        BuildUnitModificationType type,
        int gameloop,
        DirectStrikeBuildAreaUnit target,
        int amount,
        DirectStrikeBuildAreaUnit? source,
        bool rejectDuplicate)
    {
        (BuildUnitModificationType Type, int TargetUnitTag) key = (type, target.UnitTag);
        if (rejectDuplicate && !context.ModifiedTargets.Add(key))
        {
            return;
        }

        if (!rejectDuplicate)
        {
            context.ModifiedTargets.Add(key);
        }

        context.BuildUnitModifications.Add(new(
            type,
            gameloop,
            target.UnitTag,
            target.CanonicalUnitName,
            amount,
            source?.UnitTag,
            source?.CanonicalUnitName));
    }

    private static void FinalizeBuildUnitModifications(DirectStrikePlayerContext[] playerContexts)
    {
        foreach (DirectStrikePlayerContext context in playerContexts)
        {
            if (context.BuildUnitModificationType == BuildUnitModificationType.None)
            {
                continue;
            }

            context.BuildUnitModifications.Sort(static (left, right) =>
            {
                int comparison = left.Gameloop.CompareTo(right.Gameloop);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = left.Type.CompareTo(right.Type);
                return comparison != 0
                    ? comparison
                    : left.TargetUnitTag.CompareTo(right.TargetUnitTag);
            });
            context.Player.BuildUnitModifications = context.BuildUnitModifications.AsReadOnly();
            context.Player.BuildUnitModificationAnalysis = new List<BuildUnitModificationAnalysis>(1)
            {
                new(context.BuildUnitModificationType, context.BuildUnitModificationAnalysisStatus),
            }.AsReadOnly();
        }
    }

    private static string GetBuildUnitDisplayName(string rawUnitName)
    {
        return rawUnitName switch
        {
            "AbathurMutalisk" => "Mutalisk",
            "ViperAbathur" => "Viper",
            "GuardianStarlight" => "Guardian",
            "VileRoach" => "Roach",
            "SwarmHostAbathur" => "Swarm Host",
            "AscendantStarlight" => "Ascendant",
            "SupplicantStarlight" => "Supplicant",
            "HonorGuard" => "Honor Guard",
            "DragoonStarlight" or "Dragoon" => "Dragoon",
            "HighTemplarArtanis" or "HighTemplar" => "High Templar",
            "HighArchon" => "High Archon",
            "PhoenixArtanis" or "Phoenix" => "Phoenix",
            "ArtanisObserver" or "Observer" => "Observer",
            "ImmortalArtanis" or "Immortal" => "Immortal",
            "ReaverStarlight" or "Reaver" => "Reaver",
            "PurifierTempest" => "Purifier Tempest",
            "Mirage" or "MirageKarax" or "KaraxMirage" => "Mirage",
            "VoidRayVorazun" => "Void Ray",
            _ => rawUnitName,
        };
    }
}
