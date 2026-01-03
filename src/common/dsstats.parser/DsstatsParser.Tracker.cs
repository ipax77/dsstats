
using dsstats.shared;
using s2protocol.NET.Models;
using System.Numerics;

namespace dsstats.parser;

public static partial class DsstatsParser
{
    private static void ParseTrackerEvents(TrackerEvents? trackerEvents, DsstatsReplay replay)
    {
        if (trackerEvents is null)
        {
            return;
        }
        var playerDict = GetPlayerDict(trackerEvents.SPlayerSetupEvents, replay);
        var mapLayout = new MapLayout(playerDict);

        foreach (var ent in trackerEvents.SUnitBornEvents)
        {
            if (ent.Gameloop == 0)
            {
                CheckZeroGameloopBornEvent(ent, mapLayout, replay);
            }
            else
            {
                mapLayout.IsReady();
                if (!playerDict.TryGetValue(ent.ControlPlayerId, out var player)
                    || player is null)
                {
                    continue;
                }

                if (ent.Gameloop <= 1440 && ent.UnitTypeName.StartsWith("Worker"))
                {
                    player.Race = ParseRace(ent.UnitTypeName[6..]);
                    player.RaceInGameSelected = ent.Gameloop;
                    continue;
                }

                if (FilterUnits(ent.UnitTypeName))
                {
                    continue;
                }

                Pos unitPos = new(ent.X, ent.Y);

                if (mapLayout.IsSpawnUnit(unitPos, player.TeamId))
                {
                    var unit = new DsUnit()
                    {
                        Index = ent.UnitIndex,
                        Name = ent.UnitTypeName,
                        Gameloop = ent.Gameloop,
                        Position = unitPos,
                        DiedPosition = ent.SUnitDiedEvent != null ? new(ent.SUnitDiedEvent.X, ent.SUnitDiedEvent.Y) : Pos.Zero
                    };
                    player.Units.Add(unit);
                }
            }
        }

        foreach (var ent in trackerEvents.SUnitTypeChangeEvents)
        {
            if (ent.UnitTypeName.StartsWith("RefineryMinerals")
                || ent.UnitTypeName.StartsWith("AssimilatorMinerals")
                || ent.UnitTypeName.StartsWith("ExtractorMinerals"))
            {
                var refinery = playerDict.Values.SelectMany(m => m.Refineries)
                    .FirstOrDefault(f => f.UnitTagIndex == ent.UnitTagIndex && f.UnitTagRecyle == ent.UnitTagRecycle);
                if (refinery != null)
                {
                    refinery.Gameloop = ent.Gameloop;
                    refinery.Taken = true;
                }
            }
        }

        foreach (var ent in trackerEvents.SPlayerStatsEvents)
        {
            if (!playerDict.TryGetValue(ent.PlayerId, out var player)
                || player is null)
            {
                continue;
            }
            player.Stats.Add(new()
            {
                Gameloop = ent.Gameloop,
                MineralsCollectionRate = ent.MineralsCollectionRate,
                MineralsUsedActiveForces = ent.MineralsUsedActiveForces,
                MineralsUsedCurrentTechnology = ent.MineralsUsedCurrentTechnology,
                MineralsKilledArmy = ent.MineralsKilledArmy,
            });
            if (ent.MineralsCollectionRate > 0)
            {
                player.Duration = ent.Gameloop;
            }
        }

        foreach (var ent in trackerEvents.SUnitOwnerChangeEvents)
        {
            if (ent.UnitTagIndex != 20)
            {
                continue;
            }
            int team;
            if (ent.UpkeepPlayerId == 13)
            {
                team = 1;
            }
            else if (ent.UpkeepPlayerId == 14)
            {
                team = 2;
            }
            else
            {
                continue;
            }
            replay.MiddleChanges.Add(new()
            {
                Gameloop = ent.Gameloop,
                ControlTeam = team
            });
        }

        foreach (var ent in trackerEvents.SUpgradeEvents)
        {
            if (ent.Gameloop == 0)
            {
                CheckZeroGameloopUpgradeEvent(ent, replay);
            }
            else
            {
                if (!playerDict.TryGetValue(ent.PlayerId, out var player) || player is null)
                    continue;

                // Handle tier upgrades
                if (ent.UpgradeTypeName is "Tier2" or "Tier3")
                {
                    player.TierUpgrades.Add(ent.Gameloop);
                    continue;
                }

                // Skip unwanted upgrades
                if (FilterUpgrades(ent.UpgradeTypeName))
                    continue;

                // Skip non-matching level upgrades
                if (ent.UpgradeTypeName.Contains("Level") && !IsNormalizedLevelUpgrade(ent.UpgradeTypeName, player.Race))
                    continue;

                // Record valid upgrade
                player.Upgrades[ent.UpgradeTypeName] = ent.Gameloop;
            }
        }
    }

