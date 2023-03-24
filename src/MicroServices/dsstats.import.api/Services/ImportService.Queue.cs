using pax.dsstats.dbng;
using System.Threading.Channels;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    private readonly Channel<Replay> ImportChannel = Channel.CreateUnbounded<Replay>();
    private object lockobject = new();
    private bool importRunning;

    private async Task ConsumeImportChannel()
    {
        lock(lockobject)
        {
            if (importRunning)
            {
                return;
            }
            else
            {
                importRunning = true;
            }
        }

        try
        {
            while (await ImportChannel.Reader.WaitToReadAsync())
            {
                if (!ImportChannel.Reader.TryRead(out var replay) || replay == null)
                {
                    logger.LogError($"failed reading from Importchannel");
                    continue;
                }
                
                await MapReplay(replay);
                if (!await HandleDuplicate(replay))
                {
                    await SaveReplay(replay);
                }
            }
        } 
        finally
        {
            importRunning = false;
        }
    }

    private Task SaveReplay(Replay replay)
    {
        throw new NotImplementedException();
    }

    private async Task MapReplay(Replay replay)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        await MapUnits(replay, context);
        await MapUpgrades(replay, context);
        await MapPlayers(replay, context);
    }

    private async Task MapPlayers(Replay replay, ReplayContext context)
    {
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            if (replayPlayer.Player == null)
            {
                continue;
            }

            if (!dbCache.Players.TryGetValue(replayPlayer.PlayerId, out int playerId))
            {
                var player = new Player()
                {
                    Name = replayPlayer.Player.Name,
                    ToonId = replayPlayer.Player.ToonId,
                    RegionId = replayPlayer.Player.RegionId
                };
                context.Players.Add(player);
                await context.SaveChangesAsync();
                playerId = player.PlayerId;
                dbCache.Players[player.ToonId] = playerId;
            }
            replayPlayer.PlayerId = playerId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            replayPlayer.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }

    private async Task MapUnits(Replay replay, ReplayContext context)
    {
        foreach (var spawn in replay.ReplayPlayers.SelectMany(s => s.Spawns))
        {
            foreach (var spawnUnit in spawn.Units)
            {
                if (!dbCache.Units.TryGetValue(spawnUnit.Unit.Name, out int unitId))
                {
                    var unit = new Unit()
                    {
                        Name = spawnUnit.Unit.Name
                    };
                    context.Units.Add(unit);
                    await context.SaveChangesAsync();
                    unitId = unit.UnitId;
                    dbCache.Units[spawnUnit.Unit.Name] = unitId;
                }
                spawnUnit.UnitId = unitId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                spawnUnit.Unit = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }
        }
    }

    private async Task MapUpgrades(Replay replay, ReplayContext context)
    {
        foreach (var plUpgrade in replay.ReplayPlayers.SelectMany(s => s.Upgrades))
        {
            if (!dbCache.Upgrades.TryGetValue(plUpgrade.Upgrade.Name, out int upgradeId))
            {
                var upgrade = new Upgrade()
                {
                    Name = plUpgrade.Upgrade.Name
                };
                context.Upgrades.Add(upgrade);
                await context.SaveChangesAsync();
                upgradeId = upgrade.UpgradeId;
                dbCache.Upgrades[plUpgrade.Upgrade.Name] = upgradeId;
            }
            plUpgrade.UpgradeId = upgradeId;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            plUpgrade.Upgrade = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
