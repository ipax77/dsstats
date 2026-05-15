using System.Security.Claims;
using dsstats.api.Authentication;
using dsstats.api.InHouse;
using dsstats.dbServices.InHouse;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

[Authorize(AuthenticationSchemes = InHouseBearerAuthenticationHandler.SchemeName)]
public sealed class InHouseHub(
    InHouseConnectionTracker tracker,
    IInHouseGameSessionService sessionService) : Hub
{
    public const string AccountChangedEvent = "account_changed";
    public const string ConnectedPlayersCountEvent = "connected_players_count";
    public const string ActiveSessionsChangedEvent = "active_sessions_changed";
    public const string SessionStateEvent = "session_state";
    public const string SitterChangedEvent = "sitter_changed";

    public static string GetAccountGroupName(Guid publicUserId)
        => $"inhouse:account:{publicUserId:N}";

    public static string GetSessionGroupName(Guid sessionId)
        => $"inhouse:session:{sessionId:N}";

    public async Task<List<InHouseGameSessionListDto>> GetActiveSessions()
        => await sessionService.GetActiveSessionsAsync(Context.ConnectionAborted);

    public async Task<InHouseGameSessionDetailDto?> JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
        return await sessionService.GetSessionAsync(sessionId, GetUserId(), Context.ConnectionAborted);
    }

    public async Task LeaveSession(Guid sessionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));

    public async Task<InHouseGameSessionDetailDto> AddOrUpdateRosterPlayer(
        Guid sessionId,
        InHouseRosterPlayerUpsertRequest request)
    {
        var detail = await sessionService.AddRosterPlayerAsync(sessionId, GetUserId(), request, Context.ConnectionAborted);
        await BroadcastSessionStateAsync(detail);
        return detail;
    }

    public async Task<InHouseGameSessionDetailDto> RemoveRosterPlayer(Guid sessionId, Guid rosterPlayerId)
    {
        var detail = await sessionService.RemoveRosterPlayerAsync(sessionId, rosterPlayerId, GetUserId(), Context.ConnectionAborted);
        await BroadcastSessionStateAsync(detail);
        return detail;
    }

    public async Task<InHouseSitterChangedDto> SetRosterPlayerSitter(Guid sessionId, Guid rosterPlayerId, bool isSitter)
    {
        var detail = await sessionService.SetRosterPlayerSitterAsync(sessionId, rosterPlayerId, GetUserId(), isSitter, Context.ConnectionAborted);
        var changed = new InHouseSitterChangedDto
        {
            SessionId = sessionId,
            Revision = detail.Revision,
            RosterPlayerId = rosterPlayerId,
            IsSitter = isSitter,
        };
        await Clients.Group(GetSessionGroupName(sessionId)).SendAsync(SitterChangedEvent, changed, Context.ConnectionAborted);
        return changed;
    }

    public async Task<InHouseGameSessionDetailDto> CloseSession(Guid sessionId)
    {
        var detail = await sessionService.CloseSessionAsync(sessionId, GetUserId(), Context.ConnectionAborted);
        await BroadcastSessionStateAsync(detail);
        await Clients.All.SendAsync(ActiveSessionsChangedEvent, Context.ConnectionAborted);
        return detail;
    }

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

    private int GetUserId()
        => int.TryParse(Context.User?.FindFirstValue(InHouseClaims.UserId), out var userId)
            ? userId
            : throw new InvalidOperationException("You are not signed in.");

    private async Task BroadcastSessionStateAsync(InHouseGameSessionDetailDto detail)
    {
        await Clients.Group(GetSessionGroupName(detail.SessionId))
            .SendAsync(SessionStateEvent, detail, Context.ConnectionAborted);
    }
}