    private static void CheckZeroGameloopUpgradeEvent(SUpgradeEvent ent, DsstatsReplay replay)
    {
        if (ent.UpgradeTypeName.StartsWith("Mutation") || ent.UpgradeTypeName.StartsWith("GameMode"))
        {
            replay.Modes.Add(ent.UpgradeTypeName);
        }
    }

    private static void CheckZeroGameloopBornEvent(SUnitBornEvent ent, MapLayout mapLayout, DsstatsReplay replay)
    {
        // Build Pos object from event (assuming ent has X and Y)
        mapLayout.Players.TryGetValue(ent.ControlPlayerId, out var player);

        var pos = new Pos(ent.X, ent.Y);

        if (player is not null)
        {
            if (ent.UnitTypeName.StartsWith("MineralField"))
            {
                player.Refineries.Add(new()
                {
                    UnitTagIndex = ent.UnitTagIndex,
                    UnitTagRecyle = ent.UnitTagRecycle
                });
            }
            else
            {
                switch (ent.UnitTypeName)
                {
                    // Player staging areas
                    case "StagingAreaFootprintSouth":
                    case "AreaMarkerSouth":
                        player.Layout.South = pos;
                        var distance = Vector2.DistanceSquared(new Vector2(ent.X, 0), new Vector2(ent.X, ent.Y));
                        player.TeamId = distance > 10000 ? 1 : 2;
                        break;

                    case "StagingAreaFootprintWest":
                    case "AreaMarkerWest":
                        player.Layout.West = pos;
                        break;

                    case "StagingAreaFootprintNorth":
                    case "AreaMarkerNorth":
                        player.Layout.North = pos;
                        break;

                    case "StagingAreaFootprintEast":
                    case "AreaMarkerEast":
                        player.Layout.East = pos;
                        break;

                    // Worker spawn reveals the race
                    default:
                        if (ent.UnitTypeName.StartsWith("Worker"))
                        {
                            player.Race = ParseRace(ent.UnitTypeName[6..]);
                        }
                        break;
                }
            }
        }
        else
        {
            switch (ent.UnitTypeName)
            {
                // Objectives
                case "ObjectiveNexus":
                    mapLayout.Nexus = pos;
                    if (ent.SUnitDiedEvent != null)
                    {
                        replay.Duration = ent.SUnitDiedEvent.Gameloop;
                        replay.WinnerTeam = 1;
                    }
                    break;

                case "ObjectivePlanetaryFortress":
                    mapLayout.Planetary = pos;
                    if (ent.SUnitDiedEvent != null)
                    {
                        replay.Duration = ent.SUnitDiedEvent.Gameloop;
                        replay.WinnerTeam = 2;
                    }
                    break;

                case "ObjectivePhotonCannon":
                    mapLayout.Cannon = pos;
                    if (ent.SUnitDiedEvent != null)
                    {
                        replay.Cannon = ent.SUnitDiedEvent.Gameloop;
                    }
                    break;

                case "ObjectiveBunker":
                    mapLayout.Bunker = pos;
                    if (ent.SUnitDiedEvent != null)
                    {
                        replay.Bunker = ent.SUnitDiedEvent.Gameloop;
                    }
                    break;
            }
        }
    }

