using dsstats.shared;
using System.Collections.Frozen;

namespace dsstats.db8services.DsData;

public partial class DsDataService
{
    private readonly FrozenDictionary<UnitMapKey, string> unitMap = new Dictionary<UnitMapKey, string>()
    {
        { new("VileRoach", Commander.Abathur), "Vile Roach" },
        { new("SwarmQueen", Commander.Abathur), "Swarm Queen" },
        { new("SwarmHost", Commander.Abathur), "Swarm Host" },
        { new("WarPrism", Commander.Alarak), "War Prism" },
        { new("MothershipTaldarim", Commander.Alarak), "Tal'darim Mothership" },
        { new("HonorGuard", Commander.Artanis), "Honor Guard" },
        { new("HighArchon", Commander.Artanis), "High Archon" },
        { new("PurifierTempest", Commander.Artanis), "Purifier Tempest" },
        { new("HighTemplar", Commander.Artanis), "High Templar" },
        { new("PrimalHydralisk", Commander.Dehaka), "Primal Hydralisk" },
        { new("PrimalRavasaur", Commander.Dehaka), "Ravasaur" },
        { new("PrimalIgniter", Commander.Dehaka), "Primal Igniter" },
        { new("PrimalMutalisk", Commander.Dehaka), "Primal Mutalisk" },
        { new("CreeperHost", Commander.Dehaka), "Creeper Host" },
        { new("PrimalZergling", Commander.Dehaka), "Primal Zergling" },
        { new("PrimalRoach", Commander.Dehaka), "Primal Roach" },
        { new("PrimalUltralisk", Commander.Dehaka), "Primal Ultralisk" },
        { new("PrimalGuardian", Commander.Dehaka), "Primal Guardian" },
        { new("PrimalHost", Commander.Dehaka), "Primal Host" },
        { new("PurifierAdept", Commander.Fenix), "Adept" },
        { new("PurifierImmortal", Commander.Fenix), "Immortal" },
        { new("PurifierTalis", Commander.Fenix), "Talis" },
        { new("PurifierObserver", Commander.Fenix), "Observer" },
        { new("PurifierScout", Commander.Fenix), "Scout" },
        { new("Walker", Commander.Fenix), "Fenix - Dragoon" },
        { new("PurifierColossus", Commander.Fenix), "Colossus" },
        { new("PurifierCarrier", Commander.Fenix), "Carrier" },
        { new("Praetor", Commander.Fenix), "Fenix - Praetor" },
        { new("Flyer", Commander.Fenix), "Fenix - Arbiter" },
        { new("PurifierDisruptor", Commander.Fenix), "Disruptor" },
        { new("TheiaRaven", Commander.Horner), "Theia Raven" },
        { new("DeimosViking", Commander.Horner), "Deimos Viking" },
        { new("AssaultGalleon", Commander.Horner), "Assault Galleon" },
        { new("AsteriaWraith", Commander.Horner), "Asteria Wraith" },
        { new("WidowMine", Commander.Horner), "Widow Mine" },
        { new("StrikeFighter", Commander.Horner), "Strike Fighter" },
        { new("SovereignBattlecruiser", Commander.Horner), "Sovereign Battlecruiser" },
        { new("AiurCarrier", Commander.Karax), "Support Carrier" },
        { new("PurifierColossus", Commander.Karax), "Purifier Colossus" },
        { new("BroodMutalisk", Commander.Kerrigan), "Brood Mutalisk" },
        { new("BroodLord", Commander.Kerrigan), "Brood Lord" },
        { new("Raven", Commander.Mengsk), "Imperial Witness" },
        { new("WarHound", Commander.Mengsk), "Warhound" },
        { new("Marauder", Commander.Mengsk), "Aegis Guard" },
        { new("TrooperMengskFlamethrower", Commander.Mengsk), "Flamethrower Dominion Trooper" },
        { new("TrooperMengskImproved", Commander.Mengsk), "LMG Dominion Trooper" },
        { new("SiegeTank", Commander.Mengsk), "Shock Division" },
        { new("Medivac", Commander.Mengsk), "Imperial Intercessor" },
        { new("SkyFury", Commander.Mengsk), "Sky Fury" },
        { new("Ghost", Commander.Mengsk), "Emperor's Shadow" },
        { new("Trooper", Commander.Mengsk), "Dominion Trooper" },
        { new("Thor", Commander.Mengsk), "Blackhammer" },
        { new("Battlecruiser", Commander.Mengsk), "Pride of Augustgrad" },
        { new("TrooperMengskAA", Commander.Mengsk), "Hailstorm Dominion Trooper" },
        { new("StrikeGoliath", Commander.Nova), "Strike Goliath" },
        { new("RavenTypeII", Commander.Nova), "Raven Type-II" },
        { new("MarauderCommando", Commander.Nova), "Marauder Commando" },
        { new("HellbatRanger", Commander.Nova), "Hellbat Ranger" },
        { new("SpecOpsGhost", Commander.Nova), "Spec Ops Ghost" },
        { new("EliteMarine", Commander.Nova), "Elite Marine" },
        { new("HeavySiegeTank", Commander.Nova), "Heavy Siege Tank" },
        { new("RaidLiberator", Commander.Nova), "Raid Liberator" },
        { new("HoloDecoy", Commander.Nova), "Holo Decoy" },
        { new("CovertBanshee", Commander.Nova), "Covert Banshee" },
        { new("DuskWing", Commander.Raynor), "Dusk Wings" },
        { new("SiegeTank", Commander.Raynor), "Siege Tank" },
        { new("SpiderMine", Commander.Raynor), "Spider Mine" },
        { new("Zergling", Commander.Stetmann), "Mecha Zergling" },
        { new("Hydralisk", Commander.Stetmann), "Mecha Hydralisk" },
        { new("SuperGary", Commander.Stetmann), "Super Gary" },
        { new("Lurker", Commander.Stetmann), "Mecha Lurker" },
        { new("Ultralisk", Commander.Stetmann), "Mecha Ultralisk" },
        { new("Baneling", Commander.Stetmann), "Mecha Baneling" },
        { new("Corruptor", Commander.Stetmann), "Mecha Corruptor" },
        { new("Overseer", Commander.Stetmann), "Mecha Overseer" },
        { new("Infestor", Commander.Stetmann), "Mecha Infestor" },
        { new("BroodLord", Commander.Stetmann), "Mecha Battlecarrier Lord" },
        { new("InfestedBunker", Commander.Stukov), "Infested Bunker" },
        { new("InfestedCivilian", Commander.Stukov), "Infested Civilian" },
        { new("InfestedBanshee", Commander.Stukov), "Infested Banshee" },
        { new("InfestedMarine", Commander.Stukov), "Infested Marine" },
        { new("InfestedLiberator", Commander.Stukov), "Infested Liberator" },
        { new("InfestedDiamondback", Commander.Stukov), "Infested Diamondback" },
        { new("VolatileInfested", Commander.Stukov), "Volatile Infested" },
        { new("InfestedSiegeTank", Commander.Stukov), "Infested Siege Tank" },
        { new("BroodQueen", Commander.Stukov), "Brood Queen" },
        { new("ScienceVessel", Commander.Swann), "Science Vessel" },
        { new("SiegeTank", Commander.Swann), "Siege Tank" },
        { new("ARES", Commander.Swann), "A.R.E.S." },
        { new("Blaze", Commander.Tychus), "Miles \"Blaze\" Lewis" },
        { new("Rattlesnake", Commander.Tychus), "Kev \"Rattlesnake\" West" },
        { new("Cannonball", Commander.Tychus), "Rob \"Cannonball\" Boswell" },
        { new("Nikara", Commander.Tychus), "Lt. Layna Nikara" },
        { new("Sam", Commander.Tychus), "Crooked Sam" },
        { new("Sirius", Commander.Tychus), "James \"Sirius\" Sykes" },
        { new("VoidRay", Commander.Vorazun), "Void Ray" },
        { new("DarkTemplar", Commander.Vorazun), "Dark Templar" },
        { new("ShadowGuard", Commander.Vorazun), "Shadow Guard" },
        { new("DarkArchon", Commander.Vorazun), "Dark Archon" },
        { new("HunterKiller", Commander.Zagara), "Hunter Killer" },
        { new("Coop", Commander.Zeratul), "Zeratul" },
        { new("SummonKarass", Commander.Zeratul), "Telbrus" },
        { new("Stalker", Commander.Zeratul), "Xel'Naga Ambusher" },
        { new("Sentry", Commander.Zeratul), "Xel'Naga Shieldguard" },
        { new("DarkTemplar", Commander.Zeratul), "Void Templar" },
        { new("Disruptor", Commander.Zeratul), "Xel'Naga Abrogator" },
        { new("Observer", Commander.Zeratul), "Xel'Naga Watcher" },
        { new("Immortal", Commander.Zeratul), "Xel'Naga Enforcer" },
        { new("HonorGuard", Commander.Zeratul), "Honor Guard" },
        { new("VoidRay", Commander.Protoss), "Void Ray" },
        { new("HighTemplar", Commander.Protoss), "High Templar" },
        { new("SiegeTank", Commander.Terran), "Siege Tank" },
        { new("WidowMine", Commander.Terran), "Widow Mine" },
        { new("BroodLord", Commander.Zerg), "Brood Lord" },
        { new("SwarmHost", Commander.Zerg), "Swarm Host" },
    }.ToFrozenDictionary();

