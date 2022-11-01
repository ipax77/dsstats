using pax.dsstats.shared;
using s2protocol.NET.Models;
using System.Numerics;

namespace pax.dsstats.parser;
public static partial class Parse
{
    public static void ParseTrackerEvents(DsReplay replay, TrackerEvents trackerevents)
    {
        SUnitBornEvent? nexusBornEvent = trackerevents.SUnitBornEvents.FirstOrDefault(f => f.UnitTypeName == "ObjectiveNexus");
        SUnitBornEvent? planetaryBornEvent = trackerevents.SUnitBornEvents.FirstOrDefault(f => f.UnitTypeName == "ObjectivePlanetaryFortress");
        SUnitBornEvent? cannonBornEvent = trackerevents.SUnitBornEvents.FirstOrDefault(f => f.UnitTypeName == "ObjectivePhotonCannon");
        SUnitBornEvent? bunkerBornEvent = trackerevents.SUnitBornEvents.FirstOrDefault(f => f.UnitTypeName == "ObjectiveBunker");

        if (nexusBornEvent == null || planetaryBornEvent == null || cannonBornEvent == null || bunkerBornEvent == null) {
            throw new ArgumentNullException(nameof(nexusBornEvent), $"{replay.FileName}");
        }

        SetCannonBunker(replay, cannonBornEvent, bunkerBornEvent);
        SetGameMode(replay, trackerevents.SUpgradeEvents.Where(x => x.UpgradeTypeName.StartsWith("Mutation") || x.UpgradeTypeName.StartsWith("GameMode")).Select(s => s.UpgradeTypeName).ToHashSet());

        SetRaceSpawnAreaTeam(replay, trackerevents.SUnitBornEvents.Where(x => x.Gameloop == 0).ToList());
        SetCenter(replay, nexusBornEvent, planetaryBornEvent);
        SetMiddle(replay, trackerevents.SUnitOwnerChangeEvents);
        SetRefineries(replay,
                      trackerevents.SUnitTypeChangeEvents,
                      trackerevents.SUnitBornEvents.Where(x => x.Gameloop == 0 && x.UnitTypeName.StartsWith("MineralField")));
        SetWinnerTeam(replay, trackerevents.SUnitBornEvents.Where(x => x.UnitTypeName.StartsWith("DeathBurst")).ToList());
        SetSpawns(replay, trackerevents.SUpgradeEvents.Where(x => x.UpgradeTypeName == "StagingAreaNextSpawn").ToList());
        SetStats(replay, trackerevents.SPlayerStatsEvents);
        SetUnits(replay, trackerevents.SUnitBornEvents, planetaryBornEvent, nexusBornEvent);
        SetUpgrades(replay, trackerevents.SUpgradeEvents);
    }

    private static void SetCannonBunker(DsReplay replay, SUnitBornEvent cannonBornEvent, SUnitBornEvent bunkerBornEvent)
    {
        if (cannonBornEvent.SUnitDiedEvent != null) {
            replay.Cannon = cannonBornEvent.SUnitDiedEvent.Gameloop;
        }
        if (bunkerBornEvent.SUnitDiedEvent != null) {
            replay.Bunker = bunkerBornEvent.SUnitDiedEvent.Gameloop;
        }
    }

