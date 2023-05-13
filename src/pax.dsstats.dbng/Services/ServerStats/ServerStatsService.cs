using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.dbng.Services.ServerStats;

public class ServerStatsService : IServerStatsService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ServerStatsService> logger;

    public ServerStatsService(IServiceScopeFactory scopeFactory, ILogger<ServerStatsService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task<List<ServerStatsResult>> GetSc2ArcadeStats()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from r in context.ArcadeReplays
                    where r.CreatedAt > new DateTime(2021, 2, 1)
                    group r by new { r.CreatedAt.Year, r.CreatedAt.Month, r.GameMode } into g
                    select new ServerStatsResult
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        RegionId = 0,
                        GameMode = g.Key.GameMode,
                        Count = g.Count()
                    };

        return await query.ToListAsync();
    }

    public async Task<List<ServerStatsResult>> GetDsstatsStats()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic, GameMode.Standard };

        var query = from r in context.Replays
                    where r.GameTime > new DateTime(2018, 1, 1)
                        && gameModes.Contains(r.GameMode)
                        && r.Playercount == 6
                    group r by new { r.GameTime.Year, r.GameTime.Month, r.GameMode } into g
                    select new ServerStatsResult
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        RegionId = 0,
                        GameMode = g.Key.GameMode,
                        Count = g.Count()
                    };

        return await query.ToListAsync();
    }

    public Task<MergeResultReplays> GetMergeResultReplays(PlayerId playerId, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}

