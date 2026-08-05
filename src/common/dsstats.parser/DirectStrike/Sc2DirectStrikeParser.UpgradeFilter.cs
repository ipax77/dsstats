
namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private static bool FilterUpgrades(string upgradeName)
    {
        if (ExactMatches.Contains(upgradeName))
        {
            return true;
        }

        foreach (string pattern in StartsWithPatterns)
        {
            if (upgradeName.StartsWith(pattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (string pattern in EndsWithPatterns)
        {
            if (upgradeName.EndsWith(pattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (string pattern in ContainsPatterns)
        {
            if (upgradeName.Contains(pattern, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly HashSet<string> ExactMatches = new(StringComparer.Ordinal)
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
        "DehakaPrimalWurm",
    };

    private static readonly string[] StartsWithPatterns =
    [
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
    ];

    private static readonly string[] EndsWithPatterns =
    [
        "Disable",
        "Enable",
        "Starlight",
        "Modification",
        "Bonus",
        "Bonus10"
    ];

    private static readonly string[] ContainsPatterns =
    [
        "Worker",
        "PlaceEvolved"
    ];
}
