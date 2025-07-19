using s2protocol.NET;
using s2protocol.NET.Models;
using System.Reflection;

namespace dsstats.challenge;

public static partial class Parse
{
    internal static string _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    private static readonly List<string> IgnoreUnits = new List<string>
    {
        "UnitBirthBar",
        "MineralIncome",
        "Biomass",
        "GuardianShell",
        "MineralIncome",
        "Biomass",
        "DehakaCreeperHostExplosiveCreeper",
        "AdeptPhaseShift",
        "SprayDecal",
        "InfestedTerransEgg",
        "LocustMPPrecursor",
        "BroodlingEscort",
        "KD8Charge",
        "SprayDecal",
        "AutoTurret",
        "BioMechanicalRepairDrone",
        "TychusRattlesnakeDeployRevitalizer",
        "AiurCarrierRepairDrone",
        "DarkPylon",
        "SplitterlingSpawn",
        "Broodling",
        "InfestedDiamondbackSnarePlaceholder",
        "Interceptor",
        "PurifierBeam",
        "PrimalHostLocust",
        "LocustMPPrecursor",
        "PrimalWurmScan",
        "SpiderMine",
        "HyperionPointDefenseDrone",
        "CreepTumorBurrowed",
        "ClolarionBomber",
        "PurifierAdeptShade",
        "PurifierTalisShade",
        "HornerReaperKD8Charge",
        "ReaperLD9ClusterCharges",
        "SiriusWarhoundTurret",
        "AutoTurret",
        "RattlesnakeDeployRevitalizer",
        "SNARE_PLACEHOLDER",
        "Locust",
        "DisruptorPhased",
        "DehakaTyrannozorPlaceSpikedHide",
        "PurifierInterceptor",
        "HornerAssaultDrone"
    };

    private static readonly List<string> DefensiveUnits = new List<string>()
    {
        "RailgunTurretPermanent",
        "MissileTurret",
        "RailgunTurret",
        "PhotonCannonPermanent",
        "SpineCrawlerPermanent",
        "SporeCrawlerPermanent",
        "BileLauncher",
        "ShieldBattery",
        "KhaydarinMonolith",
        "PrimalWurm",
        "GreaterPrimalWurm",
        "DrakkenLaserDrill",
        "DevastationTurret",
        "BunkerPermanent",
        "PhotonCannonPermanentPurifier",
        "PhotonCannonPermanentTaldarim",
        "ToxicNest",
    };

    private static readonly List<string> BrawlUnits = new List<string>()
    {
        "SiegeBreaker",
        "HybridNemesis",
        "HelsAngelFighter",
        "HybridBehemoth",
        "HybridDestroyer",
        "HybridDominator",
        "HammerSecurities",
        "HybridReaver",
        "WarPig",
    };

    public static DsReplay? GetDsReplay(Sc2Replay sc2Replay)
    {
        ArgumentNullException.ThrowIfNull(sc2Replay.Details);
        ArgumentNullException.ThrowIfNull(sc2Replay.Metadata);
        ArgumentNullException.ThrowIfNull(sc2Replay.TrackerEvents);

        DsReplay replay = new()
        {
            FileName = sc2Replay.FileName
        };

        if (sc2Replay.Details.Title.EndsWith("TE"))
        {
            replay.TournamentEdition = true;
        }

        ParseDetails(replay, sc2Replay.Details);
        ParseMetadata(replay, sc2Replay.Metadata);
        ParseTrackerEventsNg(replay, sc2Replay.TrackerEvents);

        if (replay.Duration == 0)
        {
            replay.Duration = replay.Players.Select(s => s.Duration).Max();
        }

        return replay;
    }

    private static void ParseMetadata(DsReplay replay, ReplayMetadata metadata)
    {
        foreach (MetadataPlayer metaPlayer in metadata.Players)
        {
            if (replay.Players.Count < metaPlayer.PlayerID)
            {
                continue;
            }

            replay.Players[metaPlayer.PlayerID - 1].APM = metaPlayer.APM;
            replay.Players[metaPlayer.PlayerID - 1].SelectedRace = metaPlayer.SelectedRace;
        }
    }

    private static float CheckArea(int x1, int y1, int x2,
                  int y2, int x3, int y3)
    {
        return MathF.Abs((x1 * (y2 - y3) +
                                x2 * (y3 - y1) +
                                x3 * (y1 - y2)) / 2.0f);
    }

    private static bool CheckSquare(int[] teamArea, int x, int y)
    {
        int x1 = teamArea[0];
        int y1 = teamArea[1];
        int x2 = teamArea[2];
        int y2 = teamArea[3];
        int x3 = teamArea[4];
        int y3 = teamArea[5];
        int x4 = teamArea[6];
        int y4 = teamArea[7];

        float A = CheckArea(x1, y1, x2, y2, x3, y3) +
                  CheckArea(x1, y1, x4, y4, x3, y3);

        float A1 = CheckArea(x, y, x1, y1, x2, y2);

        float A2 = CheckArea(x, y, x2, y2, x3, y3);

        float A3 = CheckArea(x, y, x3, y3, x4, y4);

        float A4 = CheckArea(x, y, x1, y1, x4, y4);

        return (A == A1 + A2 + A3 + A4);
    }

    private static string FixUnitName(string name, string? race)
    {
        // raynor viking
        if (name == "DuskWings") return "DuskWing";
        // stukov lib
        if (name == "InfestedLiberatorViralSwarm") return "InfestedLiberator";
        // Zagara
        if (name == "InfestedAbomination") return "Aberration";

        if (name == "TychusTychus") return "Tychus";
        if (name == "DehakaHero") return "Dehaka";

        if (race == "Mengsk" && name == "Marauder") return "AegisGuard";

        if (name == race) return name;

        if (race != null && race != "Zerg")
        {
            if (name.StartsWith(race)) return name.Substring(race.Length);
            if (name.EndsWith(race)) return name[..^race.Length];
        }

        if (name.Contains("Starlight")) return name.Replace("Starlight", "");
        if (name.Contains("Lightweight")) return name.Replace("Lightweight", "");
        if (name.StartsWith("Hero") && name.EndsWith("WaveUnit")) return name[4..^8];
        if (name.EndsWith("MP")) return name[0..^2];
        if (name.EndsWith("Alternate")) return name[0..^9];

        return name;
    }


}


