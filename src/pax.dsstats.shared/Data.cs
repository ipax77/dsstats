﻿using System.Security.Cryptography;
using System.Text;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.shared;

public static class Data
{
    public static Commander GetCommander(string race)
    {
        return race switch
        {
            "Terran" => Commander.Terran,
            "Protoss" => Commander.Protoss,
            "Zerg" => Commander.Zerg,
            "Abathur" => Commander.Abathur,
            "Alarak" => Commander.Alarak,
            "Artanis" => Commander.Artanis,
            "Dehaka" => Commander.Dehaka,
            "Fenix" => Commander.Fenix,
            "Horner" => Commander.Horner,
            "Karax" => Commander.Karax,
            "Kerrigan" => Commander.Kerrigan,
            "Mengsk" => Commander.Mengsk,
            "Nova" => Commander.Nova,
            "Raynor" => Commander.Raynor,
            "Stetmann" => Commander.Stetmann,
            "Stukov" => Commander.Stukov,
            "Swann" => Commander.Swann,
            "Tychus" => Commander.Tychus,
            "Vorazun" => Commander.Vorazun,
            "Zagara" => Commander.Zagara,
            "Zeratul" => Commander.Zeratul,
            _ => Commander.None
        };
    }

    public static GameMode GetGameMode(string gameMode)
    {
        return gameMode switch
        {
            "GameModeBrawlCommanders" => GameMode.BrawlCommanders,
            "GameModeBrawlStandard" => GameMode.BrawlStandard,
            "GameModeBrawl" => GameMode.BrawlStandard,
            "GameModeCommanders" => GameMode.Commanders,
            "GameModeCommandersHeroic" => GameMode.CommandersHeroic,
            "GameModeHeroicCommanders" => GameMode.CommandersHeroic,
            "GameModeGear" => GameMode.Gear,
            "GameModeSabotage" => GameMode.Sabotage,
            "GameModeStandard" => GameMode.Standard,
            "GameModeSwitch" => GameMode.Switch,
            "GameModeTutorial" => GameMode.Tutorial,
            _ => GameMode.None
        };
    }

    public static Dictionary<Commander, string> CmdrColor { get; } = new Dictionary<Commander, string>()
        {
            {     Commander.None, "#0000ff"        },
            {     Commander.Abathur, "#266a1b" },
            {     Commander.Alarak, "#ab0f0f" },
            {     Commander.Artanis, "#edae0c" },
            {     Commander.Dehaka, "#d52a38" },
            {     Commander.Fenix, "#fcf32c" },
            {     Commander.Horner, "#ba0d97" },
            {     Commander.Karax, "#1565c7" },
            {     Commander.Kerrigan, "#b021a1" },
            {     Commander.Mengsk, "#a46532" },
            {     Commander.Nova, "#f6f673" },
            {     Commander.Raynor, "#dd7336" },
            {     Commander.Stetmann, "#ebeae8" },
            {     Commander.Stukov, "#663b35" },
            {     Commander.Swann, "#ab4f21" },
            {     Commander.Tychus, "#342db5" },
            {     Commander.Vorazun, "#07c543" },
            {     Commander.Zagara, "#b01c48" },
            {     Commander.Zeratul, "#a1e7e7"  },
            {     Commander.Protoss, "#fcc828"   },
            {     Commander.Terran, "#4a4684"   },
            {     Commander.Zerg, "#6b1c92"   }
        };

    public static string GetBackgroundColor(Commander cmdr, string transparency = "33")
    {
        return $"{CmdrColor[cmdr]}{transparency}";
    }

