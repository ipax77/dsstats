using Microsoft.AspNetCore.SignalR;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;

namespace pax.dsstats.web.Server.Hubs;

public class PickBanHub : Hub
{
    private readonly PickBanService pickBanService;

    public PickBanHub(PickBanService pickBanService)
    {
        this.pickBanService = pickBanService;
    }

    public async Task EnterPage(Guid guid, string mode)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
        var state = pickBanService.CreateOrVisit(guid, mode);
        await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorJoined", state.Visitors);
        await Clients.Client(Context.ConnectionId).SendAsync("ConnectInfo", state);
    }

    public async Task Ban(PickBanEnt ent)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject))
        {
            if (guidObject is Guid guid)
            {
                var state = pickBanService.Ban(guid, ent);
                if (state == null)
                {
                    return;
                }
                if (state.IsBansReady)
                {
                    await Clients.Group(guid.ToString()).SendAsync("ConnectInfo", state);
                }
                else
                {
                    await Clients.OthersInGroup(guid.ToString()).SendAsync("CmdrBaned", ent.Pos);
                }
            }
        }
    }

    public async Task Lock(PickBanEnt ent)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject))
        {
            if (guidObject is Guid guid)
            {
                var state = pickBanService.Lock(guid, ent);
                if (state == null)
                {
                    return;
                }
                if (state.IsPicksReady)
                {
                    await Clients.Group(guid.ToString()).SendAsync("ConnectInfo", state);
                }
                else
                {
                    await Clients.OthersInGroup(guid.ToString()).SendAsync("CmdrLocked", ent.Pos);
                }
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject))
        {
            if (guidObject is Guid guid)
            {
                var visitors = pickBanService.Disconnect(guid);
                await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorLeft", visitors);
            }
        }
        await base.OnDisconnectedAsync(e);
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject))
        {
            if (guidObject is Guid guid)
            {
                var state = pickBanService.Connect(guid);
                if (state == null)
                {
                    return;
                }
                await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorJoined", state.Visitors);
                await Clients.Clients(Context.ConnectionId).SendAsync("ConnectInfo", state);
            }
        }
        await base.OnConnectedAsync();
    }
}
