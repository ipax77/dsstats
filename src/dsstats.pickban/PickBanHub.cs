using dsstats.shared;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.pickban;

public class PickBanHub(PickBanRepository pickBanRepository) : Hub
{
    public async Task CreatePickBan(Guid guid, PickBanMode mode)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());

        var stateDto = pickBanRepository.CreatePickBanState(guid, mode);
        await Clients.Client(Context.ConnectionId).SendAsync("State", stateDto);
        await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", stateDto.Visitors);
    }

    public async Task JoinPickBan(Guid guid)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());

        var stateDto = pickBanRepository.GetPickBanState(guid, true);
        if (stateDto == null)
        {
            return;
        }
        await Clients.Client(Context.ConnectionId).SendAsync("State", stateDto);
        await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", stateDto.Visitors);
    }

    public async Task LeavePickBan()
    {
        if (Context.Items.TryGetValue("guid", out var guidObj)
            && guidObj is Guid guid)
        {
            int visitors = pickBanRepository.SetVisitor(guid, false);
            await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", visitors);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
        }
        Context.Items.Clear();
    }

    public async Task SetBan(PickBan pickBan)
    {
        if (Context.Items.TryGetValue("guid", out var guidObj)
            && guidObj is Guid guid)
        {
            var resultDto = pickBanRepository.SetBan(guid, pickBan);
            if (resultDto == null)
            {
                return;
            }
            await Clients.Group(guid.ToString()).SendAsync("Bans", resultDto.Bans);

        }
    }

    public async Task SetPick(PickBan pickBan)
    {
        if (Context.Items.TryGetValue("guid", out var guidObj)
            && guidObj is Guid guid)
        {
            var resultDto = pickBanRepository.SetPick(guid, pickBan);
            if (resultDto == null)
            {
                return;
            }
            await Clients.Group(guid.ToString()).SendAsync("Picks", resultDto.Picks);
        }
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.Items.TryGetValue("guid", out var guidObj)
            && guidObj is Guid guid)
        {
            await JoinPickBan(guid);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        if (Context.Items.TryGetValue("guid", out var guidObj)
            && guidObj is Guid guid)
        {
            int visitors = pickBanRepository.SetVisitor(guid, false);
            await Clients.OthersInGroup(guid.ToString()).SendAsync("Visitors", visitors);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
        }
        await base.OnDisconnectedAsync(e);
    }
}
