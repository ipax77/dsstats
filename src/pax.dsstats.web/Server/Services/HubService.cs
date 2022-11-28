using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using System.Collections.Concurrent;

namespace pax.dsstats.web.Server.Services;

public class HubService
{
    private ConcurrentDictionary<Guid, string> AppGuidConnectionIdMap = new();
    private readonly IServiceScopeFactory serviceScopeFactory;

    public HubService(IServiceScopeFactory serviceScopeFactory)
    {
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public event EventHandler<MmrChangedEvent>? MmrChanged;
    protected virtual void OnMmrChanged(MmrChangedEvent e)
    {
        EventHandler<MmrChangedEvent>? handler = MmrChanged;
        handler?.Invoke(this, e);
    }

    public void RegisterApp(Guid appGuid, string connectionId)
    {
        AppGuidConnectionIdMap.AddOrUpdate(appGuid, connectionId, (k, v) => connectionId);
    }

    public void UnRegisterApp(Guid appGuid, string connectionId)
    {
        AppGuidConnectionIdMap.Remove(appGuid, out var _);
    }

    public async Task HandleMmrChanged()
    {
        foreach (var ent in AppGuidConnectionIdMap)
        {
            var appGuid = ent.Key;
            ToonIdsRatingsResponse? response = await GetRatingResponse(appGuid);
            if (response == null)
            {
                continue;
            }
            if (AppGuidConnectionIdMap.TryGetValue(appGuid, out string? connectionId))
            {
                if (!String.IsNullOrEmpty(connectionId))
                {
                    OnMmrChanged(new()
                    {
                        ConnectionId = connectionId,
                        Response = response
                    });
                }
            }
        }
    }

    private async Task<ToonIdsRatingsResponse?> GetRatingResponse(Guid appGuid)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var toonIds = await context.Uploaders
            .Include(i => i.Players)
            .Where(x => x.AppGuid == appGuid)
            .SelectMany(s => s.Players)
            .Select(s => s.ToonId)
            .ToListAsync();

        if (!toonIds.Any())
        {
            return null;
        }

        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        // todo: maybe from continue mmr clalculation only
        return await ratingRepository.GetToonIdsRatings(toonIds);
    }
}

public enum HubGroups
{
    None = 0,
    MmrChange = 1
}

public class MmrChangedEvent : EventArgs
{
    public string ConnectionId { get; set; } = null!;
    public ToonIdsRatingsResponse Response { get; init; } = null!;
}