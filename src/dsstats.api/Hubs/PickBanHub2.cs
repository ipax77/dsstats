using dsstats.shared;
using Microsoft.AspNetCore.SignalR;

namespace pax.dsstats.web.Server.Hubs;

public class PickBanHub2 : Hub
{
    public async Task GetOrCreateGroup(Guid guid, PickBanMode mode)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());

        var state = PickBan2Service.GetOrCreateGroup(guid, mode);
        await Clients.Client(Context.ConnectionId).SendAsync("State", state);
        await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", state.Visitors);
    }

    public async Task Ban(PickBan ban)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && guidObject is Guid guid)
        {

        }
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && guidObject is Guid guid)
        {
            int visitors = PickBan2Service.LeaveGroup(guid);
            await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", visitors);
        }
        await base.OnDisconnectedAsync(e);
    }

}

public static class PickBan2Service
{
    private static Dictionary<Guid, PickBan2State> states = [];

    public static PickBan2State GetOrCreateGroup(Guid guid, PickBanMode mode)
    {
        if (!states.TryGetValue(guid, out var state)
            || state == null)
        {
            state = states[guid] = mode switch
            {
                PickBanMode.Std1v1 => new PickBan2State(guid, GameMode.Standard, 0, 6),
                PickBanMode.CmdrBanOnly => new PickBan2State(guid, GameMode.Commanders, 2, 0),
                _ => new PickBan2State(guid, GameMode.Standard, 0, 6)
            };
        }
        state.SetVisitor(true);
        return state;
    }

    public static int LeaveGroup(Guid guid)
    {
        if (!states.TryGetValue(guid, out var state)
            || state == null)
        {
            return 0;
        }
        state.SetVisitor(false);
        return state.Visitors;
    }
}

public class PickBan2State
{
    public PickBan2State(Guid guid, GameMode gameMode, int totalBans, int totalPicks)
    {
        Guid = guid;
        GameMode = gameMode;
        TotalBans = totalBans;
        TotalPicks = totalPicks;
    }
    private object lockobject = new();

    public Guid Guid { get; }
    public GameMode GameMode { get; }
    public int TotalBans { get; }
    public int TotalPicks { get; }
    public int Visitors { get; set; }
    public int BansCount { get; set; }
    public int PicksCount { get; set; }
    public List<PickBan> Picks { get; set; } = [];
    public List<PickBan> Bans { get; set; } = [];
    
    public void SetState(PickBan2StateDto state)
    {
        this.Visitors = state.Visitors;
        this.BansCount = state.BansCount;
        this.PicksCount = state.PicksCount;
        this.Picks = state.Picks;
        this.Bans = state.Bans;
    }
    
    public void SetPick(PickBan pick)
    {
        lock (lockobject)
        {
            var statePick = Picks.FirstOrDefault(f => f.Slot == pick.Slot);
            if (statePick == null)
            {
                Picks.Add(pick);
            }
            else
            {
                statePick.Commander = pick.Commander;
                statePick.Locked = pick.Locked;
            }
        }
    }

    public void SetBan(PickBan ban)
    {
        lock (lockobject)
        {
            var stateBan = Bans.FirstOrDefault(f => f.Slot == ban.Slot);
            if (stateBan == null)
            {
                Bans.Add(ban);
            }
            else
            {
                stateBan.Commander = ban.Commander;
                stateBan.Locked = ban.Locked;
            }
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

    public PickBan2StateDto GetDto()
    {
        return new()
        {
            Visitors = Visitors,
            BansCount = Bans.Count,
            PicksCount = Picks.Count,
            Bans = BansCount < Bans.Count ? Bans.Select(s => new PickBan()
            {
                Slot = s.Slot,
                Commander = Commander.None,
                Locked = s.Locked,
            }).ToList() : this.Bans,
            Picks = PicksCount < Picks.Count ? Picks.Select(t => new PickBan()
            {
                Slot = t.Slot,
                Commander = Commander.None,
                Locked = t.Locked,
            }).ToList() : Picks
        };
    }
}

public record PickBan2StateDto
{
    public int Visitors { get; set; }
    public int BansCount { get; set; }
    public int PicksCount { get; set; }
    public List<PickBan> Picks { get; set; } = [];
    public List<PickBan> Bans { get; set; } = [];
}

public record PickBan
{
    public int Slot { get; set; }
    public Commander Commander { get; set; }
    public bool Locked { get; set; }
}