    private static Dictionary<int, DsPlayer> GetPlayerDict_(ICollection<SPlayerSetupEvent> setupEvents,
                                                           DsstatsReplay replay)
    {
        var playerDict = new Dictionary<int, DsPlayer>();
        bool legacyMode = replay.Players.All(p => p.WorkingSetSlotId == 0);

        foreach (var setupEvent in setupEvents)
        {
            if (setupEvent.PlayerId == 0)
                continue;

            DsPlayer? player = null;
            if (legacyMode)
            {
                // Fall back: index+1 mapping
                int index = setupEvent.PlayerId - 1;
                if (index >= 0 && index < replay.Players.Count)
                    player = replay.Players[index];
            }
            else
            {
                // Modern replays: SlotId matches WorkingSetSlotId
                player = replay.Players
                    .FirstOrDefault(p => p.Observe == 0 && p.WorkingSetSlotId == setupEvent.SlotId);
            }

            if (player is null)
                throw new InvalidOperationException(
                    $"No player found for SlotId {setupEvent.SlotId} (PlayerId {setupEvent.PlayerId})");

            if (!playerDict.ContainsKey(setupEvent.PlayerId))
                playerDict[setupEvent.PlayerId] = player;
        }

        return playerDict;
    }

    private static Dictionary<int, DsPlayer> GetPlayerDict(ICollection<SPlayerSetupEvent> setupEvents,
                                                           DsstatsReplay replay)
    {

        var playerIds = setupEvents.Select(x => x.PlayerId).OrderBy(o => o).ToList();
        var playerPos = replay.Players.Select(s => s.PlayerId).OrderBy(o => o).ToList();
        var playerMetadataIds = replay.Players.Select(s => s.MetadataPlayerId).OrderBy(o => o).ToList();

        if (playerIds.SequenceEqual(playerPos))
        {
            return replay.Players.ToDictionary(k => k.PlayerId, v => v);
        }
        else if (playerIds.SequenceEqual(playerMetadataIds))
        {
            return replay.Players.ToDictionary(k => k.MetadataPlayerId, v => v);
        }
        else
        {
            // try workingsetSlotIds + 1
            var workingsetSlotIdsIncremented = replay.Players.Select(s => s.WorkingSetSlotId + 1).OrderBy(o => o).ToList();
            if (playerIds.SequenceEqual(workingsetSlotIdsIncremented))
            {
                return replay.Players.ToDictionary(k => k.WorkingSetSlotId + 1, v => v);

            }

            // try workingsetSlotIds
            var workingsetSlotIds = replay.Players.Select(s => s.WorkingSetSlotId).OrderBy(o => o).ToList();
            if (playerIds.SequenceEqual(workingsetSlotIds))
            {
                return replay.Players.ToDictionary(k => k.WorkingSetSlotId, v => v);
            }

            // try workingsetSlotIds with 0 + 1
            var workingsetSlotIdsWithZeroToOne = GetZeroToOneAdjustedWorkingsetSlotIds(replay.Players);
            if (playerIds.SequenceEqual(workingsetSlotIdsWithZeroToOne))
            {
                return replay.Players.ToDictionary(k => k.WorkingSetSlotId == 0 ? 1 : k.WorkingSetSlotId, v => v);
            }

            // 2 player by order
            if (playerIds.Count == 2 && playerPos.Count == 2)
            {
                return new Dictionary<int, DsPlayer>
                {
                    { playerIds[0], replay.Players[0] },
                    { playerIds[1], replay.Players[1] }
                };
            }

            throw new ArgumentNullException(nameof(setupEvents));
        }
    }

    private static List<int> GetZeroToOneAdjustedWorkingsetSlotIds(List<DsPlayer> players)
    {
        HashSet<int> ids = new();
        foreach (var player in players.OrderBy(o => o.WorkingSetSlotId))
        {
            if (player.WorkingSetSlotId == 0)
            {
                ids.Add(1);
            }
            else
            {
                ids.Add(player.WorkingSetSlotId);
            }
        }
        return ids.ToList();
    }

