using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;

namespace dsstats.import;

internal class ImportJob
{
    private readonly IServiceProvider serviceProvider;
    private DbImportCache DbCache = new();

    public ImportJob(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public void Import()
    {
        
    }

    private void SeedDbCache(ReplayContext context)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ReplayContext>();

        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var units = context.Units.ToList();
        var upgrades = context.Upgrades.ToList();
        var players = context.Players.ToList();


    }
}

public record DbImportCache
{
    public Dictionary<string, Unit> Units { get; set; } = new();
    public Dictionary<string, Upgrade> Upgrades { get; set; } = new();
    public Dictionary<int, Player> Players { get; set; } = new();
}