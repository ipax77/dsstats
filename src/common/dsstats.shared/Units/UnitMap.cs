using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    public static string GetNormalizedUnitName(string name, Commander commander)
    {
        return commander switch
        {
            Commander.Protoss => GetProtossUnitName(name),
            Commander.Terran => GetTerranUnitName(name),
            Commander.Zerg => GetZergUnitName(name),
            Commander.Abathur => GetAbathurUnitName(name),
            Commander.Alarak => GetAlarakUnitName(name),
            Commander.Artanis => GetArtanisUnitName(name),
            Commander.Dehaka => GetDehakaUnitName(name),
            Commander.Fenix => GetFenixUnitName(name),
            Commander.Horner => GetHornerUnitName(name),
            Commander.Karax => GetKaraxUnitName(name),
            Commander.Kerrigan => GetKerriganUnitName(name),
            Commander.Mengsk => GetMengskUnitName(name),
            Commander.Nova => GetNovaUnitName(name),
            Commander.Raynor => GetRaynorUnitName(name),
            Commander.Stetmann => GetStetmannUnitName(name),
            Commander.Stukov => GetStukovUnitName(name),
            Commander.Swann => GetSwannUnitName(name),
            Commander.Tychus => GetTychusUnitName(name),
            Commander.Vorazun => GetVorazunUnitName(name),
            Commander.Zagara => GetZagaraUnitName(name),
            Commander.Zeratul => GetZeratulUnitName(name),
            _ => name
        };
    }

    private static readonly FrozenDictionary<string, UnitInfo> _unitInfoMap = new Dictionary<string, UnitInfo>()
    {
        { "Aberration", new("Aberration", UnitSize.Large, UnitType.Ground) },
        { "Adept", new("Adept", UnitSize.Small, UnitType.Ground) },
        { "AegisGuard", new("Aegis Guard", UnitSize.Small, UnitType.Ground) },
        { "Aegis Guard", new("Aegis Guard", UnitSize.Small, UnitType.Ground) },
        { "AiurCarrier", new("AiurCarrier", UnitSize.Large, UnitType.Air) },
        { "Alarak", new("Alarak", UnitSize.Large, UnitType.Ground) },
        { "Aleksander", new("Aleksander", UnitSize.VeryLarge, UnitType.Air) },
        { "Annihilator", new("Annihilator", UnitSize.Medium, UnitType.Ground) },
        { "Aperture", new("Aperture", UnitSize.Medium, UnitType.Ground) },
        { "Apocalisk", new("Apocalisk", UnitSize.VeryLarge, UnitType.Ground) },
        { "Archon", new("Archon", UnitSize.Large, UnitType.Ground) },
        { "ARES", new("ARES", UnitSize.Medium, UnitType.Ground) },
        { "A.R.E.S.", new("A.R.E.S.", UnitSize.Medium, UnitType.Ground) },
        { "Ascendant", new("Ascendant", UnitSize.Small, UnitType.Ground) },
        { "Assault Galleon", new("Assault Galleon", UnitSize.VeryLarge, UnitType.Air) },
        { "Asteria Wraith", new("Asteria Wraith", UnitSize.Medium, UnitType.Air) },
        { "Aurora", new("Aurora", UnitSize.Medium, UnitType.Ground) },
        { "Baneling", new("Baneling", UnitSize.Small, UnitType.Ground) },
        { "Banshee", new("Banshee", UnitSize.Medium, UnitType.Air) },
        { "Battlecruiser", new("Battlecruiser", UnitSize.Large, UnitType.Air) },
        { "Blackhammer", new("Blackhammer", UnitSize.Large, UnitType.Ground) },
        { "Blaze", new("Blaze", UnitSize.Medium, UnitType.Ground) },
        { "Brood Lord", new("Brood Lord", UnitSize.Large, UnitType.Air) },
        { "BroodMutalisk", new("Brood Mutalisk", UnitSize.Small, UnitType.Air) },
        { "Brood Mutalisk", new("Brood Mutalisk", UnitSize.Small, UnitType.Air) },
        { "BroodQueen", new("BroodQueen", UnitSize.Medium, UnitType.Ground) },
        { "Brood Queen", new("BroodQueen", UnitSize.Medium, UnitType.Ground) },
        { "Brutalisk", new("Brutalisk", UnitSize.VeryLarge, UnitType.Ground) },
        { "Cannonball", new("Cannonball", UnitSize.Medium, UnitType.Ground) },
        { "Capjack", new("Capjack", UnitSize.Medium, UnitType.Ground) },
        { "Carrier", new("Carrier", UnitSize.Large, UnitType.Air) },
        { "Centurion", new("Centurion", UnitSize.Small, UnitType.Ground) },
        { "Crooked Sam", new("Crooked Sam", UnitSize.Medium, UnitType.Ground) },
        { "Clolarion", new("Clolarion", UnitSize.VeryLarge, UnitType.Air) },
        { "Colossus", new("Colossus", UnitSize.Large, UnitType.Ground) },
        { "Commando Marauder", new("Commando Marauder", UnitSize.Medium, UnitType.Ground) },
        { "Conservator", new("Conservator", UnitSize.Small, UnitType.Ground) },
        { "Corruptor", new("Corruptor", UnitSize.Medium, UnitType.Air) },
        { "Corsair", new("Corsair", UnitSize.Medium, UnitType.Air) },
        { "Covert Banshee", new("Covert Banshee", UnitSize.Medium, UnitType.Air) },
        { "Creeper Host", new("Creeper Host", UnitSize.Medium, UnitType.Ground) },
        { "Cyclone", new("Cyclone", UnitSize.Medium, UnitType.Ground) },
        { "Dark Archon", new("Dark Archon", UnitSize.Medium, UnitType.Ground) },
        { "Dark Templar", new("Dark Templar", UnitSize.Small, UnitType.Ground) },
        { "Defensive Drone", new("Defensive Drone", UnitSize.Small, UnitType.Ground) },
        { "Dehaka", new("Dehaka", UnitSize.Large, UnitType.Ground) },
        { "Deimos Viking", new("Deimos Viking", UnitSize.Medium, UnitType.Air) },
        { "Deimos Viking (Assault)", new("Deimos Viking (Assault)", UnitSize.Medium, UnitType.Ground) },
        { "Deimos Viking (Fighter)", new("Deimos Viking (Fighter)", UnitSize.Medium, UnitType.Air) },
        { "Destroyer", new("Destroyer", UnitSize.Medium, UnitType.Ground) },
        { "Devourer", new("Devourer", UnitSize.Medium, UnitType.Air) },
        { "Diamondback", new("Diamondback", UnitSize.Medium, UnitType.Ground) },
        { "Disruptor", new("Disruptor", UnitSize.Medium, UnitType.Ground) },
        { "DisruptorPhased", new("DisruptorPhased", UnitSize.Medium, UnitType.Ground) },
        { "Dominion Trooper", new("Dominion Trooper", UnitSize.Small, UnitType.Ground) },
        { "Dragoon", new("Dragoon", UnitSize.Medium, UnitType.Ground) },
        { "Dusk Wing", new("Dusk Wings", UnitSize.Medium, UnitType.Air) },
        { "Dusk Wings", new("Dusk Wings", UnitSize.Medium, UnitType.Air) },
        { "DuskWing", new("Dusk Wings", UnitSize.Medium, UnitType.Air) },
        { "Echo", new("Echo", UnitSize.Medium, UnitType.Ground) },
        { "Elite Marine", new("Elite Marine", UnitSize.Small, UnitType.Ground) },
        { "EliteMarine", new("Elite Marine", UnitSize.Small, UnitType.Ground) },
        { "Elytron", new("Elytron", UnitSize.Medium, UnitType.Air) },
        { "Energizer", new("Energizer", UnitSize.Small, UnitType.Ground) },
        { "Emperor's Shadow", new("Emperor's Shadow", UnitSize.Medium, UnitType.Air) },
        { "Fenix (Flyer)", new("Fenix (Flyer)", UnitSize.Large, UnitType.Air) },
        { "Fenix (Praetor)", new("Fenix (Praetor)", UnitSize.Large, UnitType.Ground) },
        { "Fenix (Walker)", new("Fenix (Walker)", UnitSize.Large, UnitType.Ground) },
        { "Fenix - Arbiter", new("Fenix - Arbiter", UnitSize.Large, UnitType.Air) },
        { "Fenix - Dragoon", new("Fenix - Dragoon", UnitSize.Large, UnitType.Ground) },
        { "Fenix - Praetor", new("Fenix - Praetor", UnitSize.Large, UnitType.Ground) },
        { "FenixFlyer", new("FenixFlyer", UnitSize.Medium, UnitType.Air) },
        { "Firebat", new("Firebat", UnitSize.Small, UnitType.Ground) },
        { "Firehawk", new("Firehawk", UnitSize.VeryLarge, UnitType.Air) },
        { "Flamethrower Dominion Trooper", new("Flamethrower Dominion Trooper", UnitSize.Small, UnitType.Ground) },
        { "Flyer", new("Fenix (Flyer)", UnitSize.Medium, UnitType.Air) },
        { "Fuse", new("Fuse", UnitSize.Medium, UnitType.Ground) },
        { "Gary", new("Gary", UnitSize.Large, UnitType.Ground) },
        { "Ghost", new("Ghost", UnitSize.Small, UnitType.Ground) },
        { "GhostNova", new("Ghost Nova", UnitSize.Small, UnitType.Ground) },
        { "Goliath", new("Goliath", UnitSize.Medium, UnitType.Ground) },
        { "Guardian", new("Guardian", UnitSize.Large, UnitType.Air) },
        { "Gyre", new("Gyre", UnitSize.Medium, UnitType.Ground) },
        { "Hailstorm Dominion Trooper", new("Hailstorm Dominion Trooper", UnitSize.Small, UnitType.Ground) },
        { "Hammer Securities", new("Hammer Securities", UnitSize.Medium, UnitType.Ground) },
        { "HammerSecurities", new("HammerSecurities", UnitSize.Medium, UnitType.Ground) },
        { "Harbinger", new("Harbinger", UnitSize.Medium, UnitType.Ground) },
        { "Havoc", new("Havoc", UnitSize.Small, UnitType.Ground) },
        { "HeavySiegeTank", new("Heavy Siege Tank", UnitSize.Medium, UnitType.Ground) },
        { "Heavy Siege Tank", new("Heavy Siege Tank", UnitSize.Medium, UnitType.Ground) },
        { "Hell’s Angel (Assault)", new("Hell’s Angel (Assault)", UnitSize.Medium, UnitType.Air) },
        { "Hell’s Angel (Fighter)", new("Hell’s Angel (Fighter)", UnitSize.Medium, UnitType.Air) },
        { "Hellbat", new("Hellbat", UnitSize.Medium, UnitType.Ground) },
        { "Hellbat Ranger", new("Hellbat Ranger", UnitSize.Medium, UnitType.Ground) },
        { "Hellion", new("Hellion", UnitSize.Medium, UnitType.Ground) },
        { "Hellion (Tank Mode)", new("Hellion (Tank Mode)", UnitSize.Medium, UnitType.Ground) },
        { "HelsAngel Assault", new("HelsAngel Assault", UnitSize.Medium, UnitType.Air) },
        { "HelsAngel Fighter", new("HelsAngel Fighter", UnitSize.Medium, UnitType.Air) },
        { "High Archon", new("High Archon", UnitSize.Large, UnitType.Ground) },
        { "High Templar", new("High Templar", UnitSize.Small, UnitType.Ground) },
        { "HoloDecoy", new("Holo Decoy", UnitSize.Medium, UnitType.Ground) },
        { "Holo Decoy", new("Holo Decoy", UnitSize.Medium, UnitType.Ground) },
        { "Honor Guard", new("Honor Guard", UnitSize.Small, UnitType.Ground) },
        { "Hunter Killer", new("Hunter Killer", UnitSize.Medium, UnitType.Ground) },
        { "HybridBehemoth", new("Hybrid Behemoth", UnitSize.VeryLarge, UnitType.Ground) },
        { "HybridDestroyer", new("Hybrid Destroyer", UnitSize.VeryLarge, UnitType.Ground) },
        { "HybridDominator", new("Hybrid Dominator", UnitSize.VeryLarge, UnitType.Ground) },
        { "HybridNemesis", new("Hybrid Nemesis", UnitSize.VeryLarge, UnitType.Ground) },
        { "HybridReaver", new("Hybrid Reaver", UnitSize.VeryLarge, UnitType.Ground) },
        { "Hydralisk", new("Hydralisk", UnitSize.Small, UnitType.Ground) },
        { "Hyperion", new("Hyperion", UnitSize.VeryLarge, UnitType.Air) },
        { "Immortal", new("Immortal", UnitSize.Medium, UnitType.Ground) },
        { "Impaler", new("Impaler", UnitSize.Medium, UnitType.Ground) },
        { "Imperial Intercessor", new("Imperial Intercessor", UnitSize.Medium, UnitType.Air) },
        { "Imperial Witness", new("Imperial Witness", UnitSize.Medium, UnitType.Air) },
        { "Inducer", new("Inducer", UnitSize.Medium, UnitType.Ground) },
        { "Infested Banshee", new("Infested Banshee", UnitSize.Medium, UnitType.Air) },
        { "Infested Bunker", new("Infested Bunker", UnitSize.Large, UnitType.Ground) },
        { "Infested Civilian", new("Infested Civilian", UnitSize.Small, UnitType.Ground) },
        { "Infested Diamondback", new("Infested Diamondback", UnitSize.Medium, UnitType.Ground) },
        { "Infested Liberator", new("Infested Liberator", UnitSize.Medium, UnitType.Air) },
        { "Infested Liberator (Viral Swarm)", new("Infested Liberator (Viral Swarm)", UnitSize.Medium, UnitType.Air) },
        { "Infested Marine", new("Infested Marine", UnitSize.Small, UnitType.Ground) },
        { "Infested Siege Tank", new("Infested Siege Tank", UnitSize.Medium, UnitType.Ground) },
        { "Infested Thor", new("Infested Thor", UnitSize.Medium, UnitType.Ground) },
        { "Infestor", new("Infestor", UnitSize.Medium, UnitType.Ground) },
        { "Interceptor", new("Interceptor", UnitSize.None, UnitType.Air) },
        { "James \"Sirius\" Sykes", new("James \"Sirius\" Sykes", UnitSize.Medium, UnitType.Ground) },
        { "Kev \"Rattlesnake\" West", new("Kev \"Rattlesnake\" West", UnitSize.Medium, UnitType.Ground) },
        { "Lt.Layna Nikara", new("Lt.Layna Nikara", UnitSize.Medium, UnitType.Ground) },
        { "Kaldalis", new("Kaldalis", UnitSize.Medium, UnitType.Ground) },
        { "Karass", new("Karass", UnitSize.Medium, UnitType.Ground) },
        { "Kerrigan", new("Kerrigan", UnitSize.Large, UnitType.Ground) },
        { "Legionnaire", new("Legionnaire", UnitSize.Small, UnitType.Ground) },
        { "Leviathan", new("Leviathan", UnitSize.VeryLarge, UnitType.Air) },
        { "Liberator", new("Liberator", UnitSize.Medium, UnitType.Air) },
        { "LMG Dominion Trooper", new("LMG Dominion Trooper", UnitSize.Small, UnitType.Ground) },
        { "Locust", new("Locust", UnitSize.Small, UnitType.Ground) },
        { "Locust Precursor", new("Locust Precursor", UnitSize.Small, UnitType.Ground) },
        { "Lurker", new("Lurker", UnitSize.Medium, UnitType.Ground) },
        { "LurkerStetmann", new("LurkerStetmann", UnitSize.Medium, UnitType.Ground) },
        { "Marauder", new("Marauder", UnitSize.Small, UnitType.Ground) },
        { "MarauderCommando", new("Marauder Commando", UnitSize.Medium, UnitType.Ground) },
        { "Marauder Commando", new("Marauder Commando", UnitSize.Medium, UnitType.Ground) },
        { "Marine", new("Marine", UnitSize.Small, UnitType.Ground) },
        { "Medic", new("Medic", UnitSize.Small, UnitType.Ground) },
        { "Medivac", new("Medivac", UnitSize.Medium, UnitType.Air) },
        { "Mirage", new("Mirage", UnitSize.Medium, UnitType.Air) },
        { "Mojo", new("Mojo", UnitSize.Medium, UnitType.Air) },
        { "Mothership", new("Mothership", UnitSize.VeryLarge, UnitType.Air) },
        { "Mothership Taldarim", new("Mothership Taldarim", UnitSize.VeryLarge, UnitType.Air) },
        { "Mutalisk", new("Mutalisk", UnitSize.Small, UnitType.Air) },
        { "Myriad", new("Myriad", UnitSize.Medium, UnitType.Ground) },
        { "Nikara", new("Nikara", UnitSize.Medium, UnitType.Ground) },
        { "Nova", new("Nova", UnitSize.Large, UnitType.Ground) },
        { "Nux", new("Nux", UnitSize.Medium, UnitType.Ground) },
        { "Observer", new("Observer", UnitSize.Small, UnitType.Air) },
        { "Oracle", new("Oracle", UnitSize.Medium, UnitType.Air) },
        { "Overseer", new("Overseer", UnitSize.Medium, UnitType.Air) },
        { "Paragon", new("Paragon", UnitSize.Large, UnitType.Ground) },
        { "Pariah", new("Pariah", UnitSize.Medium, UnitType.Ground) },
        { "Phoenix", new("Phoenix", UnitSize.Medium, UnitType.Air) },
        { "Praetor", new("Praetor", UnitSize.Medium, UnitType.Ground) },
        { "PrimalGuardian", new("Guardian", UnitSize.Large, UnitType.Air) },
        { "PrimalHost", new("Primal Host", UnitSize.Medium, UnitType.Ground) },
        { "PrimalHydralisk", new("Hydralisk", UnitSize.Small, UnitType.Ground) },
        { "PrimalIgniter", new("Igniter", UnitSize.Medium, UnitType.Ground) },
        { "PrimalMutalisk", new("Mutalisk", UnitSize.Medium, UnitType.Air) },
        { "PrimalRavasaur", new("Ravasaur", UnitSize.Small, UnitType.Ground) },
        { "PrimalRoach", new("Roach", UnitSize.Small, UnitType.Ground) },
        { "PrimalUltralisk", new("Ultralisk", UnitSize.Large, UnitType.Ground) },
        { "PrimalZergling", new("Zergling", UnitSize.Small, UnitType.Ground) },
        { "Probe", new("Probe", UnitSize.Small, UnitType.Ground) },
        { "Probius", new("Probius", UnitSize.Small, UnitType.Ground) },
        { "Purifier Beam", new("Purifier Beam", UnitSize.Medium, UnitType.Ground) },
        { "Queen", new("Queen", UnitSize.Medium, UnitType.Ground) },
        { "RaidLiberator", new("Raid Liberator", UnitSize.Medium, UnitType.Air) },
        { "Railgun Turret", new("Railgun Turret", UnitSize.Medium, UnitType.Ground) },
        { "Raptorling", new("Raptorling", UnitSize.Small, UnitType.Ground) },
        { "Rattlesnake", new("Rattlesnake", UnitSize.Medium, UnitType.Ground) },
        { "Ravager", new("Ravager", UnitSize.Medium, UnitType.Ground) },
        { "Raven", new("Raven", UnitSize.Medium, UnitType.Air) },
        { "Raven Type II", new("Raven Type II", UnitSize.Medium, UnitType.Air) },
        { "Reaper", new("Reaper", UnitSize.Small, UnitType.Ground) },
        { "Reaver", new("Reaver", UnitSize.Medium, UnitType.Ground) },
        { "Roach", new("Roach", UnitSize.Small, UnitType.Ground) },
        { "Sam", new("Sam", UnitSize.Medium, UnitType.Ground) },
        { "Science Vessel", new("Science Vessel", UnitSize.Medium, UnitType.Air) },
        { "Scourge", new("Scourge", UnitSize.Small, UnitType.Air) },
        { "Scout", new("Scout", UnitSize.Medium, UnitType.Air) },
        { "Sentinel", new("Sentinel", UnitSize.Small, UnitType.Ground) },
        { "Sentry", new("Sentry", UnitSize.Small, UnitType.Ground) },
        { "ShadowGuard", new("Shadow Guard", UnitSize.Small, UnitType.Ground) },
        { "Shadow Guard", new("Shadow Guard", UnitSize.Small, UnitType.Ground) },
        { "Siege Breaker", new("Siege Breaker", UnitSize.Medium, UnitType.Ground) },
        { "Siege Drone", new("Siege Drone", UnitSize.Medium, UnitType.Ground) },
        { "SiegeTank", new("Siege Tank", UnitSize.Medium, UnitType.Ground) },
        { "Siege Tank", new("Siege Tank", UnitSize.Medium, UnitType.Ground) },
        { "Sirius", new("Sirius", UnitSize.Medium, UnitType.Ground) },
        { "SkyFury", new("SkyFury", UnitSize.Medium, UnitType.Air) },
        { "Slayer", new("Slayer", UnitSize.Medium, UnitType.Ground) },
        { "SovereignBattlecruiser", new("SovereignBattlecruiser", UnitSize.VeryLarge, UnitType.Air) },
        { "Spectre", new("Spectre", UnitSize.Small, UnitType.Ground) },
        { "SpiderMine", new("SpiderMine", UnitSize.Small, UnitType.Ground) },
        { "Splitterling", new("Splitterling", UnitSize.Small, UnitType.Ground) },
        { "Stalker", new("Stalker", UnitSize.Medium, UnitType.Ground) },
        { "Strike Fighter", new("Strike Fighter", UnitSize.Medium, UnitType.Air) },
        { "Strike Goliath", new("Strike Goliath", UnitSize.Medium, UnitType.Ground) },
        { "StrikeGoliath", new("Strike Goliath", UnitSize.Medium, UnitType.Ground) },
        { "Subjecter", new("Subjecter", UnitSize.Medium, UnitType.Ground) },
        { "SummonKarass", new("SummonKarass", UnitSize.Medium, UnitType.Ground) },
        { "SuperGary", new("SuperGary", UnitSize.Large, UnitType.Ground) },
        { "Supplicant", new("Supplicant", UnitSize.Small, UnitType.Ground) },
        { "SwannThor", new("SwannThor", UnitSize.Medium, UnitType.Ground) },
        { "SwarmHost", new("SwarmHost", UnitSize.Medium, UnitType.Ground) },
        { "Swarm Host", new("SwarmHost", UnitSize.Medium, UnitType.Ground) },
        { "Swarm Queen", new("Swarm Queen", UnitSize.Large, UnitType.Ground) },
        { "Swarmling", new("Swarmling", UnitSize.Small, UnitType.Ground) },
        { "Taldarin", new("Taldarin", UnitSize.Small, UnitType.Ground) },
        { "Talis", new("Talis", UnitSize.Medium, UnitType.Ground) },
        { "Tempest", new("Tempest", UnitSize.Large, UnitType.Air) },
        { "TheiaRaven", new("TheiaRaven", UnitSize.Medium, UnitType.Air) },
        { "Thor", new("Thor", UnitSize.Medium, UnitType.Ground) },
        { "Thor (AP)", new("Thor (AP)", UnitSize.Large, UnitType.Ground) },
        { "Titan", new("Titan", UnitSize.Medium, UnitType.Ground) },
        { "Torrasque", new("Torrasque", UnitSize.Large, UnitType.Ground) },
        { "Trooper", new("Trooper", UnitSize.Small, UnitType.Ground) },
        { "Trooper (AA)", new("Trooper (AA)", UnitSize.Medium, UnitType.Ground) },
        { "Trooper (Flamethrower)", new("Trooper (Flamethrower)", UnitSize.Medium, UnitType.Ground) },
        { "Trooper (Improved)", new("Trooper (Improved)", UnitSize.Medium, UnitType.Ground) },
        { "Trooper AA", new("Trooper AA", UnitSize.Small, UnitType.Ground) },
        { "Trooper Flamethrower", new("Trooper Flamethrower", UnitSize.Small, UnitType.Ground) },
        { "Trooper Improved", new("Trooper Improved", UnitSize.Small, UnitType.Ground) },
        { "Tychus", new("Tychus", UnitSize.Large, UnitType.Ground) },
        { "Tyrannozor", new("Tyrannozor", UnitSize.VeryLarge, UnitType.Ground) },
        { "Ultralisk", new("Ultralisk", UnitSize.Medium, UnitType.Ground) },
        { "Vanguard", new("Vanguard", UnitSize.Medium, UnitType.Ground) },
        { "Vega", new("Vega", UnitSize.Medium, UnitType.Ground) },
        { "Viking", new("Viking", UnitSize.Medium, UnitType.Air) },
        { "Viking (Assault)", new("Viking (Assault)", UnitSize.Medium, UnitType.Ground) },
        { "Viking (Fighter)", new("Viking (Fighter)", UnitSize.Medium, UnitType.Air) },
        { "Vile Roach", new("Vile Roach", UnitSize.Medium, UnitType.Ground) },
        { "Viper", new("Viper", UnitSize.Medium, UnitType.Air) },
        { "VoidRay", new("VoidRay", UnitSize.Medium, UnitType.Air) },
        { "Volatile Infested", new("Volatile Infested", UnitSize.Medium, UnitType.Ground) },
        { "VolatileInfested", new("VolatileInfested", UnitSize.Small, UnitType.Ground) },
        { "Volt", new("Volt", UnitSize.Medium, UnitType.Ground) },
        { "Vulture", new("Vulture", UnitSize.Medium, UnitType.Ground) },
        { "Walker", new("Walker", UnitSize.Large, UnitType.Ground) },
        { "War Hound", new("War Hound", UnitSize.Medium, UnitType.Ground) },
        { "War Pig", new("War Pig", UnitSize.Medium, UnitType.Ground) },
        { "Warbringer", new("Warbringer", UnitSize.Medium, UnitType.Ground) },
        { "Ward", new("Ward", UnitSize.Medium, UnitType.Ground) },
        { "WarHound", new("WarHound", UnitSize.Medium, UnitType.Ground) },
        { "Warp Prism", new("Warp Prism", UnitSize.Medium, UnitType.Air) },
        { "WarPrism", new("WarPrism", UnitSize.Medium, UnitType.Air) },
        { "WidowMine", new("WidowMine", UnitSize.Small, UnitType.Ground) },
        { "Wraith", new("Wraith", UnitSize.Medium, UnitType.Air) },
        { "Wrathwalker", new("Wrathwalker", UnitSize.Large, UnitType.Ground) },
        { "Zagara", new("Zagara", UnitSize.Large, UnitType.Ground) },
        { "Zealot", new("Zealot", UnitSize.Small, UnitType.Ground) },
        { "Zeratul", new("Zeratul", UnitSize.Large, UnitType.Ground) },
        { "ZeratulStalker", new("ZeratulStalker", UnitSize.Medium, UnitType.Ground) },
        { "Zergling", new("Zergling", UnitSize.Small, UnitType.Ground) },
        { "Zerus Peridot", new("Zerus Peridot", UnitSize.Medium, UnitType.Ground) },

        { "Mecha Baneling", new("Mecha Baneling", UnitSize.Small, UnitType.Ground) },
        { "Mecha Battlecarrier Lord", new("Mecha Battlecarrier Lord", UnitSize.Large, UnitType.Air) },
        { "Mecha Broodling", new("Mecha Broodling", UnitSize.Small, UnitType.Ground) },
        { "Mecha Corruptor", new("Mecha Corruptor", UnitSize.Medium, UnitType.Air) },
        { "Mecha Hydralisk", new("Mecha Hydralisk", UnitSize.Medium, UnitType.Ground) },
        { "Mecha Infestor", new("Mecha Infestor", UnitSize.Medium, UnitType.Ground) },
        { "Mecha Lurker", new("Mecha Lurker", UnitSize.Medium, UnitType.Ground) },
        { "Mecha Overseer", new("Mecha Overseer", UnitSize.Medium, UnitType.Air) },
        { "Mecha Ravager", new("Mecha Ravager", UnitSize.Medium, UnitType.Ground) },
        { "Mecha Roach", new("Mecha Roach", UnitSize.Small, UnitType.Ground) },
        { "Mecha Ultralisk", new("Mecha Ultralisk", UnitSize.Large, UnitType.Ground) },
        { "Mecha Zergling", new("Mecha Zergling", UnitSize.Small, UnitType.Ground) },
        { "Miles \"Blaze\" Lewis", new("Miles \"Blaze\" Lewis", UnitSize.Medium, UnitType.Ground) },
        { "Pride of Augustgrad", new("Pride of Augustgrad", UnitSize.VeryLarge, UnitType.Air) },
        { "Primal Guardian", new("Primal Guardian", UnitSize.Medium, UnitType.Air) },
        { "Primal Host", new("Primal Host", UnitSize.Medium, UnitType.Ground) },
        { "Primal Hydralisk", new("Primal Hydralisk", UnitSize.Small, UnitType.Ground) },
        { "Primal Igniter", new("Primal Igniter", UnitSize.Medium, UnitType.Ground) },
        { "Primal Locusts", new("Primal Locusts", UnitSize.Medium, UnitType.Ground) },
        { "Primal Mutalisk", new("Primal Mutalisk", UnitSize.Medium, UnitType.Air) },
        { "Primal Roach", new("Primal Roach", UnitSize.Medium, UnitType.Ground) },
        { "Primal Ultralisk", new("Primal Ultralisk", UnitSize.Large, UnitType.Ground) },
        { "Primal Zergling", new("Primal Zergling", UnitSize.Small, UnitType.Ground) },
        { "PurifierAdept", new("Purifier Adept", UnitSize.Small, UnitType.Ground) },
        { "Purifier Adept", new("Purifier Adept", UnitSize.Small, UnitType.Ground) },
        { "PurifierColossus", new("Purifier Colossus", UnitSize.Large, UnitType.AirAndGround) },
        { "Purifier Colossus", new("Purifier Colossus", UnitSize.Large, UnitType.AirAndGround) },
        { "Purifier Tempest", new("Purifier Tempest", UnitSize.Large, UnitType.Air) },
        { "Raid Liberator", new("Raid Liberator", UnitSize.Medium, UnitType.Air) },
        { "Ravasaur", new("Ravasaur", UnitSize.Medium, UnitType.Ground) },
        { "Raven Type-II", new("Raven Type-II", UnitSize.Medium, UnitType.Air) },
        { "Rob \"Cannonball\" Boswell", new("Rob \"Cannonball\" Boswell", UnitSize.Medium, UnitType.Ground) },
        { "Shock Division", new("Shock Division", UnitSize.Medium, UnitType.Ground) },
        { "Sky Fury", new("Sky Fury", UnitSize.Medium, UnitType.Air) },
        { "Sovereign Battlecruiser", new("Sovereign Battlecruiser", UnitSize.VeryLarge, UnitType.Ground) },
        { "Spec Ops Ghost", new("Spec Ops Ghost", UnitSize.Small, UnitType.Ground) },
        { "Super Gary", new("Super Gary", UnitSize.Large, UnitType.Air) },
        { "Support Carrier", new("Support Carrier", UnitSize.Large, UnitType.Air) },
        { "Tal'darim Mothership", new("Tal'darim Mothership", UnitSize.VeryLarge, UnitType.Air) },
        { "Telbrus", new("Telbrus", UnitSize.Small, UnitType.Ground) },
        { "Theia Raven", new("Theia Raven", UnitSize.Medium, UnitType.Air) },
        { "Void Ray", new("Void Ray", UnitSize.Medium, UnitType.Air) },
        { "Void Templar", new("Void Templar", UnitSize.Medium, UnitType.Ground) },
        { "War Prism", new("War Prism", UnitSize.Medium, UnitType.Air) },
        { "Warhound", new("Warhound", UnitSize.Medium, UnitType.Ground) },
        { "Widow Mine", new("Widow Mine", UnitSize.Small, UnitType.Ground) },
        { "Xel'Naga Abrogator", new("Xel'Naga Abrogator", UnitSize.Medium, UnitType.Ground) },
        { "Xel'Naga Ambusher", new("Xel'Naga Ambusher", UnitSize.Medium, UnitType.Ground) },
        { "Xel'Naga Enforcer", new("Xel'Naga Enforcer", UnitSize.Medium, UnitType.Ground) },
        { "Xel'Naga Shieldguard", new("Xel'Naga Shieldguard", UnitSize.Medium, UnitType.Ground) },
        { "Xel'Naga Watcher", new("Xel'Naga Watcher", UnitSize.Medium, UnitType.Ground) },
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, int> _unitIndexMap;

    static UnitMap()
    {
        var unitIndexBuilder = new Dictionary<string, int>();
        var index = 0;
        var unitNames = _unitInfoMap.Values.Select(s => s.Name).ToHashSet();
        foreach (var unitName in unitNames.OrderBy(k => k))
        {
            unitIndexBuilder[unitName] = index++;
        }
        _unitIndexMap = unitIndexBuilder.ToFrozenDictionary();
    }

    public static UnitInfo GetUnitInfo(string name, Commander commander)
    {
        var normalizedName = GetNormalizedUnitName(name, commander);
        if (_unitInfoMap.TryGetValue(normalizedName, out var info))
        {
            return info;
        }
        return new UnitInfo(normalizedName, UnitSize.Small, UnitType.Ground);
    }

    public static bool IsKnownName(string name)
    {
        return _unitInfoMap.ContainsKey(name);
    }


    /// <summary>
    /// Gets a unique, stable color for a given unit name.
    /// </summary>
    /// <param name="unitName">The full name of the unit (e.g., "Adept").</param>
    /// <returns>A hex color string.</returns>
    public static string GetColor(string unitName)
    {
        if (_unitIndexMap.TryGetValue(unitName, out int index))
        {
            // Use the stable index to generate a color.
            return GenerateColorFromIndex(index);
        }
        // Fallback for an unknown unit name, though this should be rare if the list is comprehensive.
        return "#CCCCCC"; // A neutral gray fallback.
    }

    public static string GetColor(string unitName, Commander commander)
    {
        if (!_unitIndexMap.TryGetValue(unitName, out int index))
            return "#CCCCCC";

        if (!Data.CmdrColor.TryGetValue(commander, out var baseHex))
            return GenerateColorFromIndex(index);

        return GenerateCommanderVariant(baseHex, index);
    }

    private static string GenerateCommanderVariant(string baseHex, int index)
    {
        var (h, s, l) = HexToHsl(baseHex);

        // Large hue steps for strong separation
        int hueStep = 45;               // 8 distinct directions
        int lightnessStep = 12;         // visible brightness change

        int hueOffset = (index % 8) * hueStep;
        int lightnessOffset = ((index / 8) % 3 - 1) * lightnessStep;

        int newHue = (h + hueOffset) % 360;
        int newLightness = Clamp(l + lightnessOffset, 35, 75);

        return HslToHex(newHue, s, newLightness);
    }

    private static int Clamp(int value, int min, int max)
        => Math.Min(max, Math.Max(min, value));


    /// <summary>
    /// Generates a distinct color using the HSL color model.
    /// </summary>
    /// <param name="index">The unique index of the unit.</param>
    /// <returns>A hex color string.</returns>
    private static string GenerateColorFromIndex(int index)
    {
        // Use the Golden Angle to distribute hues evenly.
        const double goldenRatioConjugate = 0.61803398875;
        double hue = (index * goldenRatioConjugate * 360) % 360;

        // Use high saturation and brightness for visibility on a dark background.
        int saturation = 75;
        int lightness = 70;

        return HslToHex((int)hue, saturation, lightness);
    }

    private static string HslToHex(int h, int s, int l)
    {
        // This is a simplified example; a full implementation is more complex.
        // It's recommended to use a library for robust color conversions.
        double hue = h / 360.0;
        double saturation = s / 100.0;
        double lightness = l / 100.0;

        // Simplified logic for demonstration
        var r = 0.0;
        var g = 0.0;
        var b = 0.0;

        if (saturation == 0)
        {
            r = g = b = lightness; // achromatic
        }
        else
        {
            Func<double, double, double, double> hue2rgb = (p, q, t) =>
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
                return p;
            };

            var q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            var p = 2 * lightness - q;
            r = hue2rgb(p, q, hue + 1.0 / 3.0);
            g = hue2rgb(p, q, hue);
            b = hue2rgb(p, q, hue - 1.0 / 3.0);
        }

        return $"#{ToByte(r):X2}{ToByte(g):X2}{ToByte(b):X2}";
    }

    private static (int H, int S, int L) HexToHsl(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length != 6)
            throw new ArgumentException("Invalid hex color format.");

        double r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255.0;
        double g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255.0;
        double b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        double s;
        double l = (max + min) / 2.0;

        if (delta == 0)
        {
            h = 0; // achromatic
        }
        else if (max == r)
        {
            h = ((g - b) / delta) % 6;
        }
        else if (max == g)
        {
            h = ((b - r) / delta) + 2;
        }
        else
        {
            h = ((r - g) / delta) + 4;
        }

        h *= 60;
        if (h < 0) h += 360;

        s = delta == 0
            ? 0
            : delta / (1 - Math.Abs(2 * l - 1));

        return (
            (int)Math.Round(h),
            (int)Math.Round(s * 100),
            (int)Math.Round(l * 100)
        );
    }


    private static int ToByte(double val) => (int)(val * 255);

    public static void CheckNames()
    {
        HashSet<string> names = [];
        names.UnionWith(ProtossUnitMap.Values);
        names.UnionWith(TerranUnitMap.Values);
        names.UnionWith(ZergUnitMap.Values);
        names.UnionWith(AbathurUnitMap.Values);
        names.UnionWith(AlarakUnitMap.Values);
        names.UnionWith(ArtanisUnitMap.Values);
        names.UnionWith(DehakaUnitMap.Values);
        names.UnionWith(FenixUnitMap.Values);
        names.UnionWith(HornerUnitMap.Values);
        names.UnionWith(KaraxUnitMap.Values);
        names.UnionWith(KerriganUnitMap.Values);
        names.UnionWith(MengskUnitMap.Values);
        names.UnionWith(NovaUnitMap.Values);
        names.UnionWith(RaynorUnitMap.Values);
        names.UnionWith(StetmannUnitMap.Values);
        names.UnionWith(StukovUnitMap.Values);
        names.UnionWith(SwannUnitMap.Values);
        names.UnionWith(TychusUnitMap.Values);
        names.UnionWith(VorazunUnitMap.Values);
        names.UnionWith(ZagaraUnitMap.Values);
        names.UnionWith(ZeratulUnitMap.Values);

        foreach (var name in names.OrderBy(o => o))
        {
            if (!_unitInfoMap.TryGetValue(name, out var value))
            {
                Console.WriteLine($"no unit info for {name}");
            }
        }
    }
}

public sealed record UnitInfo(string Name, UnitSize Size, UnitType Type);
