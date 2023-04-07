using pax.dsstats.dbng;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Threading.Channels;

namespace dsstats.import.api.Services;

public partial class ImportService
{
    private readonly Channel<Replay> ImportChannel = Channel.CreateUnbounded<Replay>();
    private object lockobject = new();
    private bool importRunning;

    private async Task ConsumeImportChannel()
    {
        lock (lockobject)
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

        int dups = 0;
        int imports = 0;
        int errors = 0;

        try
        {
            while (await ImportChannel.Reader.WaitToReadAsync())
            {
                if (!ImportChannel.Reader.TryRead(out var replay) || replay == null)
                {
                    logger.LogError($"failed reading from Importchannel");
                    continue;
                }

                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

                    await MapReplay(replay, context);
                    AdjustImportValues(replay);

                    if (!await HandleDuplicate(replay))
                    {
                        await SaveReplay(replay, context);
                        imports++;
                    }
                    else
                    {
                        dups++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"failed importing replay: {ex.Message}");
                    if (File.Exists(replay.Blobfile))
                    {
                        File.Move(replay.Blobfile, replay.Blobfile + ".error");
                    }
                    errors++;
                }
                finally
                {
                    if (blobCaches.TryGetValue(replay.Blobfile, out var cache))
                    {
                        cache.Count--;
                        if (cache.Count == 0)
                        {
                            if (File.Exists(replay.Blobfile))
                            {
                                File.Move(replay.Blobfile, replay.Blobfile + ".done");
                            }
                            

                            if (!blobCaches.TryRemove(replay.Blobfile, out cache))
                            {
                                logger.LogWarning($"failed removing blob cache: {replay.Blobfile}");
                            }

                            if (!blobCaches.Any())
                            {
                                logger.LogInformation($"replays imported: {imports}, dups: {dups}, errors: {errors}");

                                sw.Stop();

                                if (stepQueue.Count > 4)
                                {
                                    stepQueue.Dequeue();
                                }

                                stepQueue.Enqueue(new()
                                {
                                    Imported = imports,
                                    Duplicates = dups,
                                    Errors = errors,
                                    ElapsedMs = (int)sw.ElapsedMilliseconds
                                });
                                sw.Reset();

                                imports = 0;
                                dups = 0;
                                errors = 0;

                                // BlobsHandled(new());
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            importRunning = false;
        }
    }

    private async Task SaveReplay(Replay replay, ReplayContext context)
    {
        replay.Imported = DateTime.UtcNow;
        context.Replays.Add(replay);

        await context.SaveChangesAsync();
        await CheatDetectService.AdjustReplay(context, replay, new());
        await context.SaveChangesAsync();

        dbCache.ReplayHashes[replay.ReplayHash] = replay.ReplayId;
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            if (replayPlayer.LastSpawnHash == null)
            {
                continue;
            }
            dbCache.SpawnHashes[replayPlayer.LastSpawnHash] = replay.ReplayId;
        }
    }

    private static void AdjustImportValues(Replay replay)
    {
        if (replay.Middle.Length > 4000)
        {
            replay.Middle = replay.Middle[..3999];
            var middles = replay.Middle.Split('|', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
            replay.Middle = string.Join('|', middles);
        }

        bool isComputerGame = false;
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            replayPlayer.ReplayPlayerId = 0;
            replayPlayer.LastSpawnHash = replayPlayer.Spawns
                .FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?
                .GenHash(replay);

            foreach (var spawnUnit in replayPlayer.Spawns.SelectMany(s => s.Units))
            {
                if (spawnUnit.Poss.Length > 3999)
                {
                    spawnUnit.Poss = spawnUnit.Poss[..3999];
                    var poss = spawnUnit.Poss.Split(',', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
                    if (poss.Count() % 2 != 0)
                    {
                        poss = poss.SkipLast(1);
                    }
                    spawnUnit.Poss = string.Join(',', poss);
                }
            }

            if (replayPlayer.Name.StartsWith("Computer "))
            {
                isComputerGame = true;
            }
        }

        if (isComputerGame)
        {
            replay.GameMode = GameMode.Tutorial;
        }
    }

    private async Task MapReplay(Replay replay, ReplayContext context)
    {
        await MapUnits(replay, context);
        await MapUpgrades(replay, context);
        // await MapPlayers(replay, context);
    }

    private async Task MapPlayers(Replay replay, ReplayContext context)
    {
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            if (replayPlayer.Player == null)
            {
                continue;
            }

            if (!dbCache.Players.TryGetValue(replayPlayer.Player.ToonId, out var playerIdRegionId))
            {
                var player = new Player()
                {
                    Name = replayPlayer.Player.Name,
                    ToonId = replayPlayer.Player.ToonId,
                    RegionId = replayPlayer.Player.RegionId
                };
                context.Players.Add(player);
                await context.SaveChangesAsync();
                playerIdRegionId = new(player.PlayerId, player.RegionId);
                dbCache.Players[player.ToonId] = playerIdRegionId;
            }
            replayPlayer.PlayerId = playerIdRegionId.Key;
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
                if (spawnUnit.Unit == null)
                {
                    continue;
                }

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
            if (plUpgrade.Upgrade == null)
            {
                continue;
            }

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