    public static List<Commander> GetCommanders(CmdrGet cmdrGet)
    {
        return cmdrGet switch
        {
            CmdrGet.All => Enum.GetValues(typeof(Commander)).Cast<Commander>().ToList(),
            CmdrGet.NoNone => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => x != Commander.None).ToList(),
            CmdrGet.NoStd => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => (int)x > 3).ToList(),
            CmdrGet.Std => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => x != Commander.None && (int)x <= 3).ToList(),
            _ => Enum.GetValues(typeof(Commander)).Cast<Commander>().ToList(),
        };
    }

    public enum CmdrGet
    {
        All = 0,
        NoNone = 1,
        NoStd = 2,
        Std = 3
    }

    public static Breakpoint GetBreakpoint(int gameloop)
    {
        // 5min: 6240, 6720, 7200
        // 10min: 12960, 13440, 13920
        // 15min: 19680, 20160, 20640

        return gameloop switch
        {
            > 20645 => Breakpoint.All,
            >= 19680 => Breakpoint.Min15,
            >= 13930 => Breakpoint.All,
            >= 12960 => Breakpoint.Min10,
            >= 7210 => Breakpoint.All,
            >= 6240 => Breakpoint.Min5,
            _ => Breakpoint.All,
        };
    }

    public static string GenHash(ReplayDto replay)
    {
        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
        }
        sb.Append(replay.GameMode + replay.Playercount);
        sb.Append(replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum);

        // if (replay.WinnerTeam == 0)
        // {
        //     sb.Append(replay.Maxkillsum);
        // } else
        // {
        //     sb.Append(replay.Minkillsum);
        // }

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GetMd5Hash(MD5 md5Hash, string input)
    {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }
    public static List<RequestNames> GetDefaultRequestNames()
    {
        return new() {
                new("PAX", 226401, 2, 1),
                new("PAX", 10188255, 1, 1),
                new("Feralan", 8497675, 1, 1),
                new("Feralan", 1488340, 2, 1)
            };
    }

    public static string GetRegionString(int? regionId)
    {
        return regionId switch
        {
            1 => "Am",
            2 => "Eu",
            3 => "As",
            _ => ""
        };
    }

    public static string GetRatingTypeLongName(RatingType ratingType)
    {
        return ratingType switch
        {
            RatingType.Cmdr => "Commanders 3v3",
            RatingType.Std => "Standard 3v3",
            RatingType.CmdrTE => "Cmdrs 3v3 TE",
            RatingType.StdTE => "Std 3v3 TE",
            _ => ""
        };
    }

    public static List<TimePeriod> GetTimePeriods(TimePeriodGet timePeriodGet = TimePeriodGet.None)
    {
        return timePeriodGet switch
        {
            TimePeriodGet.NoNone => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => x != TimePeriod.None).ToList(),
            TimePeriodGet.Builds => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => x != TimePeriod.None && (int)x < 6).ToList(),
            TimePeriodGet.PlayerDetails => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => (int)x >= 6).ToList(),
            _ => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().ToList(),
        };
    }

    public enum TimePeriodGet
    {
        None = 0,
        NoNone = 1,
        Builds = 2,
        PlayerDetails = 3
    }

    public static (DateTime, DateTime) TimeperiodSelected(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Past90Days => (DateTime.Today.AddDays(-90), DateTime.Today),
            TimePeriod.ThisMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Today),
            TimePeriod.LastMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1), new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
            TimePeriod.ThisYear => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today),
            TimePeriod.LastYear => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), new DateTime(DateTime.Now.Year, 1, 1)),
            TimePeriod.Last2Years => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), DateTime.Today),
            TimePeriod.Patch2_60 => (new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today),
            TimePeriod.Patch2_71 => (new DateTime(2023, 01, 22), DateTime.Today),
            _ => (new DateTime(2018, 1, 1), DateTime.Today),
        };
    }

    public static string GetTimePeriodLongName(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Past90Days => "Past 90 Days",
            TimePeriod.ThisMonth => "This Month",
            TimePeriod.ThisYear => "This Year",
            TimePeriod.Last2Years => "Last Two Years",
            TimePeriod.Patch2_60 => "Patch 2.60",
            TimePeriod.LastMonth => "Last Month",
            TimePeriod.LastYear => "Last Year",
            TimePeriod.Patch2_71 => "Patch 2.71",
            _ => "All"
        };
    }

    public static TimePeriod GetTimePeriodFromDeprecatedString(string timePeriod)
    {
        return timePeriod switch
        {
            "This Month" => TimePeriod.ThisMonth,
            "Last Month" => TimePeriod.LastMonth,
            "This Year" => TimePeriod.ThisYear,
            "Last Year" => TimePeriod.LastYear,
            "Last Two Years" => TimePeriod.Last2Years,
            "Patch 2.60" => TimePeriod.Patch2_60,
            "Patch 2.71" => TimePeriod.Patch2_71,
            _ => TimePeriod.None
        };
    }
    public static readonly int MinBuildRating = 500;
    public static readonly int MaxBuildRating = 2500;

    public static IReadOnlyList<string> UnitNames = new List<string>()
    {
        "Aberration",
        "Adept",
        "AegisGuard",
        "AiurCarrier",
        "Alarak",
        "Aleksander",
        "Annihilator",
        "Aperture",
        "Apocalisk",
        "Archon",
        "ARES",
        "Ascendant",
        "AssaultGalleon",
        "AsteriaWraith",
        "Baneling",
        "Banshee",
        "Battlecruiser",
        "Blaze",
        "BroodLord",
        "BroodMutalisk",
        "BroodQueen",
        "Brutalisk",
        "Cannonball",
        "Carrier",
        "Centurion",
        "Clolarion",
        "Colossus",
        "Conservator",
        "Corruptor",
        "Corsair",
        "CovertBanshee",
        "CreeperHost",
        "Cyclone",
        "DarkArchon",
        "DarkTemplar",
        "Dehaka",
        "DeimosViking",
        "Destroyer",
        "Devourer",
        "Disruptor",
        "Dragoon",
        "DuskWing",
        "EliteMarine",
        "Energizer",
        "Firebat",
        "Firehawk",
        "Flyer",
        "Gary",
        "Ghost",
        "Goliath",
        "Guardian",
        "HammerSecurities",
        "Harbinger",
        "Havoc",
        "HeavySiegeTank",
        "Hellbat",
        "HellbatRanger",
        "Hellion",
        "HelsAngelAssault",
        "HelsAngelFighter",
        "HighArchon",
        "HighTemplar",
        "HoloDecoy",
        "HonorGuard",
        "HunterKiller",
        "HybridBehemoth",
        "HybridDestroyer",
        "HybridDominator",
        "HybridNemesis",
        "HybridReaver",
        "Hydralisk",
        "Hyperion",
        "Immortal",
        "Impaler",
        "Inducer",
        "InfestedBanshee",
        "InfestedBunker",
        "InfestedCivilian",
        "InfestedDiamondback",
        "InfestedDiamondbackSnarePlaceholder",
        "InfestedLiberator",
        "InfestedLiberatorViralSwarm",
        "InfestedMarine",
        "InfestedSiegeTank",
        "InfestedThor",
        "Infestor",
        "Kaldalis",
        "Kerrigan",
        "Legionnaire",
        "Leviathan",
        "Liberator",
        "Lurker",
        "Marauder",
        "MarauderCommando",
        "Marine",
        "Medic",
        "Medivac",
        "Mirage",
        "Mojo",
        "Mothership",
        "MothershipTaldarim",
        "Mutalisk",
        "Nikara",
        "Nova",
        "Nux",
        "Observer",
        "Oracle",
        "Overseer",
        "Pariah",
        "Phoenix",
        "Praetor",
        "PrimalGuardian",
        "PrimalHost",
        "PrimalHydralisk",
        "PrimalIgniter",
        "PrimalZergling",
        "PrimalMutalisk",
        "PrimalRavasaur",
        "PrimalRoach",
        "PrimalUltralisk",
        "Probius",
        "PurifierAdept",
        "PurifierCarrier",
        "PurifierColossus",
        "PurifierDisruptor",
        "PurifierImmortal",
        "PurifierObserver",
        "PurifierScout",
        "PurifierTalis",
        "PurifierTempest",
        "Queen",
        "RaidLiberator",
        "Raptorling",
        "Rattlesnake",
        "Ravager",
        "Raven",
        "RavenTypeII",
        "Reaper",
        "Reaver",
        "Roach",
        "Sam",
        "ScienceVessel",
        "Scourge",
        "Sentinel",
        "Sentry",
        "ShadowGuard",
        "SiegeBreaker",
        "SiegeDrone",
        "SiegeDroneMinion",
        "SiegeTank",
        "Sirius",
        "SkyFury",
        "Slayer",
        "SovereignBattlecruiser",
        "SpecOpsGhost",
        "Spectre",
        "SpiderMine",
        "Splitterling",
        "SpookySkeleton",
        "Stalker",
        "StalkerGhost",
        "StrikeFighter",
        "StrikeGoliath",
        "SuperGary",
        "Supplicant",
        "SwarmHost",
        "Swarmling",
        "SwarmQueen",
        "Taldarin",
        "TechFoundry",
        "Tempest",
        "TheiaRaven",
        "Thor",
        "Titan",
        "Torrasque",
        "Trooper",
        "TrooperAA",
        "TrooperFlamethrower",
        "TrooperImproved",
        "Tychus",
        "Tyrannozor",
        "Ultralisk",
        "Umbra",
        "Vanguard",
        "Vega",
        "Viking",
        "VileRoach",
        "Viper",
        "VoidRay",
        "VolatileInfested",
        "Volt",
        "Vulture",
        "Walker",
        "Warbringer",
        "Ward",
        "WarHound",
        "WarPig",
        "WarPrism",
        "WidowMine",
        "Wraith",
        "Wrathwalker",
        "Zagara",
        "Zealot",
        "Zergling"
    }.AsReadOnly();

    public static IReadOnlyList<string> UpgradeNames = new List<string>() {
        "AberrationProtectiveCover",
        "AdeptPiercingAttack",
        "AdeptPsionicProjection",
        "AdvancedRepairSystems",
        "AiurCarrierLaunchSpeedUpgrade",
        "AiurCarrierRepairDrones",
        "AlarakImposingPresence",
        "AlarakTelekinesis",
        "AnabolicSynthesis",
        "ApertureKeyhole",
        "ArchonBanned",
        "AresClassWeaponsSystem",
        "ArtilleryMengskGarrison1",
        "ArtilleryMengskGarrison2",
        "ArtilleryMengskGarrison3",
        "ArtilleryMengskRange",
        "AscendantChaoticAttunement",
        "AscendantMindBlast",
        "AscendantPowerOverwhelming",
        "BanelingStetmannExtraDamage",
        "BanelingStetmannMovementSpeed",
        "BansheeCloak",
        "BansheeShockwaveMissileBattery",
        "BansheeSpeed",
        "BattlecruiserBehemothReactor",
        "BattlecruiserEnableSpecializations",
        "BattlecruiserMengskRangeAura",
        "BileLauncherArtilleryDucts",
        "BlinkTech",
        "BroodLordPorousCartilage",
        "BroodLordStetmannBombers",
        "BroodLordStetmannYamato",
        "BroodMutaliskSeveringGlave",
        "BroodMutaliskSunderingGlave",
        "BroodMutaliskViciousGlave",
        "BroodQueenEnhancedMitochondria",
        "BunkerDepotMengskRange",
        "CarrierLaunchSpeedUpgrade",
        "CentrificalHooks",
        "CenturionDarkcoil",
        "CenturionShadowCharge",
        "Charge",
        "ChitinousPlating",
        "CloakDistortionField",
        "ClolarionInterdictors",
        "ClolarionSkin_Forged",
        "ClolarionSkin_Ihanrii",
        "ClolarionSkin_None",
        "CommanderStetmann",
        "ConservatorOptimizedEmitters",
        "CorruptorStetmannBiggerAoE",
        "CorruptorStetmannCausticSpray",
        "CorsairStealthDrive",
        "CovertBansheeAdvancedCloakingField",
        "CycloneLockOnDamageUpgrade",
        "CycloneLockOnRangeUpgrade",
        "CycloneRapidFireLaunchers",
        "DarkArchonArgusCrystal",
        "DarkTemplarBlinkUpgrade",
        "DarkTemplarShadowFury",
        "DarkTemplarVoidStasis",
        "DehakaArmorLevel1",
        "DehakaArmorLevel2",
        "DehakaArmorLevel3",
        "DehakaAttacksLevel1",
        "DehakaAttacksLevel2",
        "DehakaAttacksLevel3",
        "DehakaChitinousPlating",
        "DehakaDeadlyReach",
        "DehakaEatLevel1",
        "DehakaEatLevel2",
        "DehakaEatLevel3",
        "DehakaHeroLevel1",
        "DehakaHeroLevel10",
        "DehakaHeroLevel11",
        "DehakaHeroLevel12",
        "DehakaHeroLevel13",
        "DehakaHeroLevel14",
        "DehakaHeroLevel15",
        "DehakaHeroLevel2",
        "DehakaHeroLevel3",
        "DehakaHeroLevel4",
        "DehakaHeroLevel5",
        "DehakaHeroLevel6",
        "DehakaHeroLevel7",
        "DehakaHeroLevel8",
        "DehakaHeroLevel9",
        "DehakaHeroStage2",
        "DehakaHeroStage3",
        "DehakaJumpLevel1",
        "DehakaJumpLevel2",
        "DehakaJumpLevel3",
        "DehakaKeenSenses",
        "DehakaPrimalGuardianExplosiveSpores",
        "DehakaPrimalGuardianPrimordialFury",
        "DehakaPrimalHydraliskMuscularAugments",
        "DehakaPrimalIgniterConcentratedFire",
        "DehakaPrimalMutaliskPrimalReconstitution",
        "DehakaPrimalMutaliskShiftingCarapace",
        "DehakaPrimalMutaliskSlicingGlave",
        "DehakaPrimalRavasaurDissolvingAcid",
        "DehakaPrimalRavasaurEnlargedParotidGlands",
        "DehakaPrimalRegenerationLevel1",
        "DehakaPrimalRegenerationLevel2",
        "DehakaPrimalRegenerationLevel3",
        "DehakaPrimalRoachGlialReconstitution",
        "DehakaPrimalUltraliskBrutalCharge",
        "DehakaPrimalUltraliskHealingAdaptation",
        "DehakaPrimalUltraliskImpalingStrike",
        "DehakaRoarLevel1",
        "DehakaRoarLevel2",
        "DehakaRoarLevel3",
        "DehakaScorchingBreath",
        "DehakaTyrannozorBarrageofSpikes",
        "DehakaTyrannozorPlaceSpikedHide",
        "DehakaTyrannozorTyrantsProtection",
        "DevourerCorrosiveSpray",
        "DiggingClaws",
        "DisruptorCloakingModule",
        "DisruptorPurificationEcho",
        "DragoonSingularityCharge",
        "DragoonTrillicCompressionMesh",
        "DrillClaws",
        "EliteMarineLaserTargetingSystem",
        "EliteMarineSuperStimpack",
        "EnergizerRapidRecharging",
        "EnhancedShockwaves",
        "EnhancedTargeting",
        "EvolveGroovedSpines",
        "EvolveMuscularAugments",
        "ExaltedShield",
        "ExtendedThermalLance",
        "FenixAStrongHeart",
        "FenixExtendedThermalLance",
        "FenixObservationProtocol",
        "FenixPurifierArmaments",
        "FirebatIncineratorGauntlets",
        "FortificationBarrier",
        "GaryStetmannPlaceUsed",
        "GhostMoebiusReactor",
        "GlialReconstitution",
        "GuardianProlongedDispersion",
        "HavocBloodshardResonance",
        "HavocCloakingModule",
        "HavocDetectWeakness",
        "HeavySiegeTankGraduatingRange",
        "HellbatInfernalPlating",
        "HellbatRangerInfernalPreIgniter",
        "HellbatRangerJumpJetAssault",
        "HeroDeathPenalty",
        "HeroJaina",
        "HeroSelected",
        "HeroSylvanas",
        "HeroThrall",
        "HighCapacityBarrels",
        "HighTemplarBanned",
        "HighTemplarKhaydarinAmulet",
        "HighTemplarPlasmaSurge",
        "HiSecAutoTracking",
        "HornerArmorLevel1",
        "HornerArmorLevel2",
        "HornerArmorLevel3",
        "HornerAsteriaWraithTriggerOverride",
        "HornerAsteriaWraithUnregisteredCloakingSystem",
        "HornerDeimosVikingShredderRounds",
        "HornerDeimosVikingWILDMissiles",
        "HornerHellbatImmolationFluid",
        "HornerHellbatWildfireExplosives",
        "HornerHellionAerosolStimEmitters",
        "HornerHellionTarBombs",
        "HornerReaperJetPackOverdrive",
        "HornerReaperLD9ClusterCharges",
        "HornerSovereignBattlecruiserOverchargedReactor",
        "HornerStrikeFighterNapalmPayload",
        "HornerTacticalJump",
        "HornerTheiaRavenMultiThreadedSensors",
        "HornerWeaponsLevel1",
        "HornerWeaponsLevel2",
        "HornerWeaponsLevel3",
        "HornerWidowMineBlackMarketLaunchers",
        "HornerWidowMineExecutionerMissiles",
        "HydraliskAncillaryCarapace",
        "HydraliskFrenzyKerrigan",
        "HydraliskStetmannDamage",
        "HydraliskStetmannMovementSpeed",
        "HydraliskStetmannRange",
        "ImmortalImprovedBarrier",
        "ImmortalShadowCannon",
        "IncludeStandardRacesInCommanderSelect",
        "InfestedBansheeBracedExoskeleton",
        "InfestedBansheeRapidHibernation",
        "InfestedBunkerCalcifiedArmor",
        "InfestedCivilianAnaerobicEnhancement",
        "InfestedCivilianBroodlingGestation",
        "InfestedDiamondbackSaturatedCultures",
        "InfestedLiberatorViralContamination",
        "InfestedMarinePlaguedMunitions",
        "InfestedMarineRetinalAugmentation",
        "InfestedSiegeTankAcidicEnzymes",
        "InfestorEnergyUpgrade",
        "InfestorStetmannBonusRavager",
        "InfestorStetmannRecharge",
        "InfestStructureAggressiveIncubation",
        "KaldalisEmpoweredBlades",
        "KaldalisSkin_Forged",
        "KaldalisSkin_Ihanrii",
        "KaldalisSkin_None",
        "KaraxOrbitalStrikeBeaconStun",
        "KeironBioWeaponsLevel1",
        "KerriganAbilityEfficiency",
        "KerriganChainReaction",
        "KerriganDesolateQueenActive",
        "KerriganFury",
        "KerriganHeroicFortitude",
        "LatentCharge",
        "LegionnaireCharge",
        "LiberatorAGRangeUpgrade",
        "LocustLifetimeIncrease",
        "LurkerRange",
        "LurkerSeismicSpines",
        "LurkerStetmannChannelingSpines",
        "LurkerStetmannTunnelingBurstRange",
        "MaelstromRounds",
        "MarauderCommandoMagrailMunitions",
        "MarauderCommandoSuppressionShells",
        "MarauderMengskSlow",
        "MechTransformationSpeedMengsk",
        "MedicStabilizerMedpacks",
        "MedivacCaduceusReactor",
        "MedivacIncreaseSpeedBoost",
        "MedivacMengskDoubleHealBeam",
        "MedivacMengskPermanentCloak",
        "MedivacMengskSiegeTankAirlift",
        "MengskRoyalGuardXP",
        "MengskStructureArmor",
        "MengskTrooperArmorsLevel1",
        "MengskTrooperArmorsLevel2",
        "MengskTrooperArmorsLevel3",
        "MengskTrooperWeaponsLevel1",
        "MengskTrooperWeaponsLevel2",
        "MengskTrooperWeaponsLevel3",
        "MicrobialShroud",
        "MiragePhasingArmor",
        "ModifiedGait",
        "MojoSuppressionProcedure",
        "MultilockTargetingSystems",
        "MuscularAugmentsKerrigan",
        "MutaliskRapidRegeneration",
        "MutaliskSunderingGlaveKerrigan",
        "MutaliskViciousGlave",
        "NeuralParasite",
        "NovaCaduceusReactor",
        "NovaCovertTriage",
        "NovaGhostVisor",
        "NovaPhaseReactorSuit",
        "NovaRifle",
        "NovaShotgun",
        "NovaTacticalStealthSuit",
        "ObserverGraviticBooster",
        "OptimizedOrdnance",
        "OracleStasisCalibration",
        "overlordspeed",
        "PersonalCloaking",
        "PhoenixDoubleGravitonBeam",
        "PhoenixRangeUpgrade",
        "PlayerAttributeResourcesSpentPer500",
        "PlayerStateGameOver",
        "PlayerStateVictory",
        "PremiumReward",
        "ProtossAirArmorLevel1Multi",
        "ProtossAirArmorLevel2Multi",
        "ProtossAirArmorLevel3Multi",
        "ProtossAirArmorsLevel1",
        "ProtossAirArmorsLevel2",
        "ProtossAirArmorsLevel3",
        "ProtossAirWeaponsLevel1",
        "ProtossAirWeaponsLevel1Multi",
        "ProtossAirWeaponsLevel2",
        "ProtossAirWeaponsLevel2Multi",
        "ProtossAirWeaponsLevel3",
        "ProtossAirWeaponsLevel3Multi",
        "ProtossGroundArmorLevel1Multi",
        "ProtossGroundArmorLevel2Multi",
        "ProtossGroundArmorLevel3Multi",
        "ProtossGroundArmorsLevel1",
        "ProtossGroundArmorsLevel2",
        "ProtossGroundArmorsLevel3",
        "ProtossGroundWeaponsLevel1",
        "ProtossGroundWeaponsLevel1Multi",
        "ProtossGroundWeaponsLevel2",
        "ProtossGroundWeaponsLevel2Multi",
        "ProtossGroundWeaponsLevel3",
        "ProtossGroundWeaponsLevel3Multi",
        "ProtossShieldsLevel1",
        "ProtossShieldsLevel1Multi",
        "ProtossShieldsLevel2",
        "ProtossShieldsLevel2Multi",
        "ProtossShieldsLevel3",
        "ProtossShieldsLevel3Multi",
        "PsiStormTech",
        "PulsarDampener",
        "PunisherGrenades",
        "PurifierColossusFireBeam",
        "PurifierTempestDisintegration",
        "RaidLiberatorSmartServos",
        "RavagerBloatedBileDucts",
        "RavagerPotentBile",
        "RavenCorvidReactor",
        "RavenEnhancedMunitions",
        "RaynorAfterburners",
        "RaynorHyperionAdvancedTargetingSystems",
        "RaynorMercenaryMunitions",
        "ReaverScarabHousing",
        "ReaverSolaritePayload",
        "RefinerySkinBiomass",
        "RefinerySkinCybros",
        "RefinerySkinHighlight",
        "RefinerySkinHologram",
        "RefinerySkinJade",
        "RefinerySkinMineral",
        "RefinerySkinNone",
        "RefinerySkinPsionic",
        "RefinerySkinRedstone",
        "RefinerySkinSepia",
        "RefinerySkinShadow",
        "RefinerySkinSnow",
        "RefinerySkinSolarite",
        "RefinerySkinVoid",
        "SandboxModeInfiniteEnergy",
        "SandboxModeInfiniteMinerals",
        "SandboxModeRapidCooldown",
        "ScienceVesselDefensiveMatrix",
        "ScienceVesselImprovedNanoRepair",
        "ScourgeVirulentSpores",
        "ScoutCombatSensorArray",
        "SentinelReconstruction",
        "ShieldCompulsion",
        "ShieldWall",
        "ShrikeTurret",
        "SiegeTankAdvancedSiegeTech",
        "SkinKerriganAscended",
        "SkinKerriganGhost",
        "SkinLeviathan_Ice",
        "SlayerPhasingArmor",
        "SmartServos",
        "SpecOpsGhostEMPRound",
        "SpecOpsGhostMale",
        "SpecOpsGhostTripleTap",
        "SplitterlingCorrosiveAcid",
        "SplitterlingRupture",
        "StalkerPhaseReactor",
        "StetmannAirArmorsLevel1",
        "StetmannAirArmorsLevel2",
        "StetmannAirArmorsLevel3",
        "StetmannAirWeaponsLevel1",
        "StetmannAirWeaponsLevel2",
        "StetmannAirWeaponsLevel3",
        "StetmannGroundArmorsLevel1",
        "StetmannGroundArmorsLevel2",
        "StetmannGroundArmorsLevel3",
        "StetmannMeleeWeaponsLevel1",
        "StetmannMeleeWeaponsLevel2",
        "StetmannMeleeWeaponsLevel3",
        "StetmannMissileWeaponsLevel1",
        "StetmannMissileWeaponsLevel2",
        "StetmannMissileWeaponsLevel3",
        "Stimpack",
        "StrikeGoliathAresClassTargetingSystem",
        "StrikeGoliathLockdownMissiles",
        "StukovAleksanderCrash",
        "SupplicantBloodShields",
        "SupplicantSoulAugmentation",
        "SupplicantStarlightAirAttack",
        "SwannCycloneMagFieldAccelerator",
        "SwannCycloneTargetingOptics",
        "SwannRegenerativeBioSteel",
        "SwarmHostPressurizedGlands",
        "SwarmQueenBioMechanicalTransfusion",
        "TaldarinGravimetricOverload",
        "TaldarinSkin_Forged",
        "TaldarinSkin_Ihanrii",
        "TaldarinSkin_None",
        "TalisDebilitationSystem",
        "TalisSkin_Forged",
        "TalisSkin_Ihanrii",
        "TalisSkin_None",
        "TalisSkin_RingAlternate",
        "TerranBuildingArmor",
        "TerranInfantryArmorLevel1Multi",
        "TerranInfantryArmorLevel2Multi",
        "TerranInfantryArmorLevel3Multi",
        "TerranInfantryArmorsLevel1",
        "TerranInfantryArmorsLevel2",
        "TerranInfantryArmorsLevel3",
        "TerranInfantryWeaponsLevel1",
        "TerranInfantryWeaponsLevel1Multi",
        "TerranInfantryWeaponsLevel2",
        "TerranInfantryWeaponsLevel2Multi",
        "TerranInfantryWeaponsLevel3",
        "TerranInfantryWeaponsLevel3Multi",
        "TerranShipWeaponsLevel1",
        "TerranShipWeaponsLevel1Multi",
        "TerranShipWeaponsLevel2",
        "TerranShipWeaponsLevel2Multi",
        "TerranShipWeaponsLevel3",
        "TerranShipWeaponsLevel3Multi",
        "TerranVehicleAndShipArmorsLevel1",
        "TerranVehicleAndShipArmorsLevel2",
        "TerranVehicleAndShipArmorsLevel3",
        "TerranVehicleAndShipPlatingLevel1Multi",
        "TerranVehicleAndShipPlatingLevel2Multi",
        "TerranVehicleAndShipPlatingLevel3Multi",
        "TerranVehicleWeaponsLevel1",
        "TerranVehicleWeaponsLevel1Multi",
        "TerranVehicleWeaponsLevel2",
        "TerranVehicleWeaponsLevel2Multi",
        "TerranVehicleWeaponsLevel3",
        "TerranVehicleWeaponsLevel3Multi",
        "Thor330mmBarrageCannon",
        "ThorBarrageCannonShow",
        "ThorMengskArmorAura",
        "TunnelingClaws",
        "TychusArmorLevel1",
        "TychusArmorLevel2",
        "TychusArmorLevel3",
        "TychusArmorLevel4",
        "TychusArmorLevel5",
        "TychusAttacksLevel1",
        "TychusAttacksLevel2",
        "TychusAttacksLevel3",
        "TychusAttacksLevel4",
        "TychusAttacksLevel5",
        "TychusEnduranceSupplements",
        "TychusFirstOnesontheHouseApplied",
        "TychusFlashforceGDMVisor",
        "TychusITCETriggers",
        "UltraliskBurrowCharge",
        "UltraliskBurrowChargeMechanicalStun",
        "UltraliskStetmannArmor",
        "UltraliskStetmannMechanicalLifeLeech",
        "UltraliskTissueAssimilation",
        "VanadiumPlatingController",
        "VanadiumPlatingInfantry",
        "VanadiumPlatingVehicles",
        "VanguardFusionMortars",
        "VanguardMatterDispersion",
        "VehicleAdvancedOptics",
        "VikingMengskSpeed",
        "VikingPhobosWeaponsSystem",
        "VikingRipwaveMissiles",
        "VileRoachAdaptivePlating",
        "VileRoachHydriodicBile",
        "ViperVirulentMicrobes",
        "VoidRaySpeedUpgrade",
        "VorazunStrikefromtheShadows",
        "VorazunWeaponStalkerGold",
        "VorazunWeaponVoidRayPurple",
        "VorazunWeaponVoidRayRed",
        "VultureCerberusMine",
        "VultureReplenishableMagazine",
        "WarbringerPurificationBlast",
        "WarbringerSkin_Forged",
        "WarbringerSkin_Ihanrii",
        "WarbringerSkin_None",
        "WraithChamber",
        "WraithCloak",
        "WraithPulseAmplifier",
        "WrathwalkerAerialTracking",
        "WrathwalkerPowerCycling",
        "ZagaraBroodmother",
        "ZagaraHeroicFortitude",
        "ZagaraMedusaBlades",
        "ZealotWhirlwind",
        "ZeratulArtifactTier1",
        "ZeratulArtifactTier2",
        "ZeratulArtifactTier3",
        "ZergFlyerArmorsLevel1",
        "ZergFlyerArmorsLevel2",
        "ZergFlyerArmorsLevel3",
        "ZergFlyerAttacksLevel1Multi",
        "ZergFlyerAttacksLevel2Multi",
        "ZergFlyerAttacksLevel3Multi",
        "ZergFlyerCarapaceLevel1Multi",
        "ZergFlyerCarapaceLevel2Multi",
        "ZergFlyerCarapaceLevel3Multi",
        "ZergFlyerWeaponsLevel1",
        "ZergFlyerWeaponsLevel2",
        "ZergFlyerWeaponsLevel3",
        "ZergGroundArmorsLevel1",
        "ZergGroundArmorsLevel2",
        "ZergGroundArmorsLevel3",
        "ZergGroundCarapaceLevel1Multi",
        "ZergGroundCarapaceLevel2Multi",
        "ZergGroundCarapaceLevel3Multi",
        "zerglingattackspeed",
        "ZerglingHardenedCarapace",
        "zerglingmovementspeed",
        "ZerglingShreddingClaws",
        "ZerglingStetmannAttackSpeed",
        "ZerglingStetmannHardenedShield",
        "ZerglingStetmannMovementSpeed",
        "ZergMeleeAttacksLevel1Multi",
        "ZergMeleeAttacksLevel2Multi",
        "ZergMeleeAttacksLevel3Multi",
        "ZergMeleeWeaponsLevel1",
        "ZergMeleeWeaponsLevel2",
        "ZergMeleeWeaponsLevel3",
        "ZergMissileAttacksLevel1Multi",
        "ZergMissileAttacksLevel2Multi",
        "ZergMissileAttacksLevel3Multi",
        "ZergMissileWeaponsLevel1",
        "ZergMissileWeaponsLevel2",
        "ZergMissileWeaponsLevel3"
}.AsReadOnly();

    public static IReadOnlyDictionary<PlayerId, bool> SoftBans = new Dictionary<PlayerId, bool>()
    {
        { new PlayerId(4408073, 1, 2), true}, // MemoriLuvLow
        { new PlayerId(10195430, 1, 2), true }, //HenyaMyWaifu
        { new PlayerId(5310262, 1, 1), true }, //SunayStinks
        { new PlayerId(10392393, 1, 2), true }, //EnTaroGura
        { new PlayerId(12788234, 1, 1), true }, //Amemiya
        { new PlayerId(9846569, 1, 2), true }, //Zergling
        { new PlayerId(9207965, 1, 2), true }, //kun
        { new PlayerId(12967800, 1, 1), true }, //AAAAAAAAAAAA
        { new PlayerId(1608587, 2, 3), true }, //Amemiya
        { new PlayerId(10570273, 1, 2), true }, //holymackerel
    };

    public static IReadOnlyDictionary<int, bool> SoftBanDsstatsIds = new Dictionary<int, bool>()
    {
        { 13642, true }, // MemoriLuvLow
        { 86515, true }, //	HenyaMyWaifu
        { 123640, true }, //	SunayStinks
        { 128536, true }, //	EnTaroGura
        { 128563, true }, //	Amemiya
        { 151223, true }, //	Zergling
        { 188323, true }, //	kun
        { 196041, true }, //	AAAAAAAAAAAA
        { 196042, true }, //	Amemiya
        { 199097, true }, //	holymackerel
    };

    public static IReadOnlyDictionary<int, bool> SoftBanArcadeIds = new Dictionary<int, bool>()
    {
        { 44376, true }, //	Zergling
        { 26005, true }, //	SunayStinks
        { 35247, true }, //	MemoriLuvLow
        { 306232, true }, //	kun
        { 322834, true }, //	holymackerel
        { 71165, true }, //	HenyaMyWaifu
        { 69116, true }, //	EnTaroGura
        { 84364, true }, //	Amemiya
    };

    public static bool IsMaui { get; set; }
    public static int MauiWidth { get; set; }
    public static int MauiHeight { get; set; }
    public static RequestNames? MauiRequestNames { get; set; }
    //public static string SqliteConnectionString { get; set; } = string.Empty;
    //public static string MysqlConnectionString { get; set; } = string.Empty;

    public const string ReplayBlobDir = "/data/ds/replayblobs";
    public const string MysqlFilesDir = "/data/mysqlfiles";
}

public class LatestReplayEventArgs : EventArgs
{
    public ReplayDetailsDto? LatestReplay { get; init; }
}

public record DbImportOptions
{
    public string ImportConnectionString { get; set; } = string.Empty;
}