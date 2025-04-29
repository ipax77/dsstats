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
                Info = "Abathur is a support kind of Commander with very low damage output but very good stacking/tanking capabilities. " +
                    "Main strength is the Viper which can be game changing when microed properly. Good Matchups: Horner, Difficult Matchups: Mengsk, Swann"
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Abathur,
                Info = "In a mirror matchup you have two options to either go full air (Mutas into Leviathan/Vipers) or heavy Ravagers. " + 
                    "If you feel comfortable in microing Ravagers it is the more solid approach."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Alarak,
                Info = "Alarak is a difficult matchup, but your best bet are mass Mutas into Guardians. Be careful once Alarak has his R up, then he can oneshot all Mutas. " +
                    "All Biomass should go on Mutas with one one Roach and maybe some on Vipers."                                    
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Artanis,
                Info = "Vs Artanis you can go mass Mutas into Guardians. You have a tempo advantage because Archons are very expensive. " +
                    "Be careful to not waste you global healing, you will need it to outheal the Archons Storm."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Dehaka,
                Info = "Dehaka is a difficult matchup, because you don't have enough damage output to kill Dehaka in time and they can eat your first Biomassed unit." +
                    "You can try mass Mutas or stack with Swarmhosts. Don't build Queens vs Dehaka (he loves eating psionic units)"
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Fenix,
                Info = "Adepts win vs Mutas, but Fenix is also low on damage output, so Swarmhosts with Ravager support is an option. "
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Horner,
                Info = "H&H has nothing to deal with mass Mutas (Mines do less splash to Air and can be distracted by a couple Roaches/Ravagers). "
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Karax,
                Info = "Mutas win vs Karax, but it is very close. Don't overpush, so they cannot stack at the Cannon/Nexus and be fast on the Vipers and a couple Leviathans to tank. All Biomass on Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Kerrigan,
                Info = "Kerrigan has troubles to kill Roaches and cannot deal with Guardians late game. All Biomass on Roaches (or Vipers if they try Mutas)."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Mengsk,
                Info = "Mengsk is a difficult matchup, your best bet is heavy Mutas. Once they have Thors out you can chain abduct them with Vipers to give the Mutas more time. "
                    + "If they go Emperor's Shadow you can try to snipe them with Ravagers. Biomass some Roaches then Mutas only."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Nova,
                Info = "Vs Nova you can go Ravagers but vs Ravens and Liberators you need Mutas anyhow, so you can hard commit from the get-go. All Biomass on Mutas."
                    + ""
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Raynor,
                Info = "Muta opener is your best bet. If they hard commit to groud Bio you can add some Ravagers. All Biomass on Mutas."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Stetmann,
                Info = "Vs Stetmann you can open Roach Queen into Ravagers. Brutalisks are very good if they commit to Banelings."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Stukov,
                Info = "Vs Stukov you can open Roach Queen into Mutas. Be fast with Leviathan/Devourer to protect them. Vs Apocalisks you can add a few Overseer to tank the Rockets."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Swann,
                Info = "Swann is probably the worst matchup for Abathur. The only thing that can kill the Science Vessels are Mutas, but their Radiation kills them." +
                    "All Biomass on Mutas and hope the best."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Tychus,
                Info = "Vs Tychus Roach Ravager into Guardians should be your best option. Be careful with early Swarmhosts if you don't have enough damage output in the team, " +
                    "because they can produce a monster stack for the enemy team."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Vorazun,
                Info = "Roach Ravager is save call, but you can open mass Mutas vs Vorazun. It will be painful when they have enough Corsairs out, " +
                    "but once you have a Leviathan in front and a couple Vipers you win."
            },
            new() {
                Cmdr = Commander.Abathur,
                Vs = Commander.Zagara,
                Info = "Roach Ravager into Guardians Vipers with some Brutalsiks. Don't forget to turn off Abduct on the Vipers. All Biomass on Roaches."
            }
        ];
    }
}
