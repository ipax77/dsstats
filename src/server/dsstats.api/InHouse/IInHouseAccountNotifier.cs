using dsstats.api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.InHouse;

public interface IInHouseAccountNotifier
{
    Task NotifyAccountChangedAsync(Guid publicUserId, string reason);
}

public sealed class InHouseAccountNotifier(IHubContext<InHouseHub> hubContext) : IInHouseAccountNotifier
{
    public Task NotifyAccountChangedAsync(Guid publicUserId, string reason)
        => hubContext.Clients
            .Group(InHouseHub.GetAccountGroupName(publicUserId))
            .SendAsync(InHouseHub.AccountChangedEvent, reason);
}