    public async Task SetBuildResponseLifeAndCost(BuildResponse buildResponse, Commander cmdr)
    {
        var dsUnits = await dsUnitRepository.GetDsUnits();

        foreach (var buildUnit in buildResponse.Units)
        {
            var unitName = MapUnitName(buildUnit.Name, cmdr);
            var dsUnit = dsUnits.FirstOrDefault(f => f.Name.Equals(unitName, StringComparison.Ordinal)
                && f.Commander == cmdr);

            if (dsUnit is null)
            {
                continue;
            }

            buildUnit.Name = unitName;
            buildUnit.Life = Math.Round(dsUnit.Life * buildUnit.Count, 2);
            buildUnit.Cost = Math.Round(dsUnit.Cost * buildUnit.Count, 2);
        }
    }

    public async Task<SpawnInfo> GetSpawnInfo(SpawnRequest request)
    {
        var dsUnits = await dsUnitRepository.GetDsUnits();
        Dictionary<string, DsUnitBuildDto> buildUnits = [];

        foreach (var unit in request.Units)
        {
            var unitName = MapUnitName(unit.Name, request.Commander);

            var dsUnit = dsUnits.FirstOrDefault(f => f.Name.Equals(unitName, StringComparison.Ordinal) 
                && f.Commander == request.Commander);

            if (dsUnit is null)
            {
                continue;
            }

            if (!buildUnits.ContainsKey(unit.Name))
            {
                buildUnits[unit.Name] = mapper.Map<DsUnitBuildDto>(dsUnit);
            }
        }

        return new()
        {
            BuildUnits = buildUnits
        };
    }

