using dsstats.api.Services;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

public class IhHub(IhService ihService) : Hub
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
        var groupState = ihService.CreateOrVisitGroup(groupId);
        if (groupState != null)
        {
            await Clients.OthersInGroup(groupId.ToString()).SendAsync("VisitorJoined", groupState.Visitors);
            await Clients.Client(Context.ConnectionId).SendAsync("ConnectInfo", groupState);
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
                await Clients.Group(guid.ToString()).SendAsync("NewState", groupState);
            }
        }
    }
}
