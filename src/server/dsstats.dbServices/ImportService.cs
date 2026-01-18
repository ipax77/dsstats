using dsstats.db;
using dsstats.db.Extensions;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public interface IImportService
{
    Task ImportReplay(ReplayDto replayDto);
    Task InsertReplays(List<ReplayDto> replays);
    Task ImportArcadeReplaysRaw(List<ArcadeReplayDto> replays);
    Task ImportArcadeReplays(List<ArcadeReplayDto> replays);
    void ClearExistingArcadeReplayKeys();
    int GetPlayerId(ToonIdDto toonId);
    string GetPlayerName(ToonIdDto toonId);
    int GetOrCreatePlayerId(string name, int region, int realm, int id, DsstatsContext context);
    Task CheckDuplicateCandidates();
    Task FixPlayerNames();
}
public partial class ImportService(IServiceScopeFactory scopeFactory, ILogger<ImportService> logger) : IImportService
{
    bool isInit;
    private Dictionary<ToonIdRec, PlayerInfo> toonIdPlayerIdDict = [];
    private Dictionary<string, int> unitNameIdDict = [];
    private Dictionary<string, int> upgradeNameIdDict = [];
    private readonly Lock initLock = new();
    private readonly Lock unitLock = new();
    private readonly Lock upgradeLock = new();
    private readonly SemaphoreSlim playerSs = new(1, 1);

    private void Init()
    {
        lock (initLock)
        {
            if (isInit)
            {
                return;
            }
            using var scope = scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            toonIdPlayerIdDict = context.Players
                .Select(s => new
                {
                    ToonId = new ToonIdRec(s.ToonId.Region, s.ToonId.Realm, s.ToonId.Id),
                    PlayerInfo = new PlayerInfo(s.PlayerId, s.Name)
                }
                ).ToDictionary(k => k.ToonId, v => v.PlayerInfo);
            unitNameIdDict = context.Units.ToDictionary(k => k.Name, v => v.UnitId);
            upgradeNameIdDict = context.Upgrades.ToDictionary(k => k.Name, v => v.UpgradeId);
            isInit = true;
        }
    }

