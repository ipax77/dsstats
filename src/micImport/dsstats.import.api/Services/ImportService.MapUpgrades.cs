
using pax.dsstats.dbng;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    public async Task<int> CreateAndMapUpgrades(List<Replay> replays)
    {
        int newUpgrades = await CreateMissingUpgrades(replays);
        MapUpgrades(replays);
        return newUpgrades;
    }

    private void MapUpgrades(List<Replay> replays)
    {
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ReplayPlayers)
            {
                rp.Upgrades = rp.Upgrades.Select(s => new PlayerUpgrade()
                {
                    Gameloop = s.Gameloop,
                    UpgradeId = dbCache.Upgrades[s.Upgrade.Name]
                }).ToList();
            }
        }
    }

    private async Task<int> CreateMissingUpgrades(List<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Upgrade> newUpgrades = new();
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ReplayPlayers)
            { 
                foreach (var plupgrade in rp.Upgrades)
                {
                    if (!dbCache.Upgrades.ContainsKey(plupgrade.Upgrade.Name))
                    {
                        Upgrade upgrade = new()
                        {
                            Name = plupgrade.Upgrade.Name,
                        };
                        context.Upgrades.Add(upgrade);
                        newUpgrades.Add(upgrade);
                        dbCache.Upgrades[plupgrade.Upgrade.Name] = 0;
                    }
                }
            }
        }
        if (newUpgrades.Any())
        {
            await context.SaveChangesAsync();
            newUpgrades.ForEach(f => dbCache.Upgrades[f.Name] = f.UpgradeId);
        }
        return newUpgrades.Count;
    }
}
