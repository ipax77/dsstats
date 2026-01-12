using dsstats.shared.PickBan;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace dsstats.api.Services;

public interface IPickBanService
{
    PickBanState? Create(Guid id, PickBanOptions options);
    PickBanState? Join(Guid id);
    PickBanState? Disconnect(Guid id);
    IPickBanEvent? ApplyBan(Guid id, BanCommand cmd, string userId);
    IPickBanEvent? ApplyPick(Guid id, PickCommand cmd, string userId);
    Task HandleEventAsync(Guid id, IPickBanEvent? evt);
}

public sealed class PickBanService(IHubContext<PickBanHub> hubContext) : IPickBanService
{
    private readonly ConcurrentDictionary<Guid, PickBanState> _sessions = new();
    private readonly Lock pickLock = new();

    public PickBanState? Create(Guid id, PickBanOptions options)
    {
        var state = PickBanState.Create(options);
        if (_sessions.TryAdd(id, state))
        {
            return state;
        }
        return null;
    }

    public PickBanState? Join(Guid id)
    {
        if (!_sessions.TryGetValue(id, out var state))
            return null;
        state.UserCount++;
        return state.GetPublicState();
    }

    public PickBanState? Disconnect(Guid id)
    {
        if (!_sessions.TryGetValue(id, out var state))
            return null;
        state.UserCount--;
        return state.GetPublicState();
    }

    public IPickBanEvent? ApplyBan(Guid id, BanCommand cmd, string userId)
    {
        lock (pickLock)
        {
            if (!_sessions.TryGetValue(id, out var state))
                return null;

            // VALIDATION AREA
            var slot = state.BanSlots.FirstOrDefault(s => s.TeamId == cmd.TeamId && s.SlotId == cmd.SlotId);
            if (slot is null || slot.Locked)
                return null;

            // APPLY MUTATION
            slot.Commander = cmd.Commander;
            slot.Name = cmd.Name;
            slot.Locked = true;

            // CHECK STATE
            var bansReady = state.BanSlots.All(s => s.Locked);

            if (bansReady)
            {
                return new BansReadyEvent(state.GetPublicState());
            }
            else
            {
                return new BanLockedEvent(new(slot.TeamId, slot.SlotId));
            }
        }
    }

    public IPickBanEvent? ApplyPick(Guid id, PickCommand cmd, string userId)
    {
        lock (pickLock)
        {
            if (!_sessions.TryGetValue(id, out var state))
                return null;

            // VALIDATION AREA
            var slot = state.PickSlots.FirstOrDefault(s => s.TeamId == cmd.TeamId && s.SlotId == cmd.SlotId);
            if (slot is null || slot.Locked)
                return null;

            // APPLY MUTATION
            slot.Commander = cmd.Commander;
            slot.Name = cmd.Name;
            slot.Locked = true;

            // CHECK STATE
            var ready = state.BanSlots.All(a => a.Locked) && state.PickSlots.All(s => s.Locked);

            if (ready)
            {
                return new PickBanReadyEvent(state.GetPublicState());
            }
            else
            {
                return new PickLockedEvent(new(slot.TeamId, slot.SlotId));
            }
        }
    }

    public async Task HandleEventAsync(Guid id, IPickBanEvent? evt)
    {
        if (evt is null)
            return;

        switch (evt)
        {
            case BanLockedEvent e:
                await hubContext.Clients.Group(id.ToString()).SendAsync("ban_locked", e.Info);
                break;

            case PickLockedEvent e:
                await hubContext.Clients.Group(id.ToString()).SendAsync("pick_locked", e.Info);
                break;

            case BansReadyEvent e:
                await hubContext.Clients.Group(id.ToString()).SendAsync("bans_ready", e.State);
                break;

            case PickBanReadyEvent e:
                await hubContext.Clients.Group(id.ToString()).SendAsync("ready", e.State);
                break;
        }
    }

}