    public async Task ImportReplay(ReplayDto replayDto)
    {
        Init();

        try
        {
            using var scope = scopeFactory.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            if (!OperatingSystem.IsWindows())
            {
                if (!replayDto.IsValid())
                {
                    return;
                }
                AdjustReplayResult(replayDto);
            }

            var replayEntity = replayDto.ToEntity();

            foreach (var rp in replayEntity.Players)
            {
                if (rp.Player != null)
                {
                    rp.PlayerId = GetOrCreatePlayerId(rp.Name, rp.Player.ToonId.Region, rp.Player.ToonId.Realm, rp.Player.ToonId.Id, context);
                    rp.Player = null;

                    foreach (var spawn in rp.Spawns)
                    {
                        foreach (var spawnUnit in spawn.Units)
                        {
                            if (spawnUnit.Unit == null) continue;
                            spawnUnit.UnitId = GetOrCreateUnitId(spawnUnit.Unit.Name, context);
                            spawnUnit.Unit = null;
                        }
                    }

                    foreach (var playerUpgrade in rp.Upgrades)
                    {
                        if (playerUpgrade.Upgrade == null) continue;
                        playerUpgrade.UpgradeId = GetOrCreateUpgradeId(playerUpgrade.Upgrade.Name, context);
                        playerUpgrade.Upgrade = null;
                    }
                }
            }

            context.Replays.Add(replayEntity);
            await context.SaveChangesAsync();

            var replayCalcDto = replayEntity.ToReplayCalcDto();
            if (replayCalcDto != null)
            {
                var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
                await ratingService.PreRatings([replayCalcDto]);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed importing replay: {error}", ex.Message);
        }
    }

    public int GetPlayerId(ToonIdDto toonId)
    {
        Init();
        var key = new ToonIdRec(toonId.Region, toonId.Realm, toonId.Id);
        if (toonIdPlayerIdDict.TryGetValue(key, out var playerInfo))
        {
            return playerInfo.PlayerId;
        }
        return 0;
    }

    public string GetPlayerName(ToonIdDto toonId)
    {
        Init();
        var key = new ToonIdRec(toonId.Region, toonId.Realm, toonId.Id);
        if (toonIdPlayerIdDict.TryGetValue(key, out var playerInfo))
        {
            return playerInfo.Name;
        }
        return string.Empty;
    }

    public int GetOrCreatePlayerId(
        string name,
        int region,
        int realm,
        int id,
        DsstatsContext context)
    {
        Init();
        playerSs.Wait();
        try
        {
            var toonId = new ToonIdRec(region, realm, id);

            if (toonIdPlayerIdDict.TryGetValue(toonId, out var playerInfo))
            {
                if (!string.Equals(playerInfo.Name, name, StringComparison.Ordinal))
                {
                    var player = context.Players
                        .Single(p =>
                            p.ToonId.Region == region &&
                            p.ToonId.Realm == realm &&
                            p.ToonId.Id == id);

                    player.Name = name;
                    context.SaveChanges();

                    toonIdPlayerIdDict[toonId] =
                        new(playerInfo.PlayerId, name);
                }

                return playerInfo.PlayerId;
            }

            var newPlayer = new Player
            {
                Name = name,
                ToonId = new()
                {
                    Region = region,
                    Realm = realm,
                    Id = id
                }
            };

            context.Players.Add(newPlayer);
            context.SaveChanges();

            toonIdPlayerIdDict[toonId] =
                new(newPlayer.PlayerId, newPlayer.Name);

            return newPlayer.PlayerId;
        }
        finally
        {
            playerSs.Release();
        }
    }

    public int GetOrCreateUnitId(string name, DsstatsContext context)
    {
        lock (unitLock)
        {
            if (unitNameIdDict.TryGetValue(name, out var unitId))
            {
                return unitId;
            }
            var unit = new Unit()
            {
                Name = name,
            };
            context.Units.Add(unit);
            context.SaveChanges();
            unitNameIdDict[name] = unit.UnitId;
            return unit.UnitId;
        }
    }

    public int GetOrCreateUpgradeId(string name, DsstatsContext context)
    {
        lock (upgradeLock)
        {
            if (upgradeNameIdDict.TryGetValue(name, out var upgradeId))
            {
                return upgradeId;
            }
            var upgrade = new Upgrade()
            {
                Name = name,
            };
            context.Upgrades.Add(upgrade);
            context.SaveChanges();
            upgradeNameIdDict[name] = upgrade.UpgradeId;
            return upgrade.UpgradeId;
        }
    }

    public async Task InsertReplays(List<ReplayDto> replays)
    {
        Init();

        Dictionary<ToonIdRec, string> players = [];
        foreach (var player in replays.SelectMany(s => s.Players))
        {
            var key = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
            players[key] = player.Name;
        }

        await CreatePlayerIds(players);

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var dbReplays = new List<Replay>();

        foreach (var replay in replays)
        {
            if (!OperatingSystem.IsWindows())
            {
                if (!replay.IsValid())
                {
                    continue;
                }
                AdjustReplayResult(replay);
            }

            var dbReplay = replay.ToEntity();

            foreach (var player in dbReplay.Players)
            {
                ArgumentNullException.ThrowIfNull(player.Player);
                var toonIdRec = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
                if (toonIdPlayerIdDict.TryGetValue(toonIdRec, out var playerInfo))
                {
                    player.PlayerId = playerInfo.PlayerId;
                    player.Player = null;
                }
                else
                {
                    throw new InvalidOperationException($"playerId not found for {toonIdRec}");
                }

                foreach (var upgrade in player.Upgrades)
                {
                    ArgumentNullException.ThrowIfNull(upgrade.Upgrade);
                    upgrade.UpgradeId = GetOrCreateUpgradeId(upgrade.Upgrade.Name, context);
                    upgrade.Upgrade = null;
                }

                foreach (var spawn in player.Spawns)
                {
                    foreach (var unit in spawn.Units)
                    {
                        ArgumentNullException.ThrowIfNull(unit.Unit);
                        unit.UnitId = GetOrCreateUnitId(unit.Unit.Name, context);
                        unit.Unit = null;
                    }
                }
            }
            dbReplays.Add(dbReplay);
        }

        var duplicateResult = await HandleDuplicates(dbReplays, context);

        if (duplicateResult.ReplaysToImport.Count > 0)
        {
            await context.Replays.AddRangeAsync(duplicateResult.ReplaysToImport);
            await context.SaveChangesAsync();

            var fromDate = DateTime.UtcNow.AddHours(-3);
            var replayCalcDtos = duplicateResult.ReplaysToImport
                .Where(x => x.Gametime > fromDate)
                .AsQueryable()
                .ToReplayCalcDtos()
                .ToList();
            if (replayCalcDtos.Count > 0)
            {
                var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
                await ratingService.PreRatings(replayCalcDtos);
            }
        }
        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("Replays imported: {count}, dups: {duplicates}, replaced: {replaced}",
             duplicateResult.ReplaysToImport.Count, duplicateResult.Duplicates, duplicateResult.Replaced);
        }
        else
        {
            logger.LogInformation("Replays imported: {count}, dups: {duplicates}, replaced: {replaced}",
             duplicateResult.ReplaysToImport.Count, duplicateResult.Duplicates, duplicateResult.Replaced);
        }
    }

