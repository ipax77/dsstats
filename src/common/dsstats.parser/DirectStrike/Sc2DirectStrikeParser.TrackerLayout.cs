using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private const double GameLoopsPerSecond = 22.4D;
    private const int SpawnGroupWindowGameloops = 112;
    private const string PlayerStateVictoryUpgrade = "PlayerStateVictory";
    private const string PlayerStateGameOverUpgrade = "PlayerStateGameOver";
    private const string PlayerIsAfkUpgrade = "PlayerIsAFK";

    private static readonly FrozenDictionary<string, string> CanonicalUnitNameAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SwarmHostMP"] = "SwarmHost",
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static void SetTrackerData(Sc2Replay replay, DirectStrikePlayerContext[] playerContexts, DirectStrikeReplay directStrikeReplay)
    {
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId = GetPlayerContextIndexesByControlPlayerId(replay, playerContexts);
        PlayerLayout?[] playerLayouts = new PlayerLayout?[playerContexts.Length];
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikePlayerRefinery> refineriesByTag = [];
        Commander[] commandersByPlayerIndex = new Commander[playerContexts.Length];
        bool[] hasCommanderByPlayerIndex = new bool[playerContexts.Length];
        List<MiddleControlChange> middleControlChanges = [];
        MapLayout mapLayout = new();

        SetPlayerStats(replay, playerContextsByControlPlayerId, playerContexts);

        foreach (SUnitBornEvent bornEvent in replay.TrackerEvents?.SUnitBornEvents ?? [])
        {
            bool hasPlayerContext = playerContextsByControlPlayerId.TryGetValue(bornEvent.ControlPlayerId, out PlayerContextIndex playerContext);
            if (bornEvent.Gameloop <= 1440
                && hasPlayerContext
                && TryParseWorkerCommander(bornEvent.UnitTypeName, out Commander commander))
            {
                int playerIndex = playerContext.Index;
                if (!hasCommanderByPlayerIndex[playerIndex]
                    || (IsGenericRaceCommander(commandersByPlayerIndex[playerIndex]) && !IsGenericRaceCommander(commander)))
                {
                    commandersByPlayerIndex[playerIndex] = commander;
                    hasCommanderByPlayerIndex[playerIndex] = true;
                }
            }

            if (bornEvent.Gameloop != 0)
            {
                continue;
            }

            Pos pos = new(bornEvent.X, bornEvent.Y);
            switch (bornEvent.UnitTypeName)
            {
                case "StagingAreaFootprintSouth":
                case "AreaMarkerSouth":
                    if (hasPlayerContext && TryGetPlayerLayout(playerContexts, playerLayouts, playerContext.Index, out DirectStrikePlayer? southPlayer, out PlayerLayout? southLayout))
                    {
                        southLayout.South = pos;
                        southPlayer.TeamId = bornEvent.Y * bornEvent.Y > 10000 ? 1 : 2;
                    }

                    break;

                case "StagingAreaFootprintWest":
                case "AreaMarkerWest":
                    if (hasPlayerContext && TryGetPlayerLayout(playerContexts, playerLayouts, playerContext.Index, out _, out PlayerLayout? westLayout))
                    {
                        westLayout.West = pos;
                    }

                    break;

                case "StagingAreaFootprintNorth":
                case "AreaMarkerNorth":
                    if (hasPlayerContext && TryGetPlayerLayout(playerContexts, playerLayouts, playerContext.Index, out _, out PlayerLayout? northLayout))
                    {
                        northLayout.North = pos;
                    }

                    break;

                case "StagingAreaFootprintEast":
                case "AreaMarkerEast":
                    if (hasPlayerContext && TryGetPlayerLayout(playerContexts, playerLayouts, playerContext.Index, out _, out PlayerLayout? eastLayout))
                    {
                        eastLayout.East = pos;
                    }

                    break;

                case "ObjectiveNexus":
                    mapLayout.Nexus = pos;
                    if (bornEvent.SUnitDiedEvent is { } nexusDeath)
                    {
                        directStrikeReplay.GameEndTime = ToTimeSpan(nexusDeath.Gameloop);
                        directStrikeReplay.WinnerTeam = 1;
                    }

                    break;

                case "ObjectivePlanetaryFortress":
                    mapLayout.Planetary = pos;
                    if (directStrikeReplay.GameEndTime == TimeSpan.Zero
                        && bornEvent.SUnitDiedEvent is { } planetaryDeath)
                    {
                        directStrikeReplay.GameEndTime = ToTimeSpan(planetaryDeath.Gameloop);
                        directStrikeReplay.WinnerTeam = 2;
                    }

                    break;

                case "ObjectivePhotonCannon":
                    mapLayout.Cannon = pos;
                    if (bornEvent.SUnitDiedEvent is { } cannonDeath)
                    {
                        directStrikeReplay.CannonTime = ToTimeSpan(cannonDeath.Gameloop);
                    }

                    break;

                case "ObjectiveBunker":
                    mapLayout.Bunker = pos;
                    if (bornEvent.SUnitDiedEvent is { } bunkerDeath)
                    {
                        directStrikeReplay.BunkerTime = ToTimeSpan(bunkerDeath.Gameloop);
                    }

                    break;
            }

            if (hasPlayerContext
                && bornEvent.UnitTypeName.StartsWith("MineralField", StringComparison.Ordinal))
            {
                DirectStrikePlayerRefinery refinery = new()
                {
                    UnitTagIndex = bornEvent.UnitTagIndex,
                    UnitTagRecycle = bornEvent.UnitTagRecycle,
                };
                playerContext.Context.Refineries.Add(refinery);
                refineriesByTag.TryAdd((refinery.UnitTagIndex, refinery.UnitTagRecycle), refinery);
            }
        }

        for (int i = 0; i < playerContexts.Length; i++)
        {
            if (hasCommanderByPlayerIndex[i])
            {
                playerContexts[i].Player.Commander = commandersByPlayerIndex[i];
            }
        }

        InitializeBuildUnitModificationAnalysis(replay, playerContexts);
        SetPlayerUpgrades(replay, playerContextsByControlPlayerId, playerContexts, directStrikeReplay);
        SetPlayerResultsFromWinnerTeam(directStrikeReplay);

        foreach (SUnitOwnerChangeEvent ownerChangeEvent in replay.TrackerEvents?.SUnitOwnerChangeEvents ?? [])
        {
            if (ownerChangeEvent.UnitTagIndex != 20
                || !TryGetMiddleControlTeam(ownerChangeEvent.UpkeepPlayerId, out int team))
            {
                continue;
            }

            middleControlChanges.Add(new(ownerChangeEvent.Gameloop, team));
        }

        if (middleControlChanges.Count > 0)
        {
            directStrikeReplay.FirstMiddleControlTeam = middleControlChanges[0].Team;
            TimeSpan[] middleChanges = new TimeSpan[middleControlChanges.Count];
            for (int i = 0; i < middleControlChanges.Count; i++)
            {
                middleChanges[i] = ToTimeSpan(middleControlChanges[i].Gameloop);
            }

            directStrikeReplay.MiddleChanges = middleChanges;
        }

        if (mapLayout.Planetary is { } planetary)
        {
            SetGamePositions(playerContexts, playerLayouts, planetary);
        }

        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag = [];
        SetPlayerSpawns(
            replay,
            playerContexts,
            playerContextsByControlPlayerId,
            playerLayouts,
            refineriesByTag,
            buildAreaUnitsByTag);
        DetectAlarakPowerOverwhelming(replay, buildAreaUnitsByTag);

        foreach (DirectStrikePlayerContext context in playerContexts)
        {
            context.Refineries.Sort(static (left, right) => left.Gameloop.CompareTo(right.Gameloop));
            List<TimeSpan> refineryTimes = new(context.Refineries.Count);
            foreach (DirectStrikePlayerRefinery refinery in context.Refineries)
            {
                if (refinery.Taken)
                {
                    refineryTimes.Add(ToTimeSpan(refinery.Gameloop));
                }
            }

            context.Player.RefineryTimes = [.. refineryTimes];
        }
    }

    private static Dictionary<int, PlayerContextIndex> GetPlayerContextIndexesByControlPlayerId(Sc2Replay replay, DirectStrikePlayerContext[] playerContexts)
    {
        Dictionary<int, DirectStrikePlayerContext> contextsByControlPlayerId = GetPlayerContextsByControlPlayerId(replay, playerContexts);
        Dictionary<int, PlayerContextIndex> indexedContextsByControlPlayerId = new(contextsByControlPlayerId.Count);
        foreach (KeyValuePair<int, DirectStrikePlayerContext> pair in contextsByControlPlayerId)
        {
            indexedContextsByControlPlayerId.Add(pair.Key, new(pair.Value.DetailsIndex, pair.Value));
        }

        return indexedContextsByControlPlayerId;
    }

    private static void SetPlayerResultsFromWinnerTeam(DirectStrikeReplay replay)
    {
        if (replay.WinnerTeam is not (1 or 2))
        {
            return;
        }

        foreach (DirectStrikePlayer player in replay.Players)
        {
            if (player.TeamId is 1 or 2)
            {
                player.Result = player.TeamId == replay.WinnerTeam ? PlayerResult.Win : PlayerResult.Loss;
            }
        }
    }

    private static void SetPlayerSpawns(
        Sc2Replay replay,
        DirectStrikePlayerContext[] playerContexts,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        PlayerLayout?[] playerLayouts,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikePlayerRefinery> refineriesByTag,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag)
    {
        HashSet<string>?[] builtUnitNamesByPlayer = new HashSet<string>?[playerContexts.Length];
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer = new HashSet<string>?[playerContexts.Length];
        List<TrackedSpawnUnit>?[] spawnUnitsByPlayer = new List<TrackedSpawnUnit>?[playerContexts.Length];
        Polygon?[] stagingAreasByPlayer = GetStagingAreasByPlayer(playerLayouts);
        ICollection<SUnitBornEvent> unitBornEvents = replay.TrackerEvents?.SUnitBornEvents ?? [];
        ICollection<SUnitTypeChangeEvent> unitTypeChangeEvents = replay.TrackerEvents?.SUnitTypeChangeEvents ?? [];
        if (unitBornEvents is IReadOnlyList<SUnitBornEvent> orderedUnitBornEvents
            && unitTypeChangeEvents is IReadOnlyList<SUnitTypeChangeEvent> orderedUnitTypeChangeEvents
            && IsOrderedByGameloop(orderedUnitBornEvents)
            && IsOrderedByGameloop(orderedUnitTypeChangeEvents))
        {
            TrackOrderedSpawnEvents(
                orderedUnitBornEvents,
                orderedUnitTypeChangeEvents,
                playerContextsByControlPlayerId,
                playerContexts,
                stagingAreasByPlayer,
                buildAreaUnitsByTag,
                builtUnitNamesByPlayer,
                canonicalSpawnUnitNamesByPlayer,
                spawnUnitsByPlayer,
                refineriesByTag);
        }
        else
        {
            List<OrderedSpawnTrackerEvent> orderedTrackerEvents = GetOrderedSpawnTrackerEvents(unitBornEvents, unitTypeChangeEvents);
            TrackSpawnEvents(
                orderedTrackerEvents,
                playerContextsByControlPlayerId,
                playerContexts,
                stagingAreasByPlayer,
                buildAreaUnitsByTag,
                builtUnitNamesByPlayer,
                canonicalSpawnUnitNamesByPlayer,
                spawnUnitsByPlayer,
                refineriesByTag);
        }

        for (int i = 0; i < playerContexts.Length; i++)
        {
            DirectStrikePlayer player = playerContexts[i].Player;
            player.BuildUnitNames = builtUnitNamesByPlayer[i] is { } builtUnitNames
                ? ToSortedReadOnlyCollection(builtUnitNames)
                : [];
            player.Spawns = spawnUnitsByPlayer[i] is { } spawnUnits
                ? GroupPlayerSpawns(spawnUnits, player.Stats)
                : [];
        }
    }

    private static List<OrderedSpawnTrackerEvent> GetOrderedSpawnTrackerEvents(
        ICollection<SUnitBornEvent> unitBornEvents,
        ICollection<SUnitTypeChangeEvent> unitTypeChangeEvents)
    {
        List<OrderedSpawnTrackerEvent> orderedTrackerEvents = new(unitBornEvents.Count + unitTypeChangeEvents.Count);
        int index = 0;
        foreach (SUnitBornEvent unitBornEvent in unitBornEvents)
        {
            orderedTrackerEvents.Add(new(unitBornEvent.Gameloop, index, unitBornEvent, null));
            index++;
        }

        foreach (SUnitTypeChangeEvent unitTypeChangeEvent in unitTypeChangeEvents)
        {
            orderedTrackerEvents.Add(new(unitTypeChangeEvent.Gameloop, index, null, unitTypeChangeEvent));
            index++;
        }

        orderedTrackerEvents.Sort(static (left, right) =>
        {
            int gameloopComparison = left.Gameloop.CompareTo(right.Gameloop);
            return gameloopComparison != 0 ? gameloopComparison : left.Index.CompareTo(right.Index);
        });

        return orderedTrackerEvents;
    }

    private static bool IsOrderedByGameloop<T>(IReadOnlyList<T> events)
        where T : TrackerEvent
    {
        for (int i = 1; i < events.Count; i++)
        {

            if (events[i].Gameloop < events[i - 1].Gameloop)
            {

                return false;
            }

        }


        return true;
    }

    private static void TrackOrderedSpawnEvents(
        IReadOnlyList<SUnitBornEvent> unitBornEvents,
        IReadOnlyList<SUnitTypeChangeEvent> unitTypeChangeEvents,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        DirectStrikePlayerContext[] playerContexts,
        Polygon?[] stagingAreasByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag,
        HashSet<string>?[] builtUnitNamesByPlayer,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        List<TrackedSpawnUnit>?[] spawnUnitsByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikePlayerRefinery> refineriesByTag)
    {
        int bornIndex = 0;
        int typeChangeIndex = 0;
        List<SpawnCandidate> spawnCandidates = [];
        while (bornIndex < unitBornEvents.Count || typeChangeIndex < unitTypeChangeEvents.Count)
        {
            spawnCandidates.Clear();
            int gameloop = GetNextSpawnTrackerGameloop(unitBornEvents, bornIndex, unitTypeChangeEvents, typeChangeIndex);
            int bornStart = bornIndex;
            while (bornIndex < unitBornEvents.Count && unitBornEvents[bornIndex].Gameloop == gameloop)
            {
                bornIndex++;
            }

            int typeChangeStart = typeChangeIndex;
            while (typeChangeIndex < unitTypeChangeEvents.Count && unitTypeChangeEvents[typeChangeIndex].Gameloop == gameloop)
            {
                typeChangeIndex++;
            }

            for (int i = bornStart; i < bornIndex; i++)
            {
                TrackBuildUnitBornEventAndQueueSpawnCandidate(
                    unitBornEvents[i],
                    playerContextsByControlPlayerId,
                    stagingAreasByPlayer,
                    buildAreaUnitsByTag,
                    builtUnitNamesByPlayer,
                    canonicalSpawnUnitNamesByPlayer,
                    spawnCandidates);
            }

            for (int i = typeChangeStart; i < typeChangeIndex; i++)
            {
                TrackUnitTypeChangeEvent(unitTypeChangeEvents[i], playerContexts, buildAreaUnitsByTag, builtUnitNamesByPlayer, canonicalSpawnUnitNamesByPlayer, refineriesByTag);
            }

            for (int i = 0; i < spawnCandidates.Count; i++)
            {
                TrackSpawnUnit(spawnCandidates[i], playerContexts, canonicalSpawnUnitNamesByPlayer, spawnUnitsByPlayer);
            }
        }
    }

    private static int GetNextSpawnTrackerGameloop(
        IReadOnlyList<SUnitBornEvent> unitBornEvents,
        int bornIndex,
        IReadOnlyList<SUnitTypeChangeEvent> unitTypeChangeEvents,
        int typeChangeIndex)
    {
        if (bornIndex >= unitBornEvents.Count)
        {
            return unitTypeChangeEvents[typeChangeIndex].Gameloop;
        }

        if (typeChangeIndex >= unitTypeChangeEvents.Count)
        {
            return unitBornEvents[bornIndex].Gameloop;
        }

        int bornGameloop = unitBornEvents[bornIndex].Gameloop;
        int typeChangeGameloop = unitTypeChangeEvents[typeChangeIndex].Gameloop;
        return bornGameloop <= typeChangeGameloop ? bornGameloop : typeChangeGameloop;
    }

    private static void TrackSpawnEvents(
        List<OrderedSpawnTrackerEvent> orderedTrackerEvents,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        DirectStrikePlayerContext[] playerContexts,
        Polygon?[] stagingAreasByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag,
        HashSet<string>?[] builtUnitNamesByPlayer,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        List<TrackedSpawnUnit>?[] spawnUnitsByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikePlayerRefinery> refineriesByTag)
    {
        int index = 0;
        List<SpawnCandidate> spawnCandidates = [];
        while (index < orderedTrackerEvents.Count)
        {
            spawnCandidates.Clear();
            int gameloop = orderedTrackerEvents[index].Gameloop;
            int nextIndex = index + 1;
            while (nextIndex < orderedTrackerEvents.Count && orderedTrackerEvents[nextIndex].Gameloop == gameloop)
            {
                nextIndex++;
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].BornEvent is { } bornEvent)
                {
                    TrackBuildUnitBornEventAndQueueSpawnCandidate(
                        bornEvent,
                        playerContextsByControlPlayerId,
                        stagingAreasByPlayer,
                        buildAreaUnitsByTag,
                        builtUnitNamesByPlayer,
                        canonicalSpawnUnitNamesByPlayer,
                        spawnCandidates);
                }
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].TypeChangeEvent is { } typeChangeEvent)
                {
                    TrackUnitTypeChangeEvent(typeChangeEvent, playerContexts, buildAreaUnitsByTag, builtUnitNamesByPlayer, canonicalSpawnUnitNamesByPlayer, refineriesByTag);
                }
            }

            for (int i = 0; i < spawnCandidates.Count; i++)
            {
                TrackSpawnUnit(spawnCandidates[i], playerContexts, canonicalSpawnUnitNamesByPlayer, spawnUnitsByPlayer);
            }

            index = nextIndex;
        }
    }

    private static ReadOnlyCollection<string> ToSortedReadOnlyCollection(HashSet<string> values)
    {
        List<string> sortedValues = [.. values];
        sortedValues.Sort(StringComparer.Ordinal);
        return sortedValues.AsReadOnly();
    }

    private static void TrackBuildUnitBornEventAndQueueSpawnCandidate(
        SUnitBornEvent unitBornEvent,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        Polygon?[] stagingAreasByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag,
        HashSet<string>?[] builtUnitNamesByPlayer,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        List<SpawnCandidate> spawnCandidates)
    {
        if (unitBornEvent.Gameloop == 0
            || !playerContextsByControlPlayerId.TryGetValue(unitBornEvent.ControlPlayerId, out PlayerContextIndex context))
        {
            return;
        }

        int playerIndex = context.Index;
        DirectStrikePlayer player = context.Context.Player;
        Pos position = new(unitBornEvent.X, unitBornEvent.Y);
        if (stagingAreasByPlayer[playerIndex] is { } stagingArea
            && stagingArea.Contains(position))
        {
            AddBuiltUnitName(builtUnitNamesByPlayer, canonicalSpawnUnitNamesByPlayer, playerIndex, player.Commander, unitBornEvent.UnitTypeName);
            AddBuildAreaUnit(context.Context, unitBornEvent, buildAreaUnitsByTag);
        }

        TrackTrackerBuildUnitModificationCandidate(unitBornEvent, buildAreaUnitsByTag);

        if (player.TeamId is 1 or 2 && MapLayout.IsSpawnUnit(position, player.TeamId))
        {
            spawnCandidates.Add(new(unitBornEvent, playerIndex, position));
        }
    }

    private static void TrackUnitTypeChangeEvent(
        SUnitTypeChangeEvent typeChangeEvent,
        DirectStrikePlayerContext[] playerContexts,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikeBuildAreaUnit> buildAreaUnitsByTag,
        HashSet<string>?[] builtUnitNamesByPlayer,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), DirectStrikePlayerRefinery> refineriesByTag)
    {
        (int UnitTagIndex, int UnitTagRecycle) tag = (typeChangeEvent.UnitTagIndex, typeChangeEvent.UnitTagRecycle);
        if (IsRefineryMinerals(typeChangeEvent.UnitTypeName)
            && refineriesByTag.TryGetValue(tag, out DirectStrikePlayerRefinery? refinery)
            && !refinery.Taken)
        {
            refinery.Gameloop = typeChangeEvent.Gameloop;
            refinery.Taken = true;
        }

        if (typeChangeEvent.Gameloop == 0
            || !buildAreaUnitsByTag.TryGetValue(tag, out DirectStrikeBuildAreaUnit? buildAreaUnit))
        {
            return;
        }

        int playerIndex = buildAreaUnit.Context.DetailsIndex;
        AddBuiltUnitName(
            builtUnitNamesByPlayer,
            canonicalSpawnUnitNamesByPlayer,
            playerIndex,
            playerContexts[playerIndex].Player.Commander,
            typeChangeEvent.UnitTypeName);
    }

    private static void TrackSpawnUnit(
        SpawnCandidate spawnCandidate,
        DirectStrikePlayerContext[] playerContexts,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        List<TrackedSpawnUnit>?[] spawnUnitsByPlayer)
    {
        SUnitBornEvent unitBornEvent = spawnCandidate.Event;
        int playerIndex = spawnCandidate.PlayerIndex;
        if (canonicalSpawnUnitNamesByPlayer[playerIndex] is not { } canonicalSpawnUnitNames
            || !canonicalSpawnUnitNames
                .GetAlternateLookup<ReadOnlySpan<char>>()
                .Contains(GetCanonicalSpawnUnitName(unitBornEvent.UnitTypeName, playerContexts[playerIndex].Player.Commander)))
        {
            return;
        }

        GetSpawnUnits(spawnUnitsByPlayer, playerIndex).Add(new(
            unitBornEvent.UnitIndex,
            unitBornEvent.UnitTypeName,
            unitBornEvent.Gameloop,
            spawnCandidate.Position,
            unitBornEvent.SUnitDiedEvent is { } diedEvent ? new(diedEvent.X, diedEvent.Y) : null,
            unitBornEvent.SUnitDiedEvent?.Gameloop));
    }

    private static void AddBuiltUnitName(
        HashSet<string>?[] builtUnitNamesByPlayer,
        HashSet<string>?[] canonicalSpawnUnitNamesByPlayer,
        int playerIndex,
        Commander commander,
        string unitTypeName)
    {
        HashSet<string> builtUnitNames = GetBuiltUnitNames(builtUnitNamesByPlayer, playerIndex);
        if (!builtUnitNames.Add(unitTypeName))
        {
            return;
        }

        HashSet<string> canonicalSpawnUnitNames = GetCanonicalSpawnUnitNames(canonicalSpawnUnitNamesByPlayer, playerIndex);
        ReadOnlySpan<char> canonicalName = GetCanonicalSpawnUnitName(unitTypeName, commander);
        canonicalSpawnUnitNames.Add(canonicalName.Length == unitTypeName.Length ? unitTypeName : canonicalName.ToString());
    }

    private static void SetPlayerStats(
        Sc2Replay replay,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        DirectStrikePlayerContext[] playerContexts)
    {
        List<DirectStrikePlayerStats>?[] statsByPlayer = new List<DirectStrikePlayerStats>?[playerContexts.Length];

        foreach (SPlayerStatsEvent statsEvent in replay.TrackerEvents?.SPlayerStatsEvents ?? [])
        {
            if (!playerContextsByControlPlayerId.TryGetValue(statsEvent.PlayerId, out PlayerContextIndex context))
            {
                continue;
            }

            DirectStrikePlayer player = context.Context.Player;
            DirectStrikePlayerStats stats = new()
            {
                Gameloop = statsEvent.Gameloop,
                Time = ToTimeSpan(statsEvent.Gameloop),
                MineralsCollectionRate = statsEvent.MineralsCollectionRate,
                MineralsUsedActiveForces = statsEvent.MineralsUsedActiveForces,
                MineralsUsedCurrentTechnology = statsEvent.MineralsUsedCurrentTechnology,
                MineralsKilledArmy = statsEvent.MineralsKilledArmy,
                MineralsLostArmy = statsEvent.MineralsLostArmy,
            };

            GetPlayerStats(statsByPlayer, context.Index).Add(stats);
            if (stats.MineralsCollectionRate > 0 && stats.Gameloop >= player.DurationGameloop)
            {
                player.DurationGameloop = stats.Gameloop;
                player.Duration = stats.Time;
            }
        }

        for (int i = 0; i < playerContexts.Length; i++)
        {
            playerContexts[i].Player.Stats = statsByPlayer[i] is { } stats
                ? SortStats(stats)
                : [];
        }
    }

    private static ReadOnlyCollection<DirectStrikePlayerStats> SortStats(List<DirectStrikePlayerStats> stats)
    {
        stats.Sort(static (left, right) => left.Gameloop.CompareTo(right.Gameloop));
        return stats.AsReadOnly();
    }

    private static void SetPlayerUpgrades(
        Sc2Replay replay,
        Dictionary<int, PlayerContextIndex> playerContextsByControlPlayerId,
        DirectStrikePlayerContext[] playerContexts,
        DirectStrikeReplay directStrikeReplay)
    {
        List<int>?[] tierUpgradesByPlayer = new List<int>?[playerContexts.Length];
        Dictionary<string, int>?[] upgradesByPlayer = new Dictionary<string, int>?[playerContexts.Length];
        int[]? afkGameloopsByPlayer = null;
        int? victoryTeam = null;
        bool hasInvalidVictoryTeam = false;

        foreach (SUpgradeEvent upgradeEvent in replay.TrackerEvents?.SUpgradeEvents ?? [])
        {
            if (upgradeEvent.Gameloop == 0
                || !playerContextsByControlPlayerId.TryGetValue(upgradeEvent.PlayerId, out PlayerContextIndex context))
            {
                continue;
            }

            DirectStrikePlayer player = context.Context.Player;
            string upgradeName = upgradeEvent.UpgradeTypeName;
            if (upgradeName == PlayerIsAfkUpgrade)
            {
                afkGameloopsByPlayer ??= new int[playerContexts.Length];
                ref int afkGameloop = ref afkGameloopsByPlayer[context.Index];
                if (afkGameloop == 0 || upgradeEvent.Gameloop < afkGameloop)
                {
                    afkGameloop = upgradeEvent.Gameloop;
                }

                continue;
            }

            if (upgradeName is PlayerStateVictoryUpgrade or PlayerStateGameOverUpgrade)
            {
                if (upgradeName == PlayerStateVictoryUpgrade)
                {
                    if (player.TeamId is not (1 or 2))
                    {
                        hasInvalidVictoryTeam = true;
                    }
                    else if (victoryTeam is null)
                    {
                        victoryTeam = player.TeamId;
                    }
                    else if (victoryTeam.Value != player.TeamId)
                    {
                        hasInvalidVictoryTeam = true;
                    }
                }

                continue;
            }

            if (upgradeName is "Tier2" or "Tier3")
            {
                GetTierUpgrades(tierUpgradesByPlayer, context.Index).Add(upgradeEvent.Gameloop);
                continue;
            }

            if (FilterUpgrades(upgradeName)
                || (upgradeName.Contains("Level", StringComparison.Ordinal) && !IsNormalizedLevelUpgrade(upgradeName, player.Commander)))
            {
                continue;
            }

            GetPlayerUpgrades(upgradesByPlayer, context.Index).TryAdd(upgradeName, upgradeEvent.Gameloop);
        }

        if (victoryTeam is { } team && !hasInvalidVictoryTeam)
        {
            directStrikeReplay.WinnerTeam = team;
        }

        for (int i = 0; i < playerContexts.Length; i++)
        {
            DirectStrikePlayer player = playerContexts[i].Player;
            if (afkGameloopsByPlayer?[i] is > 0 and var afkGameloop)
            {
                SetAfkDuration(player, afkGameloop);
            }

            if (tierUpgradesByPlayer[i] is { } tierUpgrades)
            {
                tierUpgrades.Sort();
                TimeSpan[] tierUpgradeTimes = new TimeSpan[tierUpgrades.Count];
                for (int j = 0; j < tierUpgrades.Count; j++)
                {
                    tierUpgradeTimes[j] = ToTimeSpan(tierUpgrades[j]);
                }

                player.TierUpgrades = tierUpgradeTimes;
            }

            if (upgradesByPlayer[i] is { } upgrades)
            {
                Dictionary<string, TimeSpan> upgradeTimes = new(upgrades.Count, StringComparer.Ordinal);
                foreach (KeyValuePair<string, int> pair in upgrades)
                {
                    upgradeTimes.Add(pair.Key, ToTimeSpan(pair.Value));
                }

                player.Upgrades = new ReadOnlyDictionary<string, TimeSpan>(upgradeTimes);
            }
        }
    }

    private static void SetAfkDuration(DirectStrikePlayer player, int afkGameloop)
    {
        foreach (DirectStrikePlayerStats stats in player.Stats)
        {
            if (stats.Gameloop >= afkGameloop && stats.MineralsCollectionRate == 0)
            {
                player.DurationGameloop = stats.Gameloop;
                player.Duration = stats.Time;
                return;
            }
        }
    }

    private static Polygon?[] GetStagingAreasByPlayer(PlayerLayout?[] playerLayouts)
    {
        Polygon?[] stagingAreasByPlayer = new Polygon?[playerLayouts.Length];
        for (int i = 0; i < playerLayouts.Length; i++)
        {
            if (playerLayouts[i]?.TryGetStagingArea(out Polygon? stagingArea) == true)
            {
                stagingAreasByPlayer[i] = stagingArea;
            }
        }

        return stagingAreasByPlayer;
    }

    private static HashSet<string> GetBuiltUnitNames(HashSet<string>?[] builtUnitNamesByPlayer, int playerIndex)
    {
        if (builtUnitNamesByPlayer[playerIndex] is not { } builtUnitNames)
        {
            builtUnitNames = new(StringComparer.Ordinal);
            builtUnitNamesByPlayer[playerIndex] = builtUnitNames;
        }

        return builtUnitNames;
    }

    private static HashSet<string> GetCanonicalSpawnUnitNames(HashSet<string>?[] canonicalSpawnUnitNamesByPlayer, int playerIndex)
    {
        if (canonicalSpawnUnitNamesByPlayer[playerIndex] is not { } canonicalSpawnUnitNames)
        {
            canonicalSpawnUnitNames = new(StringComparer.Ordinal);
            canonicalSpawnUnitNamesByPlayer[playerIndex] = canonicalSpawnUnitNames;
        }

        return canonicalSpawnUnitNames;
    }

    private static ReadOnlySpan<char> GetCanonicalSpawnUnitName(string unitTypeName, Commander commander)
    {
        ReadOnlySpan<char> canonicalName = unitTypeName;
        string? commanderAffix = GetCommanderUnitNameAffix(commander);
        while (true)
        {
            if (commanderAffix is not null && canonicalName.Length > commanderAffix.Length)
            {
                if (canonicalName.StartsWith(commanderAffix, StringComparison.Ordinal))
                {
                    canonicalName = canonicalName[commanderAffix.Length..];
                    continue;
                }

                if (canonicalName.EndsWith(commanderAffix, StringComparison.Ordinal))
                {
                    canonicalName = canonicalName[..^commanderAffix.Length];
                    continue;
                }
            }

            if (!TryTrimUnitNameSuffix(ref canonicalName, "Lightweight")
                && !TryTrimUnitNameSuffix(ref canonicalName, "Starlight"))
            {
                break;
            }
        }

        return CanonicalUnitNameAliases
            .GetAlternateLookup<ReadOnlySpan<char>>()
            .TryGetValue(canonicalName, out string? alias)
            ? alias
            : canonicalName;
    }

    private static bool TryTrimUnitNameSuffix(ref ReadOnlySpan<char> unitName, ReadOnlySpan<char> suffix)
    {
        if (unitName.Length <= suffix.Length || !unitName.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        unitName = unitName[..^suffix.Length];
        return true;
    }

    private static string? GetCommanderUnitNameAffix(Commander commander)
    {
        return commander switch
        {
            Commander.Abathur => nameof(Commander.Abathur),
            Commander.Alarak => nameof(Commander.Alarak),
            Commander.Artanis => nameof(Commander.Artanis),
            Commander.Dehaka => nameof(Commander.Dehaka),
            Commander.Fenix => nameof(Commander.Fenix),
            Commander.Horner => nameof(Commander.Horner),
            Commander.Karax => nameof(Commander.Karax),
            Commander.Kerrigan => nameof(Commander.Kerrigan),
            Commander.Mengsk => nameof(Commander.Mengsk),
            Commander.Nova => nameof(Commander.Nova),
            Commander.Raynor => nameof(Commander.Raynor),
            Commander.Stetmann => nameof(Commander.Stetmann),
            Commander.Stukov => nameof(Commander.Stukov),
            Commander.Swann => nameof(Commander.Swann),
            Commander.Tychus => nameof(Commander.Tychus),
            Commander.Vorazun => nameof(Commander.Vorazun),
            Commander.Zagara => nameof(Commander.Zagara),
            Commander.Zeratul => nameof(Commander.Zeratul),
            _ => null,
        };
    }

    private static List<TrackedSpawnUnit> GetSpawnUnits(List<TrackedSpawnUnit>?[] spawnUnitsByPlayer, int playerIndex)
    {
        if (spawnUnitsByPlayer[playerIndex] is not { } spawnUnits)
        {
            spawnUnits = [];
            spawnUnitsByPlayer[playerIndex] = spawnUnits;
        }

        return spawnUnits;
    }

    private static List<DirectStrikePlayerStats> GetPlayerStats(List<DirectStrikePlayerStats>?[] statsByPlayer, int playerIndex)
    {
        if (statsByPlayer[playerIndex] is not { } stats)
        {
            stats = [];
            statsByPlayer[playerIndex] = stats;
        }

        return stats;
    }

    private static List<int> GetTierUpgrades(List<int>?[] tierUpgradesByPlayer, int playerIndex)
    {
        if (tierUpgradesByPlayer[playerIndex] is not { } tierUpgrades)
        {
            tierUpgrades = [];
            tierUpgradesByPlayer[playerIndex] = tierUpgrades;
        }

        return tierUpgrades;
    }

    private static Dictionary<string, int> GetPlayerUpgrades(Dictionary<string, int>?[] upgradesByPlayer, int playerIndex)
    {
        if (upgradesByPlayer[playerIndex] is not { } upgrades)
        {
            upgrades = new(StringComparer.Ordinal);
            upgradesByPlayer[playerIndex] = upgrades;
        }

        return upgrades;
    }

    private static ReadOnlyCollection<DirectStrikePlayerSpawn> GroupPlayerSpawns(List<TrackedSpawnUnit> spawnUnits, IReadOnlyList<DirectStrikePlayerStats> playerStats)
    {
        List<DirectStrikePlayerSpawn> spawns = [];
        int currentSpawnStartIndex = 0;
        int lastUnitGameloop = 0;
        int spawnNumber = 1;
        int statsIndex = 0;

        for (int i = 0; i < spawnUnits.Count; i++)
        {
            TrackedSpawnUnit spawnUnit = spawnUnits[i];
            if (i == currentSpawnStartIndex)
            {
                lastUnitGameloop = spawnUnit.Gameloop;
                continue;
            }

            if (spawnUnit.Gameloop - lastUnitGameloop > SpawnGroupWindowGameloops)
            {
                spawns.Add(CreatePlayerSpawn(spawnNumber, currentSpawnStartIndex, i - currentSpawnStartIndex, spawnUnits, playerStats, ref statsIndex));
                spawnNumber++;
                currentSpawnStartIndex = i;
            }

            lastUnitGameloop = spawnUnit.Gameloop;
        }

        if (spawnUnits.Count > 0)
        {
            spawns.Add(CreatePlayerSpawn(spawnNumber, currentSpawnStartIndex, spawnUnits.Count - currentSpawnStartIndex, spawnUnits, playerStats, ref statsIndex));
        }

        return spawns.AsReadOnly();
    }

    private static DirectStrikePlayerSpawn CreatePlayerSpawn(
        int number,
        int startIndex,
        int count,
        List<TrackedSpawnUnit> spawnUnits,
        IReadOnlyList<DirectStrikePlayerStats> playerStats,
        ref int statsIndex)
    {
        int endIndex = startIndex + count - 1;
        int startGameloop = spawnUnits[startIndex].Gameloop;
        int endGameloop = spawnUnits[endIndex].Gameloop;
        List<DirectStrikeSpawnUnit> units = new(count);
        for (int i = startIndex; i <= endIndex; i++)
        {
            TrackedSpawnUnit unit = spawnUnits[i];
            units.Add(new()
            {
                UnitIndex = unit.UnitIndex,
                Name = unit.Name,
                Gameloop = unit.Gameloop,
                X = unit.Position.X,
                Y = unit.Position.Y,
                DiedGameloop = unit.DiedGameloop,
                DiedX = unit.DiedPosition?.X,
                DiedY = unit.DiedPosition?.Y,
            });
        }

        return new()
        {
            Number = number,
            StartGameloop = startGameloop,
            EndGameloop = endGameloop,
            SummaryStats = GetSummaryStats(playerStats, endGameloop, ref statsIndex),
            Units = units.AsReadOnly(),
        };
    }

    private static DirectStrikePlayerStats? GetSummaryStats(IReadOnlyList<DirectStrikePlayerStats> playerStats, int endGameloop, ref int statsIndex)
    {
        while (statsIndex < playerStats.Count)
        {
            DirectStrikePlayerStats stat = playerStats[statsIndex];
            if (stat.Gameloop >= endGameloop)
            {
                return stat;
            }

            statsIndex++;
        }

        return null;
    }

    private static bool TryGetPlayerLayout(
        DirectStrikePlayerContext[] playerContexts,
        PlayerLayout?[] playerLayouts,
        int playerIndex,
        [NotNullWhen(true)] out DirectStrikePlayer? player,
        [NotNullWhen(true)] out PlayerLayout? playerLayout)
    {
        if ((uint)playerIndex >= (uint)playerContexts.Length)
        {
            player = null;
            playerLayout = null;
            return false;
        }

        player = playerContexts[playerIndex].Player;
        if (playerLayouts[playerIndex] is not { } layout)
        {
            layout = new();
            playerLayouts[playerIndex] = layout;
        }

        playerLayout = layout;
        return true;
    }

    private static bool IsRefineryMinerals(string unitTypeName)
    {
        return unitTypeName.StartsWith("RefineryMinerals", StringComparison.Ordinal)
            || unitTypeName.StartsWith("AssimilatorMinerals", StringComparison.Ordinal)
            || unitTypeName.StartsWith("ExtractorMinerals", StringComparison.Ordinal);
    }

    private static bool TryGetMiddleControlTeam(int upkeepPlayerId, out int team)
    {
        team = upkeepPlayerId switch
        {
            13 => 1,
            14 => 2,
            _ => 0,
        };

        return team != 0;
    }

    private static void SetGamePositions(DirectStrikePlayerContext[] playerContexts, PlayerLayout?[] playerLayouts, Pos planetary)
    {
        List<PlayerLayoutEntry> playersTeam1 = GetLayoutEntries(playerContexts, playerLayouts, 1);
        List<PlayerLayoutEntry> playersTeam2 = GetLayoutEntries(playerContexts, playerLayouts, 2);

        SetTeamGamePositions(playersTeam1, planetary);
        SetTeamGamePositions(playersTeam2, planetary);

        foreach (PlayerLayoutEntry entry in playersTeam2)
        {
            if (entry.Player.GamePos > 0)
            {
                entry.Player.GamePos += 3;
            }
        }
    }

    private static List<PlayerLayoutEntry> GetLayoutEntries(DirectStrikePlayerContext[] playerContexts, PlayerLayout?[] playerLayouts, int teamId)
    {
        List<PlayerLayoutEntry> entries = new(3);
        for (int i = 0; i < playerLayouts.Length; i++)
        {
            PlayerLayout? layout = playerLayouts[i];
            DirectStrikePlayer player = playerContexts[i].Player;
            if (layout is not null && player.TeamId == teamId)
            {
                entries.Add(new(player, layout));
            }
        }

        return entries;
    }

    private static void SetTeamGamePositions(List<PlayerLayoutEntry> teamPlayers, Pos planetary)
    {
        if (teamPlayers.Count == 1)
        {
            teamPlayers[0].Player.GamePos = 1;
        }
        else if (teamPlayers.Count == 2)
        {
            SetTwoPlayerTeamPositions(teamPlayers[0], teamPlayers[1], planetary);
        }
        else if (teamPlayers.Count == 3)
        {
            SetThreePlayerTeamPositions(teamPlayers, planetary);
        }
    }

    private static void SetTwoPlayerTeamPositions(PlayerLayoutEntry player1, PlayerLayoutEntry player2, Pos planetary)
    {
        if (player1.Layout.South is not { } south1 || player2.Layout.South is not { } south2)
        {
            return;
        }

        double d1 = DistanceSquared(planetary, south1);
        double d2 = DistanceSquared(planetary, south2);

        if (d1 > d2)
        {
            player1.Player.GamePos = 1;
            player2.Player.GamePos = 2;
        }
        else if (d2 > d1)
        {
            player1.Player.GamePos = 2;
            player2.Player.GamePos = 1;
        }
    }

    private static void SetThreePlayerTeamPositions(List<PlayerLayoutEntry> teamPlayers, Pos planetary)
    {
        List<PlayerLayoutEntry> playersWithSouth = new(3);
        foreach (PlayerLayoutEntry player in teamPlayers)
        {
            if (player.Layout.South.HasValue)
            {
                playersWithSouth.Add(player);
            }
        }

        if (playersWithSouth.Count != 3)
        {
            return;
        }

        PlayerLayoutEntry middlePlayer = playersWithSouth[0];
        double middleDistance = DistanceSquared(planetary, middlePlayer.Layout.South!.Value);
        for (int i = 1; i < playersWithSouth.Count; i++)
        {
            double distance = DistanceSquared(planetary, playersWithSouth[i].Layout.South!.Value);
            if (distance < middleDistance)
            {
                middlePlayer = playersWithSouth[i];
                middleDistance = distance;
            }
        }

        PlayerLayoutEntry sidePlayer1 = default;
        PlayerLayoutEntry sidePlayer2 = default;
        bool hasSidePlayer1 = false;
        foreach (PlayerLayoutEntry player in playersWithSouth)
        {
            if (!ReferenceEquals(player.Player, middlePlayer.Player))
            {
                if (hasSidePlayer1)
                {
                    sidePlayer2 = player;
                }
                else
                {
                    sidePlayer1 = player;
                    hasSidePlayer1 = true;
                }
            }
        }

        SetThreePlayerSidePositions(middlePlayer, sidePlayer1, sidePlayer2);
    }

    private static void SetThreePlayerSidePositions(PlayerLayoutEntry middlePlayer, PlayerLayoutEntry player1, PlayerLayoutEntry player2)
    {
        if (middlePlayer.Layout.West is not { } middleWest
            || player1.Layout.South is not { } south1
            || player2.Layout.South is not { } south2)
        {
            return;
        }

        double dm1 = DistanceSquared(middleWest, south1);
        double dm2 = DistanceSquared(middleWest, south2);

        if (dm1 < dm2)
        {
            middlePlayer.Player.GamePos = 2;
            player1.Player.GamePos = 1;
            player2.Player.GamePos = 3;
        }
        else if (dm2 < dm1)
        {
            middlePlayer.Player.GamePos = 2;
            player1.Player.GamePos = 3;
            player2.Player.GamePos = 1;
        }
    }

    private static double DistanceSquared(Pos p1, Pos p2)
    {
        double x = p1.X - p2.X;
        double y = p1.Y - p2.Y;

        return (x * x) + (y * y);
    }

    private static TimeSpan ToTimeSpan(int gameloop)
    {
        return TimeSpan.FromSeconds(gameloop / GameLoopsPerSecond);
    }

    private static bool IsGenericRaceCommander(Commander commander)
    {
        return commander is Commander.Protoss or Commander.Terran or Commander.Zerg or Commander.Random;
    }

    private static bool IsNormalizedLevelUpgrade(string upgradeName, Commander commander)
    {
        string? normalizedCommanderPrefix = GetNormalizedCommanderPrefix(commander);
        return normalizedCommanderPrefix is not null && upgradeName.StartsWith(normalizedCommanderPrefix, StringComparison.Ordinal);
    }

    private static string? GetNormalizedCommanderPrefix(Commander commander)
    {
        return commander switch
        {
            Commander.Zagara or Commander.Abathur or Commander.Kerrigan => nameof(Commander.Zerg),
            Commander.Alarak or Commander.Artanis or Commander.Vorazun or Commander.Fenix or Commander.Karax or Commander.Zeratul => nameof(Commander.Protoss),
            Commander.Raynor or Commander.Swann or Commander.Nova or Commander.Stukov => nameof(Commander.Terran),
            Commander.None => nameof(Commander.None),
            Commander.Protoss => nameof(Commander.Protoss),
            Commander.Terran => nameof(Commander.Terran),
            Commander.Zerg => nameof(Commander.Zerg),
            Commander.Dehaka => nameof(Commander.Dehaka),
            Commander.Horner => nameof(Commander.Horner),
            Commander.Mengsk => nameof(Commander.Mengsk),
            Commander.Stetmann => nameof(Commander.Stetmann),
            Commander.Tychus => nameof(Commander.Tychus),
            Commander.Random => nameof(Commander.Random),
            _ => null,
        };
    }

    private sealed class MapLayout
    {
        private static readonly Polygon Team1SpawnArea = new(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        private static readonly Polygon Team2SpawnArea = new(new(84, 93), new(101, 76), new(90, 65), new(73, 82));

        public Pos? Bunker { get; set; }
        public Pos? Cannon { get; set; }
        public Pos? Nexus { get; set; }
        public Pos? Planetary { get; set; }

        public static bool IsSpawnUnit(Pos position, int teamId)
        {
            return teamId switch
            {
                1 => Team1SpawnArea.Contains(position),
                2 => Team2SpawnArea.Contains(position),
                _ => false,
            };
        }
    }

    private sealed class PlayerLayout
    {
        public Pos? East { get; set; }
        public Pos? North { get; set; }
        public Pos? South { get; set; }
        public Pos? West { get; set; }

        public bool TryGetStagingArea([NotNullWhen(true)] out Polygon? stagingArea)
        {
            if (South is { } south && East is { } east && North is { } north && West is { } west)
            {
                stagingArea = new(south, east, north, west);
                return true;
            }

            stagingArea = null;
            return false;
        }
    }

    private readonly struct Polygon(Pos p0, Pos p1, Pos p2, Pos p3)
    {
        private readonly Pos p0 = p0;
        private readonly Pos p1 = p1;
        private readonly Pos p2 = p2;
        private readonly Pos p3 = p3;

        public Polygon(params Pos[] points)
            : this(
                points.Length > 0 ? points[0] : default,
                points.Length > 1 ? points[1] : default,
                points.Length > 2 ? points[2] : default,
                points.Length > 3 ? points[3] : default)
        {
        }

        public bool Contains(Pos position)
        {
            bool inside = false;
            return AddSegment(p3, p0, position, ref inside)
                || AddSegment(p0, p1, position, ref inside)
                || AddSegment(p1, p2, position, ref inside)
                || AddSegment(p2, p3, position, ref inside)
                || inside;
        }

        private static bool AddSegment(Pos start, Pos end, Pos position, ref bool inside)
        {
            if (IsOnSegment(start, end, position))
            {
                return true;
            }

            bool intersects = (end.Y > position.Y) != (start.Y > position.Y)
                && IsLeftOfIntersection(start, end, position);
            if (intersects)
            {
                inside = !inside;
            }

            return false;
        }

        private static bool IsLeftOfIntersection(Pos start, Pos end, Pos position)
        {
            int dy = start.Y - end.Y;
            long left = ((long)position.X - end.X) * dy;
            long right = (long)(start.X - end.X) * (position.Y - end.Y);
            return dy > 0 ? left < right : left > right;
        }

        private static bool IsOnSegment(Pos start, Pos end, Pos position)
        {
            long crossProduct = (((long)position.Y - start.Y) * (end.X - start.X))
                - (((long)position.X - start.X) * (end.Y - start.Y));
            return crossProduct == 0 && position.X >= Math.Min(start.X, end.X)
                && position.X <= Math.Max(start.X, end.X)
                && position.Y >= Math.Min(start.Y, end.Y)
                && position.Y <= Math.Max(start.Y, end.Y);
        }
    }

    private readonly record struct PlayerLayoutEntry(DirectStrikePlayer Player, PlayerLayout Layout);

    private readonly record struct MiddleControlChange(int Gameloop, int Team);

    private readonly record struct OrderedSpawnTrackerEvent(int Gameloop, int Index, SUnitBornEvent? BornEvent, SUnitTypeChangeEvent? TypeChangeEvent);

    private readonly record struct SpawnCandidate(SUnitBornEvent Event, int PlayerIndex, Pos Position);

    private readonly record struct TrackedSpawnUnit(int UnitIndex, string Name, int Gameloop, Pos Position, Pos? DiedPosition, int? DiedGameloop);

    private readonly record struct PlayerContextIndex(int Index, DirectStrikePlayerContext Context);

    private readonly record struct Pos(int X, int Y);
}
