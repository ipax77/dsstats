using dsstats.db8;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.pickban;

public class PickBanRepository(IServiceScopeFactory scopeFactory)
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
        (GameMode gameMode, int totalBans, int totalPicks) = Data.GetPickBanModeSettings(mode);

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

    public async Task SavePickBan(PickBanStateDto pickBan)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        DsPickBan dsPickBan = new()
        {
            PickBanMode = pickBan.PickBanMode,
            Time = DateTime.UtcNow,
            Bans = pickBan.Bans.Select(s => s.Commander).ToList(),
            Picks = pickBan.Picks.Select(s => s.Commander).ToList()
        };
        context.DsPickBans.Add(dsPickBan);
        await context.SaveChangesAsync();
    }
}