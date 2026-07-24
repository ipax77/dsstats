using dsstats.db;
using dsstats.db.Extensions;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.dbServices;

public interface IImportService
{
    Task ImportReplay(ReplayDto replayDto);
    Task InsertReplays(List<ReplayDto> replays);
    Task InsertReplayImports(List<ReplayImportDto> imports);
    Task<ReplayImportBatchResultDto> InsertReplayImportsWithSidecars(
        UploadRequestDto request,
        IReadOnlyList<SpawnPlaybackUploadManifestEntryDto> manifestEntries,
        IReadOnlyDictionary<string, SpawnPlaybackUploadPayload> payloadsByPartName,
        CancellationToken token = default);
    Task ImportArcadeReplaysRaw(List<ArcadeReplayDto> replays);
    Task ImportArcadeReplays(List<ArcadeReplayDto> replays);
    void ClearExistingArcadeReplayKeys();
    int GetPlayerId(ToonIdDto toonId);
    string GetPlayerName(ToonIdDto toonId);
    int GetOrCreatePlayerId(string name, int region, int realm, int id);
    Task CheckDuplicateCandidates();
    Task FixPlayerNames();
    Task CheckRealmDuplicateCandidates();
}
public partial class ImportService(
    IDbContextFactory<DsstatsContext> contextFactory,
    IServiceScopeFactory scopeFactory,
    IRatingService ratingService,
    ILogger<ImportService> logger) : IImportService
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
            using var context = contextFactory.CreateDbContext();
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
        NormalizeReplayPlayerCompatHashes(replayDto);

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();

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

    public int GetOrCreatePlayerId(string name, int region, int realm, int id)
    {
        using var context = contextFactory.CreateDbContext();
        return GetOrCreatePlayerId(name, region, realm, id, context);
    }

    private int GetOrCreatePlayerId(
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

    public Task InsertReplays(List<ReplayDto> replays)
    {
        return InsertReplayImports(replays.Select(replay => new ReplayImportDto(replay, null)).ToList());
    }

    public async Task InsertReplayImports(List<ReplayImportDto> imports)
    {
        Init();
        var replays = imports.Select(import => import.Replay).ToList();
        foreach (var replay in replays)
        {
            NormalizeReplayPlayerCompatHashes(replay);
        }

        Dictionary<string, (int Duration, SpawnPlaybackEncodedSidecar Sidecar)> sidecarCandidates = [];
        foreach (var import in imports)
        {
            if (import.SpawnPlayback is null)
            {
                continue;
            }
            if (import.SpawnPlayback.UnitCount <= 0
                || !SpawnPlaybackEligibility.IsEligible(import.Replay.Players.Count, import.Replay.Duration))
            {
                continue;
            }

            var replayHash = import.Replay.ComputeHash();
            if (!sidecarCandidates.TryGetValue(replayHash, out var existing)
                || import.Replay.Duration >= existing.Duration)
            {
                sidecarCandidates[replayHash] = (import.Replay.Duration, import.SpawnPlayback);
            }
        }
        Dictionary<string, SpawnPlaybackEncodedSidecar> sidecarsByReplayHash = sidecarCandidates
            .ToDictionary(k => k.Key, v => v.Value.Sidecar);

        Dictionary<ToonIdRec, string> players = [];
        foreach (var player in replays.SelectMany(s => s.Players))
        {
            var key = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
            players[key] = player.Name;
        }

        await CreatePlayerIds(players);

        await using var context = await contextFactory.CreateDbContextAsync();
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

            var replayHash = replay.ComputeHash();
            var dbReplay = replay.ToEntity(sidecarsByReplayHash.ContainsKey(replayHash));

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

        var duplicateResult = await HandleDuplicates(dbReplays, context, sidecarsByReplayHash);

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
                await ratingService.PreRatings(replayCalcDtos);
            }
        }

        if (duplicateResult.SidecarsToSave.Count > 0)
        {
            await SaveSpawnPlaybackSidecars(duplicateResult.SidecarsToSave, context);
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
            }

            if (playersToInsert.Count == 0)
            {
                return;
            }

            await using var context = await contextFactory.CreateDbContextAsync();

            // Insert new players
            if (playersToInsert.Count > 0)
            {
                context.Players.AddRange(playersToInsert);
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
        }
        finally
        {
            playerSs.Release();
        }
    }

    private static void NormalizeReplayPlayerCompatHashes(ReplayDto replay)
    {
        foreach (var player in replay.Players)
        {
            player.CompatHash = NormalizeReplayPlayerCompatHash(player.CompatHash);
        }
    }

    private static string? NormalizeReplayPlayerCompatHash(string? compatHash)
    {
        if (string.IsNullOrEmpty(compatHash) || IsSha256Hex(compatHash))
        {
            return compatHash;
        }

        if (compatHash.StartsWith("ds-player-compat", StringComparison.OrdinalIgnoreCase)
            || compatHash.Length > 64)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compatHash)));
        }

        return compatHash;
    }

    private static bool IsSha256Hex(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}

