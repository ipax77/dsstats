using dsstats.shared;

namespace dsstats.pickban;

public class PickBanState
{
    public PickBanState(Guid guid, PickBanMode mode, GameMode gameMode, int totalBans, int totalPicks)
    {
        Guid = guid;
        PickBanMode = mode;
        GameMode = gameMode;
        TotalBans = totalBans;
        TotalPicks = totalPicks;
    }
    private object lockobject = new();

    public Guid Guid { get; }
    public PickBanMode PickBanMode { get; }
    public GameMode GameMode { get; }
    public int TotalBans { get; }
    public int TotalPicks { get; }
    public int Visitors { get; set; }
    public List<PickBan> Picks { get; set; } = [];
    public List<PickBan> Bans { get; set; } = [];
    public bool BansPublic => TotalBans == Bans.Count;
    public bool PicksPublic => TotalPicks == Picks.Count;

    public PickBanStateDto? SetPick(PickBan pick)
    {
        lock (lockobject)
        {
            var statePick = Picks.FirstOrDefault(f => f.Slot == pick.Slot);
            if (statePick == null)
            {
                Picks.Add(pick);
                return GetDto();
            }
            return null;
        }
    }

    public PickBanStateDto? SetBan(PickBan ban)
    {
        lock (lockobject)
        {
            var stateBan = Bans.FirstOrDefault(f => f.Slot == ban.Slot);
            if (stateBan == null)
            {
                Bans.Add(ban);
                return GetDto();
            }
            return null;
        }
    }

    public void SetVisitor(bool joined)
    {
        lock (lockobject)
        {
            if (joined)
            {
                Visitors++;
            }
            else
            {
                Visitors--;
            }
        }
    }

    public PickBanStateDto GetDto()
    {
        return new()
        {
            PickBanMode = PickBanMode,
            GameMode = GameMode,
            Visitors = Visitors,
            TotalBans = TotalBans,
            TotalPicks = TotalPicks,
            Bans = !BansPublic ? Bans.Select(s => new PickBan()
            {
                Slot = s.Slot,
                Commander = Commander.None,
                Name = null,
                Locked = s.Locked,
            }).ToList() : this.Bans,
            Picks = !PicksPublic ? Picks.Select(t => new PickBan()
            {
                Slot = t.Slot,
                Commander = Commander.None,
                Name = null,
                Locked = t.Locked,
            }).ToList() : Picks
        };
    }
}
