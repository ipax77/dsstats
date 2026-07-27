using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private static HashSet<string> GetGameModeUpgradeNames(Sc2Replay replay)
    {
        HashSet<string> modes = new(StringComparer.Ordinal);

        foreach (SUpgradeEvent upgradeEvent in replay.TrackerEvents?.SUpgradeEvents ?? [])
        {
            if (upgradeEvent.Gameloop != 0)
            {
                continue;
            }

            string upgradeTypeName = upgradeEvent.UpgradeTypeName;
            if (upgradeTypeName.StartsWith("GameMode", StringComparison.Ordinal)
                || upgradeTypeName.StartsWith("Mutation", StringComparison.Ordinal))
            {
                modes.Add(upgradeTypeName);
            }
        }

        return modes;
    }

    private static GameMode GetGameMode(HashSet<string> modes, int playerCount)
    {
        if (playerCount == 1)
        {
            return GameMode.Tutorial;
        }

        bool isBrawl = false;
        bool isCommanders = false;
        bool isStandard = false;

        foreach (string mode in modes)
        {
            if (mode == "GameModeBrawl")
            {
                isBrawl = true;
            }
            else if (mode == "GameModeBrawlCommanders")
            {
                return GameMode.BrawlCommanders;
            }
            else if (mode == "GameModeBrawlStandard")
            {
                return GameMode.BrawlStandard;
            }
            else if (mode is "GameModeHeroicCommanders" or "GameModeCommandersHeroic")
            {
                return GameMode.CommandersHeroic;
            }
            else if (mode is "GameModeCommanders" or "MutationCommanders")
            {
                isCommanders = true;
            }
            else if (mode == "GameModeStandard")
            {
                isStandard = true;
            }
            else if (mode == "GameModeGear")
            {
                return GameMode.Gear;
            }
            else if (mode == "GameModeSwitch")
            {
                return GameMode.Switch;
            }
            else if (mode == "GameModeSabotage")
            {
                return GameMode.Sabotage;
            }
        }

        if (isBrawl && isCommanders)
        {
            return GameMode.BrawlCommanders;
        }
        else if (isBrawl && isStandard)
        {
            return GameMode.BrawlStandard;
        }
        else if (isCommanders)
        {
            return GameMode.Commanders;
        }
        else if (isStandard)
        {
            return GameMode.Standard;
        }

        return GameMode.None;
    }
}
