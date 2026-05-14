using System.Security.Claims;
using dsstats.api.Authentication;
using dsstats.api.InHouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

[Authorize(AuthenticationSchemes = InHouseBearerAuthenticationHandler.SchemeName)]
public sealed class InHouseHub(InHouseConnectionTracker tracker) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var count = tracker.Connect(userId, Context.ConnectionId);
            await Clients.All.SendAsync("connected_players_count", count, Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var count = tracker.Disconnect(Context.ConnectionId);
        await Clients.All.SendAsync("connected_players_count", count, Context.ConnectionAborted);
        await base.OnDisconnectedAsync(exception);
    }
}
