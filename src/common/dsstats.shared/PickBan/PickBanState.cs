namespace dsstats.shared.PickBan;

public sealed record PickBanOptions
{
    public GameMode GameMode { get; init; }
    public int Bans { get; init; }
    public int PlayerCount { get; init; }
    public bool UseNames { get; init; }
    public bool UniqueCommanders { get; init; }
}

public sealed record PickBanState
{
    public PickBanOptions Options { get; init; } = new();
    public List<PickBanSlot> BanSlots { get; init; } = [];
    public List<PickBanSlot> PickSlots { get; init; } = [];
    public int UserCount { get; set; }

    public static PickBanState Create(PickBanOptions options)
    {
        PickBanState state = new()
        {
            Options = options,
            UserCount = 1,
        };

        for (int i = 0; i < options.Bans; i++)
        {
            state.BanSlots.Add(new PickBanSlot()
            {
                TeamId = 1,
                SlotId = i,
            });
            state.BanSlots.Add(new PickBanSlot()
            {
                TeamId = 2,
                SlotId = i,
            });
        }

        for (int i = 0; i < options.PlayerCount / 2; i++)
        {
            state.PickSlots.Add(new PickBanSlot()
            {
                TeamId = 1,
                SlotId = i,
            });
            state.PickSlots.Add(new PickBanSlot()
            {
                TeamId = 2,
                SlotId = i,
            });
        }
        return state;
    }

    public PickBanState GetPublicState()
    {
        bool bansReady = BanSlots.All(a => a.Locked);
        bool picksReady = PickSlots.All(a => a.Locked);

        return this with
        {
            BanSlots = MaskOrCopy(BanSlots, bansReady),
            PickSlots = MaskOrCopy(PickSlots, picksReady),
            Options = Options with { }
        };
    }

    private static List<PickBanSlot> MaskOrCopy(IReadOnlyList<PickBanSlot> slots, bool reveal)
    {
        if (reveal)
            return slots.Select(s => s with { }).ToList();

        return slots.Select(s => s with { Commander = Commander.None, Name = null }).ToList();
    }

}

public sealed record PickBanSlot
{
    public int TeamId { get; init; }
    public int SlotId { get; init; }
    public Commander Commander { get; set; }
    public string? Name { get; set; }
    public bool Locked { get; set; }
}

public static class PickBanStateExtensions
{
    public static List<Commander> GetAvailableBanCommanders(this PickBanState state)
    {
        var allCommanders = Enum.GetValues<Commander>().ToHashSet();

        if (state.Options.GameMode == GameMode.Standard)
        {
            allCommanders = allCommanders.Where(x => (int)x <= 3).ToHashSet();
        }
        else
        {
            allCommanders = allCommanders.Where(x => (int)x == 0 || (int)x > 3).ToHashSet();
        }

        HashSet<Commander> bannedCommanders = [];
        foreach (var slot in state.BanSlots.Where(x => x.Locked && x.Commander != Commander.None))
        {
            bannedCommanders.Add(slot.Commander);
        }

        return allCommanders.Except(bannedCommanders).ToList();
    }

    public static List<Commander> GetAvailablePickCommanders(this PickBanState state)
    {
        var allCommanders = Enum.GetValues<Commander>().ToHashSet();

        if (state.Options.GameMode == GameMode.Standard)
        {
            allCommanders = allCommanders.Where(x => (int)x <= 3).ToHashSet();
        }
        else
        {
            allCommanders = allCommanders.Where(x => (int)x == 0 || (int)x > 3).ToHashSet();
        }

        allCommanders = allCommanders.Except(state.BanSlots.Select(s => s.Commander)).ToHashSet();

        if (state.Options.UniqueCommanders)
        {
            HashSet<Commander> pickedCommanders = [];
            foreach (var slot in state.PickSlots.Where(x => x.Locked && x.Commander != Commander.None))
            {
                pickedCommanders.Add(slot.Commander);
            }
            allCommanders = allCommanders.Except(pickedCommanders).ToHashSet();
        }

        return allCommanders.ToList();
    }

    public static bool BansReady(this PickBanState state)
    {
        return state.Options.Bans == 0 || state.BanSlots.All(a => a.Locked);
    }

    public static bool IsReady(this PickBanState state)
    {
        return state.BansReady() && state.PickSlots.All(a => a.Locked);
    }

    public static SlotState GetSlotState(this PickBanSlot slot, int teamId)
    {
        if (!slot.Locked)
        {
            if (teamId > 0 && slot.TeamId != teamId)
            {
                return SlotState.Forbidden;
            }
            return SlotState.None;
        }

        if (slot.Commander == Commander.None && string.IsNullOrEmpty(slot.Name))
        {
            return SlotState.UnknownLocked;
        }

        return SlotState.KnownLocked;
    }
}

public sealed record LockInfo(int TeamId, int SlotId);
public sealed record BanCommand(int TeamId, int SlotId, Commander Commander, string? Name);
public sealed record PickCommand(int TeamId, int SlotId, Commander Commander, string? Name);


public interface IPickBanEvent;
public sealed record BansReadyEvent(PickBanState State) : IPickBanEvent;
public sealed record BanLockedEvent(LockInfo Info) : IPickBanEvent;
public sealed record PickLockedEvent(LockInfo Info) : IPickBanEvent;
public sealed record PickBanReadyEvent(PickBanState State) : IPickBanEvent;


public enum PickBanPhase
{
    Setup,
    Banning,
    Picking,
    Revealed
}

public enum SlotState
{
    None,
    KnownLocked,
    UnknownLocked,
    Forbidden,
}