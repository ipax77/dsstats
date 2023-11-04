
using AutoMapper;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dsstats.services;

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

    public async Task<(HashSet<Unit>, HashSet<Upgrade>, Replay)> SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades, ReplayEventDto? replayEventDto)
    {
        replayDto.SetDefaultFilter();
        var dbReplay = mapper.Map<Replay>(replayDto);

        if (replayDto.ReplayEvent != null)
        {
            replayEventDto = replayDto.ReplayEvent;
        }

        if (replayEventDto != null)
        {
            var dbEvent = await context.Events.FirstOrDefaultAsync(f => f.Name == replayEventDto.Event.Name);

            if (dbEvent == null)
            {
                dbEvent = new()
                {
                    Name = replayEventDto.Event.Name,
                    EventStart = DateTime.UtcNow.Date
                };
                context.Events.Add(dbEvent);
                await context.SaveChangesAsync();
            }

            var replayEvent = await context.ReplayEvents.FirstOrDefaultAsync(f => f.Event == dbEvent && f.Round == replayEventDto.Round && f.WinnerTeam == replayEventDto.WinnerTeam && f.RunnerTeam == replayEventDto.RunnerTeam);

            if (replayEvent == null)
            {
                replayEvent = new()
                {
                    Round = replayEventDto.Round,
                    WinnerTeam = replayEventDto.WinnerTeam,
                    RunnerTeam = replayEventDto.RunnerTeam,
                    Ban1 = (int)replayEventDto.Ban1,
                    Ban2 = (int)replayEventDto.Ban2,
                    Ban3 = (int)replayEventDto.Ban3,
                    Ban4 = (int)replayEventDto.Ban4,
                    Ban5 = (int)replayEventDto.Ban5,
                    Event = dbEvent
                };
                context.ReplayEvents.Add(replayEvent);
                await context.SaveChangesAsync();
            }
            dbReplay.ReplayEvent = replayEvent;
        }

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
                    logger.LogError($"failed saving replay: {ex.Message}");
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
                (spawn.SpawnUnits, units) = await GetMapedSpawnUnits(spawn, replayPlayer.Race, units);
            }

            (replayPlayer.PlayerUpgrades, upgrades) = await GetMapedPlayerUpgrades(replayPlayer, upgrades);

        }

        if (isComputer)
        {
            dbReplay.GameMode = (int)GameMode.Tutorial;
        }

        dbReplay.Imported = DateTime.UtcNow;
        context.Replays.Add(dbReplay);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed saving replay: {ex.Message}");
            throw;
        }

        return (units, upgrades, dbReplay);
    }

    private async Task<(ICollection<SpawnUnit>, HashSet<Unit>)> GetMapedSpawnUnits(Spawn spawn, Commander commander, HashSet<Unit> units)
    {
        List<SpawnUnit> spawnUnits = new();
        foreach (var spawnUnit in spawn.SpawnUnits)
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
        return (spawnUnits, units);
    }

    private async Task<(ICollection<PlayerUpgrade>, HashSet<Upgrade>)> GetMapedPlayerUpgrades(ReplayPlayer player, HashSet<Upgrade> upgrades)
    {
        List<PlayerUpgrade> playerUpgrades = new();
        foreach (var playerUpgrade in player.PlayerUpgrades)
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
        return (playerUpgrades, upgrades);
    }
}
