using dsstats.api.Services;
using dsstats.shared.PickBan;
using Microsoft.AspNetCore.SignalR;

public class PickBanHub(IPickBanService pickBanService) : Hub
{
    public async Task Create(Guid id, PickBanOptions options)
    {
        var state = pickBanService.Create(id, options);
        if (state is null)
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());

        var publicState = state.GetPublicState();
        await Clients.Group(id.ToString()).SendAsync("state_created", publicState);
    }

    public async Task Join(Guid id)
    {
        var state = pickBanService.Join(id);
        if (state is null)
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        await Clients.OthersInGroup(id.ToString()).SendAsync("user_joined");
        await Clients.Caller.SendAsync("state_synced", state);
    }

    public async Task Ban(Guid id, BanCommand cmd)
    {
        var result = pickBanService.ApplyBan(id, cmd, Context.ConnectionId);
        await pickBanService.HandleEventAsync(id, result);
    }

    public async Task Pick(Guid id, PickCommand cmd)
    {
        var result = pickBanService.ApplyPick(id, cmd, Context.ConnectionId);
        await pickBanService.HandleEventAsync(id, result);
    }

    public async Task Rejoin(Guid id)
    {
        var state = pickBanService.Join(id);
        if (state is null)
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString());
        await Clients.Caller.SendAsync("state_synced", state);
    }
}
