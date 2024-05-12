using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace dsstats.api.Hubs;

public class IhHub(IIhService ihService) : Hub
{

    public async Task JoinGroup(Guid groupId)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            await LeaveGroup();
        }

        Context.Items.Add("guid", groupId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        var groupState = await ihService.CreateOrVisitGroup(groupId);
        if (groupState != null)
        {
            await Clients.OthersInGroup(groupId.ToString()).SendAsync("VisitorJoined", groupState.Visitors);
            await Clients.Client(Context.ConnectionId).SendAsync("ConnectInfo", groupState);

            var replayList = await ihService.GetReplays(groupId);
            if (replayList.Count > 0)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Replays", replayList);
            }
        }
    }

    public async Task LeaveGroup()
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
            var groupState = ihService.LeaveGroup(guid);
            if (groupState is not null)
            {
                await Clients.Group(guid.ToString()).SendAsync("VisitorLeft", groupState.Visitors);
            }
        }
        Context.Items.Clear();
    }

    public async Task DecodeRequest()
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            await Clients.OthersInGroup(guid.ToString()).SendAsync("DecodingStart");

            var groupState = await ihService.GetDecodeResultAsync(guid);

            if (groupState is null)
            {
                await Clients.Group(guid.ToString()).SendAsync("DecodeError");
            }
            else
            {
                await Clients.Group(guid.ToString()).SendAsync("ConnectInfo", groupState);

                var replayList = await ihService.GetReplays(guid);
                if (replayList.Count > 0)
                {
                    await Clients.Group(guid.ToString()).SendAsync("Replays", replayList);
                }
            }
        }
    }

    public async Task AddPlayerToGroup(RequestNames requestNames)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            var playerState = await ihService.AddPlayerToGroup(guid, requestNames);
            if (playerState is not null)
            {
                await Clients.Group(guid.ToString()).SendAsync("NewPlayer", playerState);
            }
        }
    }

    public async Task AddPlayerToQueue(PlayerId playerId)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            if (await ihService.AddPlayerToQueue(guid, playerId))
            {
                await Clients.Group(guid.ToString()).SendAsync("AddedToQueue", playerId);
            }
        }
    }

    public async Task RemovePlayerFromQueue(PlayerId playerId)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            if (await ihService.AddPlayerToQueue(guid, playerId))
            {
                await Clients.Group(guid.ToString()).SendAsync("RemovedFromQueue", playerId);
            }
        }
    }

    public async Task RemovePlayerFromGroup(RequestNames requestNames)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            var playerState = await ihService.RemovePlayerFromGroup(guid, requestNames);
            if (playerState is not null)
            {
                await Clients.Group(guid.ToString()).SendAsync("RemovePlayer", playerState);
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        if (Context.Items.TryGetValue("guid", out object? guidObject)
            && Guid.TryParse(guidObject?.ToString(), out Guid guid))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
            var groupState = ihService.LeaveGroup(guid);
            if (groupState is not null)
            {
                await Clients.Group(guid.ToString()).SendAsync("VisitorLeft", groupState.Visitors);
            }
        }
        await base.OnDisconnectedAsync(e);
    }
}
