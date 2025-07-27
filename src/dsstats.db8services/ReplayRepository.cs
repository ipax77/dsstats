
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace dsstats.db8services;

public class ReplayRepository : IReplayRepository
{
    private readonly ILogger<ReplayRepository> logger;
    private readonly ReplayContext context;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly IMapper mapper;
    private readonly IImportService importService;

    public ReplayRepository(ILogger<ReplayRepository> logger,
                        ReplayContext context,
                        IOptions<DbImportOptions> dbImportOptions,
                        IMapper mapper,
                        IImportService importService)
    {
        this.logger = logger;
        this.context = context;
        this.dbImportOptions = dbImportOptions;
        this.mapper = mapper;
        this.importService = importService;
    }

    public async Task SaveReplay(ReplayDto replayDto)
    {
        await importService.Init();
        replayDto.SetDefaultFilter();

        var dbReplay = mapper.Map<Replay>(replayDto);

        bool isComputer = false;

        foreach (var replayPlayer in dbReplay.ReplayPlayers)
        {
            if (replayPlayer.Player!.ToonId == 0)
            {
                isComputer = true;
            }

            replayPlayer.PlayerId = await importService
                .GetPlayerIdAsync(new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId), 
                    replayPlayer.Name);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            replayPlayer.Player = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            foreach (var spawn in replayPlayer.Spawns)
            {
                spawn.Units = GetMapedSpawnUnits(spawn, replayPlayer.Race);
            }

            replayPlayer.Upgrades = GetMapedPlayerUpgrades(replayPlayer);

        }

        if (isComputer)
        {
            dbReplay.GameMode = GameMode.Tutorial;
        }

        dbReplay.Imported = DateTime.UtcNow;
        context.Replays.Add(dbReplay);

        await context.SaveChangesAsync();
    }

    private ICollection<SpawnUnit> GetMapedSpawnUnits(Spawn spawn, Commander commander)
    {
        List<SpawnUnit> spawnUnits = new();
        foreach (var spawnUnit in spawn.Units)
        {
            spawnUnits.Add(new()
            {
                Count = spawnUnit.Count,
                Poss = spawnUnit.Poss,
                UnitId = importService.GetUnitId(spawnUnit.Unit.Name),
                SpawnId = spawn.SpawnId
            });
        }
        return spawnUnits;
    }

    private ICollection<PlayerUpgrade> GetMapedPlayerUpgrades(ReplayPlayer player)
    {
        List<PlayerUpgrade> playerUpgrades = new();
        foreach (var playerUpgrade in player.Upgrades)
        {
            playerUpgrades.Add(new()
            {
                Gameloop = playerUpgrade.Gameloop,
                UpgradeId = importService.GetUpgradeId(playerUpgrade.Upgrade.Name),
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
