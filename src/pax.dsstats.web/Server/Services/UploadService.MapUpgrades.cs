using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private SemaphoreSlim ssMapUpgrades = new(1, 1);
    private Dictionary<string, int> upgradesDic = new();

    public async Task MapUpgrades(ICollection<Replay> replays)
    {
        await ssMapUpgrades.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            if (!upgradesDic.Any())
            {
                await InitUpgradesDic(context);
            }

            await CreateMissingUpgrades(context, upgradesDic, replays);

            MapPlayerUpgrades(upgradesDic, replays);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed mapping upgrades: {ex.Message}");
        }
        finally
        {
            ssMapUpgrades.Release();
        }
    }

    private async Task InitUpgradesDic(ReplayContext context)
    {
        var untrackedUpgrades = await context.Upgrades
            .AsNoTracking()
            .ToListAsync();

        upgradesDic = untrackedUpgrades.ToDictionary(k => k.Name, v => v.UpgradeId);
    }

    private async Task CreateMissingUpgrades(ReplayContext context, Dictionary<string, int> untrackedUpgradesDic, ICollection<Replay> replays)
    {
        foreach (var upgrade in replays.SelectMany(s => s.Players).SelectMany(s => s.Upgrades).Select(s => mapper.Map<UpgradeDto>(s.Upgrade)).Distinct())
        {
            if (!untrackedUpgradesDic.ContainsKey(upgrade.Name))
            {
                var dbUpgrade = await CreateUpgrade(context, upgrade);
                untrackedUpgradesDic[upgrade.Name] = dbUpgrade.UpgradeId;
            }
        }
    }

    private void MapPlayerUpgrades(Dictionary<string, int> untrackedUpgradesDic, ICollection<Replay> replays)
    {
        foreach (var player in replays.SelectMany(s => s.Players))
        {
            player.Upgrades = player.Upgrades.Select(s => new PlayerUpgrade()
            {
                Gameloop = s.Gameloop,
                ReplayPlayerId = s.ReplayPlayerId,
                UpgradeId = untrackedUpgradesDic[s.Upgrade.Name]
            }).ToList();
        }
    }

    private async Task<Upgrade> CreateUpgrade(ReplayContext context, UpgradeDto upgradeDto)
    {
        var upgrade = mapper.Map<Upgrade>(upgradeDto);
        context.Upgrades.Add(upgrade);
        await context.SaveChangesAsync();
        return upgrade;
    }
}
