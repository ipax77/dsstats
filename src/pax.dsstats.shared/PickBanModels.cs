using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.shared;

public record PickBanEnt
{
    public int Pos { get; set; }
    public bool IsLocked { get; set; }
    public int Commander { get; set; }
    public string? CommanderString { get; set; }
    public int Team { get; set; }
    public int Order { get; set; }
    [MaxLength(30)]
    public string? PlayerName { get; set; }
}

public record PickBanState
{
    public PickBanState() { }

    public PickBanState(int teams = 2, int bansPerTeam = 1, int entsPerTeam = 3)
    {
        for (int i = 0; i < bansPerTeam * teams; i++)
        {
            Bans.Add(new PickBanEnt { Pos = i, Team = i % teams, });
        }

        if (!Bans.Any())
        {
            IsBansReady = true;
        }

        for (int i = 0; i < entsPerTeam * teams; i++)
        {
            Picks.Add(new PickBanEnt { Pos = i, Team = i % teams, });
        }
    }
    public int Visitors { get; set; }
    public int Turn => Picks.Where(x => x.IsLocked).Count();
    public bool IsBansReady { get; set; }
    public bool IsPicksReady { get; set; }

    public ICollection<PickBanEnt> Bans { get; set; } = new List<PickBanEnt>();
    public ICollection<PickBanEnt> Picks { get; set; } = new List<PickBanEnt>();

    public HashSet<Commander> GetOpenCommanders(int team)
    {
        var commanders = Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => (int)x >= 10).ToHashSet();

        var bans = Bans
            .Where(x => x.IsLocked && x.Commander > 0)
            .Select(s => s.Commander)
            .Cast<Commander>();
        commanders.ExceptWith(bans);

        var picked = Picks
            .Where(x => x.IsLocked && x.Team == team && x.Commander > 0)
            .Select(s => s.Commander)
            .Cast<Commander>();
        commanders.ExceptWith(picked);

        return commanders;
    }

    public HashSet<Commander> GetGdslFunCommanders()
    {
        return new() { Commander.Protoss, Commander.Zerg };
    }

    public HashSet<Commander> GetStdCommanders()
    {
        return new() { Commander.Protoss, Commander.Terran, Commander.Zerg };
    }
}
