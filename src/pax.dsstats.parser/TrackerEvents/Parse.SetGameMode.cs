using pax.dsstats.shared;

namespace pax.dsstats.parser;
public partial class Parse
{
    private static void SetGameModeNg(DsReplay replay)
    {
        var gameModes = replay.Mutations.Where(x => x.StartsWith("GameMode")).ToList();
        var mutations = replay.Mutations.Where(x => x.StartsWith("Mutation")).ToList();

        if (gameModes.Count == 1)
        {
            replay.GameMode = gameModes.First();
            return;
        }
        else if (gameModes.Any())
        {
            if (replay.Mutations.Contains("GameModeBrawl"))
            {
                if (replay.Mutations.Contains("GameModeCommanders"))
                {
                    replay.GameMode = "GameModeBrawlCommanders";
                    return;
                }
                else if (replay.Mutations.Contains("GameModeStandard"))
                {
                    replay.GameMode = "GameModeBrawlStandard";
                    return;
                }
            }
            else if (replay.GameMode.Contains("GameModeHeroicCommanders"))
            {
                replay.GameMode = "GameModeHeroicCommanders";
                return;
            }
            else
            {
                replay.GameMode = gameModes.First();
                return;
            }
        }
        else // time before GameMode existed
        {
            if (mutations.Contains("MutationSuperscan"))
            {
                mutations.Remove("MutationSuperscan"); // 1v1 only?
            }

            if (mutations.Contains("MutationCommanders"))
            {
                if (mutations.Count == 3
                    && mutations.Contains("MutationExpansion")
                    && mutations.Contains("MutationOvertime"))
                {
                    replay.GameMode = "GameModeHeroicCommanders";
                }
                else if (mutations.Count == 2
                    && mutations.Contains("MutationOvertime"))
                {
                    replay.GameMode = "GameModeCommanders";
                }
                else if (mutations.Count >= 3)
                {
                    replay.GameMode = "GameModeBrawlCommanders";
                }
                else if (mutations.Contains("MutationAura"))
                {
                    replay.GameMode = "GameModeBrawlCommanders";
                }
                else
                {
                    replay.GameMode = "GameModeCommanders";
                }
            }
            else
            {
                if (mutations.Count == 0)
                {
                    replay.GameMode = "GameModeStandard";
                }
                else
                {
                    replay.GameMode = "GameModeBrawlStandard";
                }
            }
        }
    }
}