    private async Task CreatePlayerIds(Dictionary<ToonIdRec, string> players)
    {
        await playerSs.WaitAsync();
        try
        {
            List<Player> playersToInsert = [];
            Dictionary<ToonIdRec, PlayerInfo> playersToRename = [];

            // First pass: classify work
            foreach (var (toonId, name) in players)
            {
                if (!toonIdPlayerIdDict.TryGetValue(toonId, out var cached))
                {
                    playersToInsert.Add(new Player
                    {
                        Name = name,
                        ToonId = new()
                        {
                            Region = toonId.Region,
                            Realm = toonId.Realm,
                            Id = toonId.Id
                        }
                    });
                }
                else if (!string.Equals(cached.Name, name, StringComparison.Ordinal))
                {
                    playersToRename.Add(toonId, new(cached.PlayerId, name));
                }
            }

            if (playersToInsert.Count == 0 && playersToRename.Count == 0)
            {
                return;
            }

            using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            // Insert new players
            if (playersToInsert.Count > 0)
            {
                context.Players.AddRange(playersToInsert);
            }

            // Rename existing players
            if (playersToRename.Count > 0)
            {
                var idsToUpdate = playersToRename.Values
                    .Select(p => p.PlayerId)
                    .ToHashSet();

                var dbPlayers = await context.Players
                    .Where(p => idsToUpdate.Contains(p.PlayerId))
                    .ToListAsync();

                foreach (var dbPlayer in dbPlayers)
                {
                    ToonIdRec toonId = new(dbPlayer.ToonId.Region, dbPlayer.ToonId.Realm, dbPlayer.ToonId.Id);
                    if (playersToRename.TryGetValue(toonId, out var rnPlayer))
                    {
                        dbPlayer.Name = rnPlayer.Name;
                    }
                }
            }

            await context.SaveChangesAsync();

            // Update cache for inserts
            foreach (var player in playersToInsert)
            {
                var key = new ToonIdRec(
                    player.ToonId.Region,
                    player.ToonId.Realm,
                    player.ToonId.Id);

                toonIdPlayerIdDict[key] =
                    new(player.PlayerId, player.Name);
            }

            foreach (var ent in playersToRename)
            {
                toonIdPlayerIdDict[ent.Key] =
                    ent.Value;
            }
        }
        finally
        {
            playerSs.Release();
        }
    }
}

