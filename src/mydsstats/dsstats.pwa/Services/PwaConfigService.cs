
using dsstats.indexedDb.Services;
using dsstats.shared;

namespace dsstats.pwa.Services;

public class PwaConfigService(IServiceScopeFactory scopeFactory)
{
    private PwaConfig? config;
    private const string DbName = "DsstatsDB";

    public async Task<PwaConfig> GetConfig()
    {
        if (config == null)
        {
            using var scope = scopeFactory.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
            var result = await dbService.GetConfig();
            if (result is not null)
            {
                config = result;
            }
            else
            {
                config = new();
            }
        }
        return config ?? new PwaConfig();
    }

    public async Task SaveConfig(PwaConfig config)
    {
        using var scope = scopeFactory.CreateScope();
        var dbService = scope.ServiceProvider.GetRequiredService<IndexedDbService>();
        await dbService.SaveConfig(config);
        this.config = config;

        var notificationService = scope.ServiceProvider.GetRequiredService<AppNotificationService>();
        notificationService.ShowSuccess("Config successfully saved.");
    }
}
