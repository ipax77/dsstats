using dsstats.shared;

namespace dsstats.parser;

internal sealed class DsstatsReplay
{
    public int Duration { get; set; }
    public Dictionary<Breakpoint, MiddleIncome> MiddleIncome { get; set; } = [];
    public List<DsMiddle> MiddleChanges { get; set; } = [];
}

internal sealed class DsPlayer
{
    public ToonId ToonId { get; set; } = new();
    public int TeamId { get; set; }
    public List<Refinery> Refineries { get; set; } = [];
}

internal sealed class ToonId
{
    public int Region { get; set; }
    public int Realm { get; set; }
    public int Id { get; set; }
}

internal sealed class Refinery
{
    public int Gameloop { get; set; }
    public bool Taken { get; set; }
}

internal sealed class DsMiddle
{
    public int Gameloop { get; set; }
    public int ControlTeam { get; set; }
}

internal sealed class MiddleIncome
{
    public int Team1 { get; set; }
    public int Team2 { get; set; }
}

internal static class DsstatsReplayMapper
{
    internal static int GetIncome(DsstatsReplay replay, DsPlayer player, Breakpoint bp)
    {
        double baseIncome = 7.5;
        double baseGasIncome = 0.5;
        int[] refineryCosts = [150, 225, 300, 375, 500];

        int gameloop = bp switch
        {
            Breakpoint.Min5 => DsstatsParser.min5,
            Breakpoint.Min10 => DsstatsParser.min10,
            Breakpoint.Min15 => DsstatsParser.min15,
            _ => replay.Duration
        };

        double gasIncome = 0;
        double middleIncome = 0;
        double income = gameloop / 22.4 * baseIncome;

        int refineryIndex = 0;
        foreach (Refinery refinery in player.Refineries)
        {
            if (refinery.Taken && refinery.Gameloop < gameloop)
            {
                gasIncome += (gameloop - refinery.Gameloop) / 22.4 * baseGasIncome;
                gasIncome -= refineryCosts[refineryIndex];
                refineryIndex++;
            }
        }

        if (replay.MiddleIncome.TryGetValue(bp, out MiddleIncome? teamMiddleIncome))
        {
            middleIncome = player.TeamId == 1 ? teamMiddleIncome.Team1 : teamMiddleIncome.Team2;
        }

        return (int)(gasIncome + middleIncome + income);
    }
}