    private static void SetGameMode(DsReplay replay, HashSet<string> mutations)
    {
        if (mutations.Contains("GameModeTutorial"))
            replay.GameMode = "GameModeTutorial";
        else if (mutations.Contains("GameModeBrawl")) {
            if (mutations.Contains("GameModeCommanders"))
                replay.GameMode = "GameModeBrawlCommanders";
            else
                replay.GameMode = "GameModeBrawl";
        } else if (mutations.Contains("GameModeGear"))
            replay.GameMode = "GameModeGear";
        else if (mutations.Contains("GameModeHeroicCommanders"))
            replay.GameMode = "GameModeHeroicCommanders";
        else if (mutations.Contains("GameModeSabotage"))
            replay.GameMode = "GameModeSabotage";
        else if (mutations.Contains("GameModeSwitch"))
            replay.GameMode = "GameModeSwitch";
        else if (mutations.Contains("MutationCovenant"))
            replay.GameMode = "GameModeSwitch";
        else if (mutations.Contains("MutationEquipment"))
            replay.GameMode = "GameModeGear";
        else if (mutations.Contains("MutationExile")
                && mutations.Contains("MutationRescue")
                && mutations.Contains("MutationShroud"))
            replay.GameMode = "GameModeSabotage";
        else if (mutations.Contains("MutationCommanders")) {
            replay.GameMode = "GameModeCommanders"; // fail safe
            if (mutations.Count == 3 && mutations.Contains("MutationExpansion") && mutations.Contains("MutationOvertime")) replay.GameMode = "GameModeCommandersHeroic";
            else if (mutations.Count == 2 && mutations.Contains("MutationOvertime")) replay.GameMode = "GameModeCommanders";
            else if (mutations.Count >= 3) replay.GameMode = "GameModeBrawlCommanders";
            else if (mutations.Contains("MutationAura")) replay.GameMode = "GameModeBrawlCommanders";
        } else {
            if (replay.GameMode == "unknown" && mutations.Count == 0) replay.GameMode = "GameModeStandard";
            else if (replay.GameMode == "unknown" && mutations.Count > 0) replay.GameMode = "GameModeBrawlStandard";
        }
    }

