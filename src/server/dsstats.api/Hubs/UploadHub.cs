using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Hubs;

public class UploadHub : Hub
{
    public Task DecodeRequest(Guid guid)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            guid.ToString()
        );
    }

    public Task Rejoin(Guid guid)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            guid.ToString()
        );
    }
}
