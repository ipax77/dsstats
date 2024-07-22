
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services.Import;

public partial class ImportService : IImportService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ImportService> logger;
    private readonly bool IsMaui;

    public ImportService(IServiceProvider serviceProvider, ILogger<ImportService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        using var scope = serviceProvider.CreateScope();
        var remoteToggleService = scope.ServiceProvider.GetRequiredService<IRemoteToggleService>();
        IsMaui = remoteToggleService.IsMaui;
    }

    Dictionary<string, int> Units = new();
    Dictionary<string, int> Upgrades = new();
    Dictionary<PlayerId, int> PlayerIds = new();

    SemaphoreSlim unitsSs = new(1, 1);
    SemaphoreSlim upgradesSs = new(1, 1);
    SemaphoreSlim playersSs = new(1, 1);
    SemaphoreSlim initSs = new(1, 1);
    public bool IsInit { get; private set; }

    public async Task Init()
    {
        await initSs.WaitAsync();
        if (IsInit)
        {
            return;
        }
        try
        {
            using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            Units = (await context.Units
                .Select(s => new { s.Name, s.UnitId }).ToListAsync())
                .ToDictionary(k => k.Name, v => v.UnitId);
            Upgrades = (await context.Upgrades
                .Select(s => new { s.Name, s.UpgradeId }).ToListAsync())
                .ToDictionary(k => k.Name, v => v.UpgradeId);
            PlayerIds = (await context.Players
                .Select(s => new { s.ToonId, s.RealmId, s.RegionId, s.PlayerId }).ToListAsync())
                .ToDictionary(k => new PlayerId(k.ToonId, k.RealmId, k.RegionId), v => v.PlayerId);
        }
        finally
        {
            IsInit = true;
            initSs.Release();
        }
    }

    public async Task<Dictionary<PlayerId, int>> GetPlayerIdDictionary()
    {
        if (!IsInit)
        {
            await Init();
        }
        return PlayerIds;
    }

    public async Task<int> GetPlayerIdAsync(PlayerId playerId, string name)
    {
        if (!IsInit)
        {
            await Init();
        }

        await playersSs.WaitAsync();
        try
        {

            if (!PlayerIds.TryGetValue(playerId, out var dbPlayerId))
            {
                Player player = new()
                {
                    Name = name,
                    ToonId = playerId.ToonId,
                    RealmId = playerId.RealmId,
                    RegionId = playerId.RegionId
                };
                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Players.Add(player);
                await context.SaveChangesAsync();
                dbPlayerId = PlayerIds[playerId] = player.PlayerId;
            }
            return dbPlayerId;
        }
        finally
        {
            playersSs.Release();
        }
    }

    private async Task<Dictionary<PlayerId, int>> GetPlayerIds(List<RequestNames> requestNames)
    {
        Dictionary<PlayerId, int> playerIds = new();
        List<Player> missingPlayers = new();

        await playersSs.WaitAsync();
        try
        {
            foreach (var requestName in requestNames)
            {
                PlayerId playerId = new(requestName.ToonId, requestName.RealmId, requestName.RegionId);
                if (!PlayerIds.TryGetValue(playerId, out var dbPlayerId))
                {
                    missingPlayers.Add(new()
                    {
                        Name = requestName.Name,
                        ToonId = requestName.ToonId,
                        RealmId = requestName.RealmId,
                        RegionId = requestName.RegionId
                    });
                }
                else
                {
                    playerIds[playerId] = dbPlayerId;
                }
            }


            if (missingPlayers.Count > 0)
            {

                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Players.AddRange(missingPlayers);
                await context.SaveChangesAsync();
                foreach (var player in missingPlayers)
                {
                    PlayerId playerId = new(player.ToonId, player.RealmId, player.RegionId);
                    playerIds[playerId] = PlayerIds[playerId] = player.PlayerId;
                }

            }
            return playerIds;
        }
        finally
        {
            playersSs.Release();
        }
    }

    private async Task<int> GetUnitIdAsync(string name)
    {
        await unitsSs.WaitAsync();
        try
        {

            if (!Units.TryGetValue(name, out var unitId))
            {
                Unit unit = new()
                {
                    Name = name
                };
                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Units.Add(unit);
                await context.SaveChangesAsync();
                unitId = Units[name] = unit.UnitId;
            }
            return unitId;
        }
        finally
        {
            unitsSs.Release();
        }
    }

    private int GetUnitId(string name)
    {
        if (!Units.TryGetValue(name, out var unitId))
        {
            unitsSs.Wait();
            try
            {
                Unit unit = new()
                {
                    Name = name
                };
                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Units.Add(unit);
                context.SaveChanges();
                unitId = Units[name] = unit.UnitId;
            }
            catch (Exception)
            {
                unitId = Units.ContainsKey(name) ? Units[name] : 1;
            }
            finally
            {
                unitsSs.Release();
            }
        }
        return unitId;
    }

    private async Task<int> GetUpgradeIdAsync(string name)
    {
        await upgradesSs.WaitAsync();
        try
        {

            if (!Upgrades.TryGetValue(name, out var upgradeId))
            {
                Upgrade upgrade = new()
                {
                    Name = name
                };
                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Upgrades.Add(upgrade);
                await context.SaveChangesAsync();
                upgradeId = Upgrades[name] = upgrade.UpgradeId;
            }
            return upgradeId;
        }
        finally
        {
            upgradesSs.Release();
        }
    }

    private int GetUpgradeId(string name)
    {
        if (!Upgrades.TryGetValue(name, out var upgradeId))
        {
            upgradesSs.Wait();
            try
            {
                Upgrade upgrade = new()
                {
                    Name = name
                };
                using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                context.Upgrades.Add(upgrade);
                context.SaveChanges();
                upgradeId = Upgrades[name] = upgrade.UpgradeId;
            }
            catch (Exception)
            {
                upgradeId = Upgrades.ContainsKey(name) ? Upgrades[name] : 1;
            }
            finally
            {
                upgradesSs.Release();
            }
        }
        return upgradeId;
    }

    public async Task<Dictionary<int, int>> MapArcadePlayers()
    {
        await playersSs.WaitAsync();
        Dictionary<int, int> arcadePlayerIdMap = [];
        try
        {
            if (!IsInit)
            {
                await Init();
            }
            List<Player> newPlayers = [];

            using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var arcadePlayersDict = (await context.ArcadePlayers
                .ToListAsync()).ToDictionary(k => new PlayerId(k.ProfileId, k.RealmId, k.RegionId), v => v);

            foreach (var arcadePlayer in arcadePlayersDict.Values)
            {
                var playerId = new PlayerId(arcadePlayer.ProfileId, arcadePlayer.RealmId, arcadePlayer.RegionId);
                if (PlayerIds.TryGetValue(playerId, out var dsPlayerId))
                {
                    arcadePlayerIdMap[arcadePlayer.ArcadePlayerId] = dsPlayerId;
                }
                else
                {
                    newPlayers.Add(new()
                    {
                        Name = arcadePlayer.Name,
                        ToonId = arcadePlayer.ProfileId,
                        RealmId = arcadePlayer.RealmId,
                        RegionId = arcadePlayer.RegionId,
                    });
                }
            }
            context.Players.AddRange(newPlayers);
            await context.SaveChangesAsync();
            foreach (var player in newPlayers)
            {
                var playerId = new PlayerId(player.ToonId, player.RealmId, player.RegionId);
                PlayerIds[playerId] = player.PlayerId;
                arcadePlayerIdMap[arcadePlayersDict[playerId].ArcadePlayerId] = player.PlayerId;
            }
        }
        finally
        {
            playersSs.Release();
        }
        return arcadePlayerIdMap;
    }
}
