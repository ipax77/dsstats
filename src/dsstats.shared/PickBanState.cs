using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dsstats.shared;
public record PickBanState
{
    public PickBanState(PickBanMode pickBanMode)
    {
        PickBanMode = pickBanMode;

        int teams = 2;
        int entsPerTeam = 3;
        int bansPerTeam = pickBanMode == PickBanMode.Standard
            || pickBanMode == PickBanMode.StdRandom ? 0 : 1;

        for (int i = 0; i < bansPerTeam * teams; i++)
        {
            Bans.Add(new PickBanEnt { Pos = i, Team = i % teams, });
        }

        for (int i = 0; i < entsPerTeam * teams; i++)
        {
            Picks.Add(new PickBanEnt { Pos = i, Team = i % teams, });
        }
    }

    [JsonConstructor]
    public PickBanState() { }

    public PickBanMode PickBanMode { get; set; }
    public int Visitors { get; set; }
    public int Turn => Picks.Where(x => x.IsLocked).Count();
    public bool IsBansReady => Bans.All(a => a.IsLocked);
    public bool IsPicksReady => Picks.All(a => a.IsLocked);

    public ICollection<PickBanEnt> Bans { get; set; } = new List<PickBanEnt>();
    public ICollection<PickBanEnt> Picks { get; set; } = new List<PickBanEnt>();

    public HashSet<Commander> GetOpenCommanders(int team)
    {
        var commanders = Data.GetCommanders(PickBanMode == PickBanMode.Standard 
            || PickBanMode == PickBanMode.StdRandom
            ? Data.CmdrGet.Std : Data.CmdrGet.NoStd).ToHashSet();

        if (PickBanMode == PickBanMode.StdRandom)
        {
            commanders.Add(Commander.None);
        }

        var bans = Bans
            .Where(x => x.IsLocked && x.Commander > 0)
            .Select(s => s.Commander);
        commanders.ExceptWith(bans);

        var picked = PickBanMode == PickBanMode.Commanders
            ? Picks
                .Where(x => x.IsLocked && x.Team == team && x.Commander > 0)
                .Select(s => s.Commander)
            : new List<Commander>();
        commanders.ExceptWith(picked);

        return commanders;
    }
}

public record PickBanEnt
{
    public int Pos { get; set; }
    public int Team { get; set; }
    public Commander Commander { get; set; }
    public bool IsLocked { get; set; }
    public bool Random {  get; set; }
    public int Order { get; set; }
    [MaxLength(30)]
    public string? PlayerName { get; set; }
}

public record PickBanSetting
{
    public PickBanMode PickBanMode { get; set; }
    public Guid Id { get; set; }
}

public enum PickBanMode
{
    None = 0,
    Standard = 1,
    Commanders = 2,
    Name = 3,
    StdRandom = 4
}