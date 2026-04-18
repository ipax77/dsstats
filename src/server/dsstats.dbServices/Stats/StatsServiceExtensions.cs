using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.dbServices.Stats;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddStats(this IServiceCollection services)
    {
        services.AddScoped<IStatsProvider, WinrateStatsProvider>();
        services.AddScoped<IStatsProvider, SynergyStatsProvider>();
        services.AddScoped<IStatsProvider, TimelineStatsProvider>();
        services.AddScoped<IStatsProvider, CountStatsProvider>();
        services.AddScoped<IStatsService, StatsService>();
        return services;
    }
}