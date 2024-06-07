using dsstats.shared;

namespace dsstats.pickban;

public class PickBanRepository
{
    private Dictionary<Guid, PickBanState> pickbanStates = [];

    public PickBanStateDto? GetPickBanState(Guid guid, bool setVisitors)
    {
        if (pickbanStates.TryGetValue(guid, out var pickBanState)
            && pickBanState != null)
        {
            if (setVisitors)
            {
                pickBanState.SetVisitor(true);
            }
            return pickBanState.GetDto();
        }
        return null;
    }

    public PickBanStateDto CreatePickBanState(Guid guid, PickBanMode mode)
    {
        (GameMode gameMode, int totalBans, int totalPicks) = mode switch
        {
            PickBanMode.Standard => (GameMode.Standard, 0, 6),
            PickBanMode.Commanders => (GameMode.Commanders, 2, 6),
            PickBanMode.StdRandom => (GameMode.Standard, 0, 6),
            PickBanMode.Std1v1 => (GameMode.Standard, 0, 2),
            PickBanMode.CmdrBanOnly => (GameMode.Commanders, 2, 0),
            _ => (GameMode.Standard, 0, 6)
        };
        if (!pickbanStates.TryGetValue(guid, out var pickBanState)
            || pickBanState == null)
        {
            pickBanState = pickbanStates[guid] = new(guid, mode, gameMode, totalBans, totalPicks);
            pickBanState.SetVisitor(true);
        }
        return pickBanState.GetDto();
    }

    public int SetVisitor(Guid guid, bool joined)
    {
        if (pickbanStates.TryGetValue(guid, out var state)
            && state != null)
        {
            state.SetVisitor(joined);
            return state.Visitors;
        }
        return 0;
    }

    public PickBanStateDto? SetBan(Guid guid, PickBan pickBan)
    {
        if (pickbanStates.TryGetValue(guid, out var state)
            && state != null)
        {
            return state.SetBan(pickBan);
        }
        return null;
    }

    public PickBanStateDto? SetPick(Guid guid, PickBan pickBan)
    {
        if (pickbanStates.TryGetValue(guid, out var state)
            && state != null)
        {
            return state.SetPick(pickBan);
        }
        return null;
    }
}