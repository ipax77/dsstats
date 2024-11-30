
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;

namespace dsstats.db8services;

public class ReplayRepository : IReplayRepository
{
    private readonly ILogger<ReplayRepository> logger;
    private readonly ReplayContext context;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly IMapper mapper;

    public ReplayRepository(ILogger<ReplayRepository> logger,
                        ReplayContext context,
                        IOptions<DbImportOptions> dbImportOptions,
                        IMapper mapper)
    {
        this.logger = logger;
        this.context = context;
        this.dbImportOptions = dbImportOptions;
        this.mapper = mapper;
    }

    public async Task SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades)
    {
        replayDto.SetDefaultFilter();

        var dbReplay = mapper.Map<Replay>(replayDto);

        bool isComputer = false;

        foreach (var replayPlayer in dbReplay.ReplayPlayers)
        {
            if (replayPlayer.Player.ToonId == 0)
            {
                isComputer = true;
            }

            var dbPlayer = await context.Players.FirstOrDefaultAsync(f =>
                f.ToonId == replayPlayer.Player.ToonId
                && f.RealmId == replayPlayer.Player.RealmId
                && f.RegionId == replayPlayer.Player.RegionId);
            if (dbPlayer == null)
            {
                dbPlayer = new()
                {
                    Name = replayPlayer.Player.Name,
                    ToonId = replayPlayer.Player.ToonId,
                    RegionId = replayPlayer.Player.RegionId,
                    RealmId = replayPlayer.Player.RealmId,
                };
                context.Players.Add(dbPlayer);
                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError("failed saving replay: {error}", ex.Message);
                    throw;
                }
            }
            else
            {
                dbPlayer.RegionId = replayPlayer.Player.RegionId;
                dbPlayer.Name = replayPlayer.Player.Name;
            }

            replayPlayer.Player = dbPlayer;
            replayPlayer.Name = dbPlayer.Name;

            foreach (var spawn in replayPlayer.Spawns)
            {
                spawn.Units = await GetMapedSpawnUnits(spawn, replayPlayer.Race, units);
            }

            replayPlayer.Upgrades = await GetMapedPlayerUpgrades(replayPlayer, upgrades);

        }

        if (isComputer)
        {
            dbReplay.GameMode = GameMode.Tutorial;
        }

        dbReplay.Imported = DateTime.UtcNow;
        context.Replays.Add(dbReplay);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving replay: {error}", ex.Message);
            throw;
        }
    }

    private async Task<ICollection<SpawnUnit>> GetMapedSpawnUnits(Spawn spawn, Commander commander, HashSet<Unit> units)
    {
        List<SpawnUnit> spawnUnits = new();
        foreach (var spawnUnit in spawn.Units)
        {
            var listUnit = units.FirstOrDefault(f => f.Name.Equals(spawnUnit.Unit.Name));
            if (listUnit == null)
            {
                listUnit = new()
                {
                    Name = spawnUnit.Unit.Name
                };
                context.Units.Add(listUnit);
                await context.SaveChangesAsync();
                units.Add(listUnit);
            }

            spawnUnits.Add(new()
            {
                Count = spawnUnit.Count,
                Poss = spawnUnit.Poss,
                UnitId = listUnit.UnitId,
                SpawnId = spawn.SpawnId
            });
        }
        return spawnUnits;
    }

    private async Task<ICollection<PlayerUpgrade>> GetMapedPlayerUpgrades(ReplayPlayer player, HashSet<Upgrade> upgrades)
    {
        List<PlayerUpgrade> playerUpgrades = new();
        foreach (var playerUpgrade in player.Upgrades)
        {
            var listUpgrade = upgrades.FirstOrDefault(f => f.Name.Equals(playerUpgrade.Upgrade.Name));
            if (listUpgrade == null)
            {
                listUpgrade = new()
                {
                    Name = playerUpgrade.Upgrade.Name
                };
                context.Upgrades.Add(listUpgrade);
                await context.SaveChangesAsync();
                upgrades.Add(listUpgrade);
            }

            playerUpgrades.Add(new()
            {
                Gameloop = playerUpgrade.Gameloop,
                UpgradeId = listUpgrade.UpgradeId,
                ReplayPlayerId = player.ReplayPlayerId
            });
        }
        return playerUpgrades;
    }

    public async Task<ReplayDto?> GetLatestReplay()
    {
        return await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(o => o.GameTime)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<ReplayDto?> GetPreviousReplay(DateTime gameTime)
    {
        return await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(o => o.GameTime)
            .Where(x => x.GameTime < gameTime)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<ReplayDto?> GetNextReplay(DateTime gameTime)
    {
        return await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(o => o.GameTime)
            .Where(x => x.GameTime > gameTime)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<ReplayDto?> GetReplay(string replayHash)
    {
        return await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.ReplayHash == replayHash)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task SetReplayViews()
    {
        var viewedHashes = await context.ReplayViewCounts
            .ToListAsync();

        var replayHashViews = viewedHashes.GroupBy(g => g.ReplayHash)
            .Select(s => new { Hash = s.Key, Count = s.Count() })
            .ToDictionary(k => k.Hash, v => v.Count);

        int i = 0;
        foreach (var ent in replayHashViews)
        {
            var replay = await context.Replays
                .FirstOrDefaultAsync(f => f.ReplayHash == ent.Key);
            if (replay != null)
            {
                replay.Views += ent.Value;
            }
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();

        context.ReplayViewCounts.RemoveRange(viewedHashes);

        await context.SaveChangesAsync();
    }

    public async Task SetReplayDownloads()
    {
        var downloadedHashes = await context.ReplayDownloadCounts
            .ToListAsync();

        var replayHashDownloads = downloadedHashes.GroupBy(g => g.ReplayHash)
            .Select(s => new { Hash = s.Key, Count = s.Count() })
            .ToDictionary(k => k.Hash, v => v.Count);

        int i = 0;
        foreach (var ent in replayHashDownloads)
        {
            var replay = await context.Replays
                .FirstOrDefaultAsync(f => f.ReplayHash == ent.Key);
            if (replay != null)
            {
                replay.Downloads += ent.Value;
            }
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();

        context.ReplayDownloadCounts.RemoveRange(downloadedHashes);

        await context.SaveChangesAsync();
    }

    public async Task FixDsstatsPlayerNames()
    {
        var fromDate = DateTime.Today.AddDays(-1);

        Stopwatch sw = Stopwatch.StartNew();

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => x.GameTime > fromDate)
            .OrderByDescending(o => o.GameTime)
            .ToListAsync();

        if (replays.Count == 0)
        {
            return;
        }

        Dictionary<int, string> playersDone = new();

        foreach (var replay in replays)
        {
            foreach (var replayPlayer in replay.ReplayPlayers)
            {
                if (playersDone.ContainsKey(replayPlayer.Player.PlayerId))
                {
                    continue;
                }

                if (replayPlayer.Name != replayPlayer.Player.Name)
                {
                    replayPlayer.Player.Name = replayPlayer.Name;
                    playersDone[replayPlayer.Player.PlayerId] = replayPlayer.Name;
                }
                else
                {
                    playersDone[replayPlayer.Player.PlayerId] = replayPlayer.Player.Name;
                }
            }
        }
        int count = await context.SaveChangesAsync();
        sw.Stop();
        logger.LogWarning("Dsstats {count} player names fixed in {ms} ms", count, sw.ElapsedMilliseconds);
    }

    public async Task FixArcadePlayerNames()
    {
        var fromDate = DateTime.Today.AddDays(-6);

        Stopwatch sw = Stopwatch.StartNew();

        var replays = await context.ArcadeReplays
            .Include(i => i.ArcadeReplayDsPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => x.CreatedAt > fromDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        if (replays.Count == 0)
        {
            return;
        }

        Dictionary<int, string> playersDone = new();

        foreach (var replay in replays)
        {
            foreach (var replayPlayer in replay.ArcadeReplayDsPlayers)
            {
                if (playersDone.ContainsKey(replayPlayer.Player!.PlayerId))
                {
                    continue;
                }

                if (replayPlayer.Name != replayPlayer.Player.Name)
                {
                    replayPlayer.Player.Name = replayPlayer.Name;
                    playersDone[replayPlayer.Player!.PlayerId] = replayPlayer.Name;
                }
                else
                {
                    playersDone[replayPlayer.Player!.PlayerId] = replayPlayer.Player.Name;
                }
            }
        }
        int count = await context.SaveChangesAsync();
        sw.Stop();
        logger.LogWarning("Arcade {count} player names fixed in {ms} ms", count, sw.ElapsedMilliseconds);
    }
}
