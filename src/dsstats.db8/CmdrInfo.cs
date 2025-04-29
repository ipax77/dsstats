using dsstats.shared;

namespace dsstats.db8;

public sealed class CmdrInfo
{
    public int CmdrInfoId { get; set; }
    public Commander Cmdr { get; set; }
    public Commander Vs { get; set; }
    public string Info { get; set; } = string.Empty;
}

public static class CmdrInfoHelper
{
    public static void SeedCmdrInfos(ReplayContext context)
    {
        var count = context.CmdrInfos.Count();
        if (count > 0)
        {
            return;
        }

        List<CmdrInfo> infos = [
            ..GetAbathurInfos(),
        ];
        context.CmdrInfos.AddRange(infos);
        context.SaveChanges();
    }

    public static List<CmdrInfo> GetAbathurInfos()
    {
        return [
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.None,
                Info = "Abathur is a support-oriented Commander with very low damage output but excellent stacking and tanking capabilities. " +
                    "His main strength is the Viper, which can be game-changing when microed properly. Good Matchups: Horner. Difficult Matchups: Mengsk, Swann."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Abathur,
                Info = "In a mirror matchup, you can either go full air (Mutas into Leviathans/Vipers) or go heavy on Ravagers. " +
                    "If you're comfortable microing Ravagers, it's generally the more reliable option."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Alarak,
                Info = "Alarak is a difficult matchup. Your best option is mass Mutas into Guardians. Be careful once Alarak has his ultimate, as he can one-shot all Mutas. " +
                    "All Biomass should go on Mutas, with one Roach and possibly some on Vipers."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Artanis,
                Info = "Against Artanis, you can go mass Mutas into Guardians. You have a tempo advantage, as Archons are very expensive. " +
                    "Don't waste your global healing — it’s essential for outhealing Archon Storms."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Dehaka,
                Info = "Dehaka is a tough matchup because you don’t have enough damage to kill him quickly, and he can eat your first Biomassed unit. " +
                    "You can try mass Mutas or stack with Swarmhosts. Avoid building Queens, as Dehaka loves eating psionic units."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Fenix,
                Info = "Adepts beat Mutas, but Fenix is also low on damage output, so Swarmhosts supported by Ravagers are a viable option."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Horner,
                Info = "Han & Horner have little to deal with mass Mutas. Their Widow Mines do reduced splash to air and can be distracted with a few Roaches or Ravagers."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Karax,
                Info = "Mutas beat Karax, but it’s a close match. Don’t overextend, or they’ll stack up at the Cannons/Nexus. " +
                    "Be quick to get Vipers and a couple of Leviathans to tank. All Biomass should go on Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Kerrigan,
                Info = "Kerrigan struggles to kill Roaches and can’t handle Guardians in the late game. All Biomass on Roaches, or on Vipers if they go for Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Mengsk,
                Info = "Mengsk is a tough matchup. Your best option is heavy Mutas. Once Thors appear, use Vipers to chain Abduct them and buy time for your Mutas. " +
                    "If they build Emperor’s Shadows, try sniping them with Ravagers. Biomass a few Roaches, then focus Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Nova,
                Info = "Against Nova, Ravagers are an option, but if she builds Ravens and Liberators, you’ll need Mutas anyway — so you might as well hard commit early. " +
                    "All Biomass on Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Raynor,
                Info = "A Muta opener is your best bet. If they commit to ground Bio, add some Ravagers. All Biomass on Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Stetmann,
                Info = "Against Stetmann, start with Roach/Queen into Ravagers. Brutalisks are effective if they focus on Banelings."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Stukov,
                Info = "Open with Roach/Queen into Mutas. Get Leviathans and Devourers early to protect them. Against Apocalisks, add a few Overseers to tank rockets."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Swann,
                Info = "Swann is arguably Abathur’s worst matchup. The only effective counter to Science Vessels is Mutas, but their Radiation ability kills them quickly. " +
                    "Put all Biomass on Mutas and hope for the best."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Tychus,
                Info = "Against Tychus, go Roach/Ravager into Guardians. Be cautious with early Swarmhosts — if your team lacks damage, they can create a massive enemy stack."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Vorazun,
                Info = "Roach/Ravager is the safe choice, but you can open with mass Mutas. It will get rough once Corsairs arrive, " +
                    "but once you have a Leviathan in front and a couple of Vipers, you should win."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Zagara,
                Info = "Go Roach/Ravager into Guardians and Vipers, with some Brutalisks. Don’t forget to disable Abduct on the Vipers. All Biomass on Roaches."
            }
        ];

    }

    public static List<CmdrInfo> GetAlarakInfos()
    {
        return [
            new() {
                Cmdr = Commander.Alarak,
                Vs = Commander.None,
                Info = ""
            }
        ];
    }
}
