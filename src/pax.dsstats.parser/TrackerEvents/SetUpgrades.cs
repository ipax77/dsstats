using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;

public static partial class Parse
{
    public static void SetUpgradesNG(DsReplay replay, List<SUpgradeEvent> upgradeEvents)
    {
        HashSet<string> Modes = new();

        for (int i = 0; i < upgradeEvents.Count; i++)
        {
            var upgradeEvent = upgradeEvents[i];

            if (upgradeEvent.Gameloop == 0
                && (upgradeEvent.UpgradeTypeName.StartsWith("Mutation")
                    || upgradeEvent.UpgradeTypeName.StartsWith("GameMode")))
            {
                Modes.Add(upgradeEvent.UpgradeTypeName);
            }

            if (upgradeEvent.Gameloop < 2) continue;
            if (upgradeEvent.PlayerId < 1) continue;
            if (upgradeEvent.PlayerId > 6) continue;

            var player = replay.Players.FirstOrDefault(f => f.Pos == upgradeEvent.PlayerId);

            if (player == null) continue;

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

            if (upgradeEvent.UpgradeTypeName.Contains("Level"))
            {
                string urace = player.Race;
                if (player.Race == "Zagara" || player.Race == "Abathur" || player.Race == "Kerrigan")
                    urace = "Zerg";
                else if (player.Race == "Alarak" || player.Race == "Artanis" || player.Race == "Vorazun" || player.Race == "Fenix" || player.Race == "Karax" || player.Race == "Zeratul")
                    urace = "Protoss";
                else if (player.Race == "Raynor" || player.Race == "Swann" || player.Race == "Nova" || player.Race == "Stukov")
                    urace = "Terran";
                if (!upgradeEvent.UpgradeTypeName.StartsWith(urace)) continue;
            }

            player.Upgrades.Add(new()
            {
                Gameloop = upgradeEvent.Gameloop,
                Upgrade = upgradeEvent.UpgradeTypeName,
                Count = upgradeEvent.Count
            });
        }

        // replay.Mutations = Modes.Where(x => x.StartsWith("Mutation")).ToList();
        replay.Mutations = new(Modes);
        SetGameModeNg(replay);
    }
}