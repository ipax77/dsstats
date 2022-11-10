
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    public async Task<int> CreateAndMapUpgrades(ICollection<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var upgradesDic = await GetUpgradesDic(context);
        var newUpgrades = await CreateMissingUpgrades(context, upgradesDic, replays);
        newUpgrades.ForEach(f => upgradesDic[f.Name] = f.UpgradeId);
        MapPlayerUpgrades(upgradesDic, replays);
        return newUpgrades.Count;
    }

    private static async Task<Dictionary<string, int>> GetUpgradesDic(ReplayContext context)
    {
        var untrackedUpgrades = await context.Upgrades
            .AsNoTracking()
            .ToListAsync();

        return untrackedUpgrades.ToDictionary(k => k.Name, v => v.UpgradeId);
    }

    private async Task<List<Upgrade>> CreateMissingUpgrades(ReplayContext context, Dictionary<string, int> untrackedUpgradesDic, ICollection<Replay> replays)
    {
        List<Upgrade> newUpgrades = new();
        foreach (var upgrade in replays.SelectMany(s => s.ReplayPlayers).SelectMany(s => s.Upgrades).Select(s => mapper.Map<UpgradeDto>(s.Upgrade)).Distinct())
        {
            if (!untrackedUpgradesDic.ContainsKey(upgrade.Name))
            {
                var dbUpgrade = mapper.Map<Upgrade>(upgrade);
                context.Upgrades.Add(dbUpgrade);
                newUpgrades.Add(dbUpgrade);
                if (newUpgrades.Count % 1000 == 0)
                {
                    await context.SaveChangesAsync();
                }
                untrackedUpgradesDic[upgrade.Name] = dbUpgrade.UpgradeId;
            }
        }
        if (newUpgrades.Count > 0)
        {
            await context.SaveChangesAsync();
        }
        return newUpgrades;
    }

    private static void MapPlayerUpgrades(Dictionary<string, int> untrackedUpgradesDic, ICollection<Replay> replays)
    {
        foreach (var player in replays.SelectMany(s => s.ReplayPlayers))
        {
            player.Upgrades = player.Upgrades.Select(s => new PlayerUpgrade()
            {
                Gameloop = s.Gameloop,
                ReplayPlayerId = s.ReplayPlayerId,
                UpgradeId = untrackedUpgradesDic[s.Upgrade.Name]
            }).ToList();
        }
    }
}
