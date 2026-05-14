using System.Security.Claims;
using dsstats.api.Authentication;
using dsstats.api.InHouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

[Authorize(AuthenticationSchemes = InHouseBearerAuthenticationHandler.SchemeName)]
public sealed class InHouseHub(InHouseConnectionTracker tracker) : Hub
{
    public const string AccountChangedEvent = "account_changed";
    public const string ConnectedPlayersCountEvent = "connected_players_count";
    public const string SessionChangedEvent = "session_changed";
    public const string ReplayAddedEvent = "replay_added";

    public static string GetAccountGroupName(Guid publicUserId)
        => $"inhouse:account:{publicUserId:N}";

    public static string GetSessionGroupName(Guid sessionId)
        => $"inhouse:session:{sessionId:N}";

    public async Task JoinSession(Guid sessionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));

    public async Task LeaveSession(Guid sessionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));

    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetAccountGroupName(userId));
            var count = tracker.Connect(userId, Context.ConnectionId);
            await BroadcastConnectedPlayersCountAsync(count);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var count = tracker.Disconnect(Context.ConnectionId);
        try
        {
            await BroadcastConnectedPlayersCountAsync(count);
        }
        catch (OperationCanceledException)
        {
            // Disconnects and shutdowns can cancel the outgoing hub write after the
            // tracker state has already been updated. The next connection event will
            // publish the latest count.
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastConnectedPlayersCountAsync(int count)
        => Clients.All.SendAsync(ConnectedPlayersCountEvent, count);
}