    private static readonly HashSet<string> IgnoreUnits = new()
    {
        "Interceptor",
        "Worker",
        "Decoration",
        "Broodling",
        "BroodlingEscort",
        "BroodlingEscortStetmann",
        "BroodlingStukov",
        "ReaperLD9ClusterCharges",
        "KD8Charge",
        "Trophy",
        "StrikeWeaponry",
        "AdeptPhaseShift",
        "InfestedLiberatorViralSwarm",
        "Locust",
        "Railgun",
        "PrimalWurm",
        "UED",
        "TrainDummy",
        "ReaverStarlightDummy",
        "AiurCarrierRepairDrone",
        "StukovAleksanderPlaceholder",
        "AssaultDrone",
        "AutoTurret",
        "BioMechanicalRepairDrone",
        "Biomass",
        "ClolarionBomber",
        "CreepTumorBurrowed",
        "CreeperHostExplosiveCreeper",
        "DehakaCreeperHostExplosiveCreeper",
        "DisruptorPhased",
        "GuardianShell",
        "HornerAssaultDrone",
        "RaynorHyperionPointDefenseDrone",
        "InfestedTerransEgg",
        "InvisibleTargetDummy",
        "LurkerStetmannBurrowed",
        "LocustMP",
        "ParasiticBombDummy",
        "ParasiticBombRelayDummy",
        "PrimalHostLocust",
        "PurifierAdeptShade",
        "PurifierTalisShade",
        "TychusRattlesnakeDeployRevitalizer",
        "SNARE_PLACEHOLDER",
        "TychusSiriusWarhoundTurret",
        "SpiderMine",
        "SplitterlingSpawn",
        "SprayDecal",
        "Tier2Dummy",
        "Tier3Dummy",
        "Tier4Dummy",
        "BelShirSapphire",
        "CharAgate",
        "InfestedDiamondbackSnarePlaceholder",
        "TerrazineAmethyst",
        "TridentMissiles",
        "UnitBirthBar",
    };


    private static bool FilterUnits(string name)
    {
        return IgnoreUnits.Contains(name);
    }


    private static readonly HashSet<string> ExactMatches = new()
    {
        "MineralIncomeBonus",
        "HighCapacityMode",
        "HornerMySignificantOtherBuffHan",
        "HornerMySignificantOtherBuffHorner",
        "StagingAreaNextSpawn",
        "MineralIncome",
        "SpookySkeletonNerf",
        "NeosteelFrame",
        "PlayerIsAFK",
        "DehakaHeroLevel",
        "DehakaSkillPoint",
        "DehakaHeroPlaceUsed",
        "KerriganMutatingCarapaceBonus",
        "TychusTychusPlaced",
        "TychusFirstOnesontheHouse",
        "ClolarionInterdictorsBonus",
        "PartyFrameHide",
        "FenixUnlock",
        "FenixExperienceAwarded",
        "HideWorkerCommandCard",
        "UsingVespeneIncapableWorker",
        "DehakaPrimalWurm"
    };

    private static readonly string[] StartsWithPatterns = new[]
    {
        "AFKTimer",
        "Decoration",
        "Mastery",
        "Emote",
        "Tier",
        "DehakaCreeperHost",
        "Blacklist",
        "RaynorCostReduced",
        "Theme",
        "Worker",
        "AreaFlair",
        "AreaWeather",
        "Aura",
        "PowerField"
    };

    private static readonly string[] EndsWithPatterns = new[]
    {
        "Disable",
        "Enable",
        "Starlight",
        "Modification",
        "Bonus",
        "Bonus10"
    };

    private static readonly string[] ContainsPatterns = new[]
    {
        "Worker",
        "PlaceEvolved"
    };

    private static bool FilterUpgrades(string upgradeName)
    {
        if (ExactMatches.Contains(upgradeName))
            return true;

        if (StartsWithPatterns.Any(upgradeName.StartsWith))
            return true;

        if (EndsWithPatterns.Any(upgradeName.EndsWith))
            return true;

        if (ContainsPatterns.Any(upgradeName.Contains))
            return true;

        return false;
    }

    private static readonly Dictionary<Commander, Commander> HeroToDefaultRace = new()
    {
        { Commander.Zagara, Commander.Zerg },
        { Commander.Abathur, Commander.Zerg },
        { Commander.Kerrigan, Commander.Zerg },
        { Commander.Alarak, Commander.Protoss },
        { Commander.Artanis, Commander.Protoss },
        { Commander.Vorazun, Commander.Protoss },
        { Commander.Fenix, Commander.Protoss },
        { Commander.Karax, Commander.Protoss },
        { Commander.Zeratul, Commander.Protoss },
        { Commander.Raynor, Commander.Terran },
        { Commander.Swann, Commander.Terran },
        { Commander.Nova, Commander.Terran },
        { Commander.Stukov, Commander.Terran }
    };

    private static bool IsNormalizedLevelUpgrade(string upgradeName, Commander race)
        => upgradeName.StartsWith(HeroToDefaultRace.GetValueOrDefault(race, race).ToString());

}

