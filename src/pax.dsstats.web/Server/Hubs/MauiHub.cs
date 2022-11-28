using Microsoft.AspNetCore.SignalR;
using pax.dsstats.web.Server.Services;

namespace pax.dsstats.web.Server.Hubs;

public class MauiHub : Hub
{
    private readonly HubService hubService;

    public MauiHub(HubService hubService)
    {
        this.hubService = hubService;
        hubService.MmrChanged += NotifyMmrChange;
    }

    public async Task Subscribe(Guid guid)
    {
        Context.Items.Clear();
        Context.Items.Add("guid", guid);
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.MmrChange.ToString());
        hubService.RegisterApp(guid, Context.ConnectionId);
    }

    public async Task UnSubscribe(Guid guid)
    {
        Context.Items.Clear();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.MmrChange.ToString());
        hubService.UnRegisterApp(guid, Context.ConnectionId);
    }

    public void DEBUGMmrChange(Guid guid)
    {
        NotifyMmrChange(null, new MmrChangedEvent()
        {
            ConnectionId = Context.ConnectionId,
            Response = new shared.Raven.ToonIdsRatingsResponse()
            {
                RatingInfos = new()
                {
                    new shared.Raven.ToonIdRatingInfo()
                    {
                        ToonId = 226401,
                        Name = "PAX",
                        Ratings = new()
                        {
                            new shared.Raven.ToonIdRating()
                            {
                                RatingType = shared.Raven.RatingType.Cmdr,
                                Mmr = 2000.0,
                                RegionId = 2
                            }
                        }
                    }
                }
            }
        });
    }

    private async void NotifyMmrChange(object? sender, MmrChangedEvent e)
    {
        await Clients.Client(e.ConnectionId).SendAsync("mmrchanged", e.Response);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("guid", out var guid))
        {
            if (guid is Guid appGuid)
            {
                hubService.UnRegisterApp(appGuid, Context.ConnectionId);
            }
        }
        return base.OnDisconnectedAsync(exception);
    }
}