    public async Task<SpawnInfo> GetDsUnitSpawnInfo(SpawnDto spawn, Commander cmdr)
    {
        var dsUnits = await dsUnitRepository.GetDsUnits();

        int armyValue = 0;
        int armyLife = 0;
        Dictionary<string, DsUnitBuildDto> buildUnits = [];

        foreach (var spawnUnit in spawn.Units)
        {
            var unitName = MapUnitName(spawnUnit.Unit.Name, cmdr);

            var dsUnit = dsUnits.FirstOrDefault(f => f.Name.Equals(unitName));

            if (dsUnit is null)
            {
                continue;
            }

            armyValue += dsUnit.Cost * spawnUnit.Count;
            armyLife += (dsUnit.Life + dsUnit.Shields) * spawnUnit.Count;

            if (!buildUnits.ContainsKey(spawnUnit.Unit.Name))
            {
                buildUnits[spawnUnit.Unit.Name] = mapper.Map<DsUnitBuildDto>(dsUnit);
            }
        }

        return new() 
        { 
            ArmyTotalVitality = armyLife,
            ArmyValue = armyValue,
            BuildUnits = buildUnits
        };
    }

    private string MapUnitName(string unitName, Commander cmdr)
    {
        if (unitMap.TryGetValue(new(unitName, cmdr), out var nameValue)
            && nameValue is string value)
        {
            return nameValue;
        }
        else
        {
            return unitName;
        }
    }


}

internal sealed record UnitMapKey(string UnitName, Commander Commander);