    private static void SetUpgrades(DsReplay replay, ICollection<SUpgradeEvent> sUpgradeEvents)
    {
        foreach (var upgradeEvent in sUpgradeEvents) {
            if (upgradeEvent.Gameloop < 2) continue;
            if (upgradeEvent.PlayerId < 1) continue;
            if (upgradeEvent.PlayerId > 6) continue;
            if (replay.Players.Count < upgradeEvent.PlayerId) {
                continue;
            }

            if (upgradeEvent.UpgradeTypeName == "MineralIncomeBonus") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("AFKTimer")) continue;
            if (upgradeEvent.UpgradeTypeName == "HighCapacityMode") continue;
            if (upgradeEvent.UpgradeTypeName == "HornerMySignificantOtherBuffHan") continue;
            if (upgradeEvent.UpgradeTypeName == "HornerMySignificantOtherBuffHorner") continue;
            if (upgradeEvent.UpgradeTypeName == "StagingAreaNextSpawn") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Decoration")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Mastery")) continue;
            if (upgradeEvent.UpgradeTypeName == "MineralIncome") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Emote")) continue;
            if (upgradeEvent.UpgradeTypeName == "SpookySkeletonNerf") continue;
            if (upgradeEvent.UpgradeTypeName == "NeosteelFrame") continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Disable")) continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Enable")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Tier")) continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Starlight")) continue;
            if (upgradeEvent.UpgradeTypeName.Contains("Worker")) continue;
            if (upgradeEvent.UpgradeTypeName == "PlayerIsAFK") continue;
            if (upgradeEvent.UpgradeTypeName == "DehakaHeroLevel") continue;
            if (upgradeEvent.UpgradeTypeName == "DehakaSkillPoint") continue;
            if (upgradeEvent.UpgradeTypeName == "DehakaHeroPlaceUsed") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("DehakaCreeperHost")) continue;
            if (upgradeEvent.UpgradeTypeName.Contains("PlaceEvolved")) continue;
            if (upgradeEvent.UpgradeTypeName == "KerriganMutatingCarapaceBonus") continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Modification")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Blacklist")) continue;
            if (upgradeEvent.UpgradeTypeName == "TychusTychusPlaced") continue;
            if (upgradeEvent.UpgradeTypeName == "TychusFirstOnesontheHouse") continue;
            if (upgradeEvent.UpgradeTypeName == "ClolarionInterdictorsBonus") continue;
            if (upgradeEvent.UpgradeTypeName == "PartyFrameHide") continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Bonus")) continue;
            if (upgradeEvent.UpgradeTypeName == "FenixUnlock") continue;
            if (upgradeEvent.UpgradeTypeName == "FenixExperienceAwarded") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("RaynorCostReduced")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Theme")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Worker")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("AreaFlair")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("AreaWeather")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("Aura")) continue;
            if (upgradeEvent.UpgradeTypeName == "HideWorkerCommandCard") continue;
            if (upgradeEvent.UpgradeTypeName == "UsingVespeneIncapableWorker") continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("PowerField")) continue;
            if (upgradeEvent.UpgradeTypeName.EndsWith("Bonus10")) continue;
            if (upgradeEvent.UpgradeTypeName == "DehakaPrimalWurm") continue;
            if (upgradeEvent.UpgradeTypeName.Contains("Skin")) continue;
            if (upgradeEvent.UpgradeTypeName.StartsWith("DehakaHeroLevel")) continue;
            if (upgradeEvent.UpgradeTypeName == "NovaRifle") continue;
            if (upgradeEvent.UpgradeTypeName == "NovaShotgun") continue;


            DsPlayer dsPlayer = replay.Players[upgradeEvent.PlayerId - 1];

            if (upgradeEvent.UpgradeTypeName.Contains("Level")) {
                string urace = dsPlayer.Race;
                if (dsPlayer.Race == "Zagara" || dsPlayer.Race == "Abathur" || dsPlayer.Race == "Kerrigan")
                    urace = "Zerg";
                else if (dsPlayer.Race == "Alarak" || dsPlayer.Race == "Artanis" || dsPlayer.Race == "Vorazun" || dsPlayer.Race == "Fenix" || dsPlayer.Race == "Karax" || dsPlayer.Race == "Zeratul")
                    urace = "Protoss";
                else if (dsPlayer.Race == "Raynor" || dsPlayer.Race == "Swann" || dsPlayer.Race == "Nova" || dsPlayer.Race == "Stukov")
                    urace = "Terran";
                if (!upgradeEvent.UpgradeTypeName.StartsWith(urace)) continue;
            }

            dsPlayer.Upgrades.Add(new DsUpgrade() {
                Gameloop = upgradeEvent.Gameloop,
                Upgrade = upgradeEvent.UpgradeTypeName,
                Count = upgradeEvent.Count
            });
        }
    }

    private static void SetRaceSpawnAreaTeam(DsReplay replay, List<SUnitBornEvent> zeroBornEvents)
    {
        foreach (var bornEvent in zeroBornEvents) {
            if (replay.Players.Count < bornEvent.ControlPlayerId) {
                continue;
            }

            if (bornEvent.UnitTypeName == "StagingAreaFootprintSouth" || bornEvent.UnitTypeName == "AreaMarkerSouth") {
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[0] = bornEvent.X;
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[1] = bornEvent.Y;

                var distance = Vector2.DistanceSquared(new Vector2(bornEvent.X, 0), new Vector2(bornEvent.X, bornEvent.Y));
                if (distance > 10000) {
                    replay.Players[bornEvent.ControlPlayerId - 1].Team = 1;
                } else {
                    replay.Players[bornEvent.ControlPlayerId - 1].Team = 2;
                }
            } else if (bornEvent.UnitTypeName == "StagingAreaFootprintWest" || bornEvent.UnitTypeName == "AreaMarkerWest") {
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[2] = bornEvent.X;
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[3] = bornEvent.Y;
            } else if (bornEvent.UnitTypeName == "StagingAreaFootprintNorth" || bornEvent.UnitTypeName == "AreaMarkerNorth") {
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[4] = bornEvent.X;
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[5] = bornEvent.Y;
            } else if (bornEvent.UnitTypeName == "StagingAreaFootprintEast" || bornEvent.UnitTypeName == "AreaMarkerEast") {
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[6] = bornEvent.X;
                replay.Players[bornEvent.ControlPlayerId - 1].SpawnArea[7] = bornEvent.Y;
            } else if (bornEvent.UnitTypeName.StartsWith("Worker")) {

                replay.Players[bornEvent.ControlPlayerId - 1].Race = bornEvent.UnitTypeName[6..];
            }
        }
    }

    private static void SetUnits(DsReplay replay, ICollection<SUnitBornEvent> unitBornEvents, SUnitBornEvent planetaryBornEvent, SUnitBornEvent nexusBornEvent)
    {
        foreach (var bornEvent in unitBornEvents) {
            if (bornEvent.Gameloop == 0) continue;
            if (bornEvent.ControlPlayerId < 1) continue;
            if (bornEvent.ControlPlayerId > 6) continue;
            if (replay.Players.Count < bornEvent.ControlPlayerId) {
                continue;
            }

            if (IgnoreUnits.Contains(bornEvent.UnitTypeName)) continue;
            if (DefensiveUnits.Contains(bornEvent.UnitTypeName)) continue;

            var dsPlayer = replay.Players[bornEvent.ControlPlayerId - 1];

            if (bornEvent.UnitTypeName == "Tier2Dummy" || bornEvent.UnitTypeName == "Tier3Dummy" || bornEvent.UnitTypeName == "Tier4Dummy") {
                dsPlayer.TierUpgrades.Add(bornEvent.Gameloop);
                continue;
            }

            if (bornEvent.UnitTypeName.EndsWith("Dummy") || bornEvent.UnitTypeName.EndsWith("Item")) {
                continue;
            }

            if (bornEvent.UnitTypeName.StartsWith("Trophy") || bornEvent.UnitTypeName.StartsWith("Decoration")) {
                continue;
            }

            if (bornEvent.UnitTypeName.StartsWith("Worker")) {
                dsPlayer.Race = bornEvent.UnitTypeName[6..];
                continue;
            }

            var unit = new DsUnit() {
                Index = bornEvent.UnitIndex,
                Name = FixUnitName(bornEvent.UnitTypeName, dsPlayer.Race),
                Gameloop = bornEvent.Gameloop,
                Position = new Position() { X = bornEvent.X, Y = bornEvent.Y },
                BuildArea = CheckSquare(dsPlayer.SpawnArea, bornEvent.X, bornEvent.Y),
            };

            if (!unit.BuildArea) {
                if (CheckSpawnUnit(replay, dsPlayer, bornEvent)) {
                    continue;
                }

                if (dsPlayer.GamePos == 0) {
                    SetGamePos(replay, dsPlayer, unit);
                }

                if (bornEvent.SUnitDiedEvent != null) {
                    unit.KillerPlayer = bornEvent.SUnitDiedEvent.KillerPlayerId == null ? 0 : bornEvent.SUnitDiedEvent.KillerPlayerId.Value;

                    if (replay.Players.Count >= unit.KillerPlayer) {
                        unit.KillerUnit = bornEvent.SUnitDiedEvent.KillerUnitBornEvent == null
                            ? null :
                            FixUnitName(bornEvent.SUnitDiedEvent.KillerUnitBornEvent.UnitTypeName,
                                        unit.KillerPlayer > 0 && unit.KillerPlayer < 7 ? replay.Players[unit.KillerPlayer - 1].Race : null);
                    }

                    unit.DiedPosition = new Position() { X = bornEvent.SUnitDiedEvent.X, Y = bornEvent.SUnitDiedEvent.Y };
                    unit.DiedGameloop = bornEvent.SUnitDiedEvent.Gameloop;
                }

                // unit.DsUnitData = DsUnitDatas.FirstOrDefault(f => f.Race == dsPlayer.Race && f.Name.Contains(unit.Name));
                //if (unit.DsUnitData == null)
                //{
                //    Console.WriteLine($"could not find unitdata for {unit.Name}|{dsPlayer.Race}");
                //}

                if (!dsPlayer.HasSpawns) {
                    var lastUnit = dsPlayer.Units.LastOrDefault();
                    if (lastUnit == null) {
                        dsPlayer.Spawns.Add(new DsSpawns() { Gameloop = unit.Gameloop });
                    } else if (unit.Gameloop - lastUnit.Gameloop > 400) {
                        dsPlayer.Spawns.Add(new DsSpawns() { Gameloop = unit.Gameloop });
                    }
                }
            } else {
                dsPlayer.Units.Add(unit);
            }
        }
    }

    private static void SetGamePos(DsReplay replay, DsPlayer dsPlayer, DsUnit unit)
    {
        int pos = 0;
        if (replay.Players.Count <= 2) {
            pos = 1;
        } else if ((unit.Gameloop - 480) % 1440 == 0
              || (unit.Gameloop - 481) % 1440 == 0
              || (unit.Gameloop - 482) % 1440 == 0
              || (unit.Gameloop - 483) % 1440 == 0
              || (unit.Gameloop - 484) % 1440 == 0) {
            pos = 1;
        } else if ((unit.Gameloop - 960) % 1440 == 0
              || (unit.Gameloop - 961) % 1440 == 0
              || (unit.Gameloop - 962) % 1440 == 0
              || (unit.Gameloop - 963) % 1440 == 0
              || (unit.Gameloop - 964) % 1440 == 0) {
            pos = 2;
        } else if ((unit.Gameloop - 1440) % 1440 == 0
              || (unit.Gameloop - 1441) % 1440 == 0
              || (unit.Gameloop - 1442) % 1440 == 0
              || (unit.Gameloop - 1443) % 1440 == 0
              || (unit.Gameloop - 1444) % 1440 == 0) {
            pos = 3;
        }

        if (replay.Players.Count == 4 && pos == 3) {
            pos = 1;
        }

        if (pos > 0) {
            dsPlayer.GamePos = dsPlayer.Team == 1 ? pos : pos + 3;
        }
    }

    private static void SetSpawns(DsReplay replay, List<SUpgradeEvent> spawnEvents)
    {
        foreach (var spawn in spawnEvents.Where(x => x.PlayerId != 0 && x.PlayerId <= 6 && x.Gameloop >= 480)) {
            if (spawn.Count == -1) {
                if (replay.Players.Count < spawn.PlayerId) {
                    continue;
                }

                replay.Players[spawn.PlayerId - 1].Spawns.Add(new DsSpawns() {
                    Gameloop = spawn.Gameloop
                });
            }
        }

        replay.Players.Where(x => x.Spawns.Any()).ToList().ForEach(x => x.HasSpawns = true);
    }

    private static void SetWinnerTeam(DsReplay replay, IEnumerable<SUnitBornEvent> deathBurstEvents)
    {
        var burstEvent = deathBurstEvents.FirstOrDefault();
        if (burstEvent != null) {
            replay.WinnerTeam = burstEvent.ControlPlayerId == 13 ? 2 : 1;
            replay.Duration = burstEvent.Gameloop;
        }
    }

    private static void SetStats(DsReplay replay, ICollection<SPlayerStatsEvent> sPlayerStatsEvents)
    {
        foreach (var stat in sPlayerStatsEvents) {
            if (replay.Duration > 0 && stat.Gameloop > replay.Duration) {
                continue;
            }

            if (replay.Players.Count < stat.PlayerId) {
                continue;
            }

            var player = replay.Players[stat.PlayerId - 1];

            var lastSpawn = player.Spawns.LastOrDefault(f => f.Gameloop < stat.Gameloop);
            var lastStat = player.Stats.LastOrDefault();
            int army = lastStat?.Army ?? 0;
            int lastActiveForces = lastStat?.MineralsUsedActiveForces ?? 0;

            if (lastSpawn != null && stat.Gameloop - lastSpawn.Gameloop <= 160) {
                army = stat.MineralsUsedActiveForces - lastActiveForces;
                player.Army += army;
            }

            player.Stats.Add(new DsStats() {
                Gameloop = stat.Gameloop,
                FoodUsed = stat.FoodUsed,
                MineralsCollectionRate = stat.MineralsCollectionRate,
                MineralsCurrent = stat.MineralsCurrent,
                MineralsFriendlyFireArmy = stat.MineralsFriendlyFireArmy,
                MineralsFriendlyFireTechnology = stat.MineralsFriendlyFireTechnology,
                MineralsKilledArmy = stat.MineralsKilledArmy,
                MineralsLostArmy = stat.MineralsLostArmy,
                MineralsUsedActiveForces = stat.MineralsUsedActiveForces,
                MineralsUsedCurrentArmy = stat.MineralsUsedCurrentArmy,
                MineralsUsedCurrentTechnology = stat.MineralsUsedCurrentTechnology,
                Army = army
            });
            if (stat.MineralsCollectionRate > 0) {
                player.Duration = stat.Gameloop;
            }
        }
    }

    private static void SetRefineries(DsReplay replay, ICollection<SUnitTypeChangeEvent> sUnitTypeChangeEvents, IEnumerable<SUnitBornEvent> mineralBornEvents)
    {
        foreach (SUnitTypeChangeEvent changeEvent in sUnitTypeChangeEvents) {
            if (changeEvent.UnitTypeName.StartsWith("RefineryMinerals")
                || changeEvent.UnitTypeName.StartsWith("AssimilatorMinerals")
                || changeEvent.UnitTypeName.StartsWith("ExtractorMinerals")) {
                var refinery = mineralBornEvents.FirstOrDefault(f => f.UnitTagIndex == changeEvent.UnitTagIndex && f.UnitTagRecycle == changeEvent.UnitTagRecycle);
                if (refinery != null) {
                    if (replay.Players.Count < refinery.ControlPlayerId) {
                        continue;
                    }

                    replay.Players[refinery.ControlPlayerId - 1].Refineries.Add(new DsRefinery() {
                        Gameloop = changeEvent.Gameloop
                    });
                }
            }
        }
    }

    private static void SetMiddle(DsReplay replay, ICollection<SUnitOwnerChangeEvent> sUnitOwnerChangeEvents)
    {
        foreach (var changeEvent in sUnitOwnerChangeEvents.Where(x => x.UnitTagIndex == 20)) {
            int team = 0;
            if (changeEvent.UpkeepPlayerId == 13)
                team = 1;
            else if (changeEvent.UpkeepPlayerId == 14)
                team = 2;
            replay.Middles.Add(new DsMiddle() {
                Gameloop = changeEvent.Gameloop,
                Team = team,
            });
        }
    }

    private static int GetTeam(int x, int y)
    {
        if (CheckSquare(AreaTeam1, x, y)) {
            return 1;
        }
        if (CheckSquare(AreaTeam2, x, y)) {
            return 2;
        }
        return 0;
    }

    private static bool CheckSpawnUnit(DsReplay replay, DsPlayer player, SUnitBornEvent bornEvent)
    {
        var line = player.Team == 1 ? replay.LineTeam1 : replay.LineTeam2;
        var distance = (bornEvent.X - line[0]) * (line[3] - line[1]) - (bornEvent.Y - line[1]) * (line[2] - line[0]);
        return player.Team == 1 ? distance > 0 : distance < 0;
    }

    private static void SetCenter(DsReplay replay, SUnitBornEvent nexusBornEvent, SUnitBornEvent planetaryBornEvent)
    {
        replay.Center = new Position() { X = (nexusBornEvent.X + planetaryBornEvent.X) / 2, Y = (nexusBornEvent.Y + planetaryBornEvent.Y) / 2 };
        float x1t1 = planetaryBornEvent.X + MathF.Cos(135 * MathF.PI / 180) * 100;
        float y1t1 = planetaryBornEvent.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
        float x2t1 = planetaryBornEvent.X + MathF.Cos(315 * MathF.PI / 180) * 100;
        float y2t1 = planetaryBornEvent.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

        replay.LineTeam1 = new float[4] { x1t1, y1t1, x2t1, y2t1 };

        float x1t2 = nexusBornEvent.X + MathF.Cos(135 * MathF.PI / 180) * 100;
        float y1t2 = nexusBornEvent.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
        float x2t2 = nexusBornEvent.X + MathF.Cos(315 * MathF.PI / 180) * 100;
        float y2t2 = nexusBornEvent.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

        replay.LineTeam2 = new float[4] { x1t2, y1t2, x2t2, y2t2 };
    }
}
