using Microsoft.AspNetCore.SignalR;

namespace pax.dsstats.web.Server.Hubs;

public class MauiHub : Hub
{
    public async Task Subscribe(Guid guid)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
        await Clients.Group(guid.ToString()).SendAsync("CurrentMmr", 7777);
    }
}

