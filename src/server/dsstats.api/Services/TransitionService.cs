using dsstats.db;
using dsstats.db.Old;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace dsstats.api.Services;

public class TransitionService(DsstatsContext context,
                               OldReplayContext oldContext,
                               IImportService importService,
                               IRatingService ratingService,
                               ILogger<TransitionService> logger)
{
    private const int ReplayChunkSize = 1_000;
    private const int ArcadeReplayChunkSize = 10_000;

    public async Task ImportReplays()
    {
        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting replay import process");

        var latestImport = context.Replays
            .OrderByDescending(r => r.Imported)
            .Select(r => r.Imported)
            .FirstOrDefault();
        latestImport = latestImport.AddHours(-3);

        var latestArcadeImport = context.ArcadeReplays
            .OrderByDescending(r => r.Imported)
            .Select(r => r.Imported)
            .FirstOrDefault();
        latestArcadeImport = latestArcadeImport.AddHours(-3);

        // Import regular replays in chunks of 1,000
        var regularCount = await ImportReplayChunks(latestImport);

        // Import arcade replays in chunks of 10,000
        var arcadeCount = await ImportArcadeReplayChunks(latestArcadeImport);

        // await ratingService.FindSc2ArcadeMatches(); // todo: enable when needed
        await ratingService.CreateRatings();

        totalStopwatch.Stop();
        logger.LogInformation(
            "Replay import completed. Regular replays: {RegularCount}, Arcade replays: {ArcadeCount}, Total time: {ElapsedTime}",
            regularCount, arcadeCount, totalStopwatch.Elapsed);
    }

    private async Task<int> ImportReplayChunks(DateTime latestImported)
    {
        var stopwatch = Stopwatch.StartNew();
        var totalCount = 0;
        var skip = 0;

        while (true)
        {
            var oldReplays = await GetOldReplays(latestImported)
                .Skip(skip)
                .Take(ReplayChunkSize)
                .ToListAsync();

            if (!oldReplays.Any())
                break;

            var dtos = oldReplays.Select(s => s.ToV3Dto()).ToList();
            await importService.InsertReplays(dtos);

            totalCount += oldReplays.Count;
            skip += ReplayChunkSize;

            logger.LogInformation(
                "Imported {Count} regular replays (total: {TotalCount})",
                oldReplays.Count, totalCount);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Regular replay import completed. Total: {TotalCount}, Time: {ElapsedTime}",
            totalCount, stopwatch.Elapsed);

        return totalCount;
    }

    private async Task<int> ImportArcadeReplayChunks(DateTime latestImported)
    {
        var stopwatch = Stopwatch.StartNew();
        var totalCount = 0;
        var skip = 0;

        while (true)
        {
            var oldArcadeReplays = await GetOldArcadeReplays(latestImported)
                .Skip(skip)
                .Take(ArcadeReplayChunkSize)
                .ToListAsync();

            if (!oldArcadeReplays.Any())
                break;

            await importService.ImportArcadeReplays(oldArcadeReplays);

            totalCount += oldArcadeReplays.Count;
            skip += ArcadeReplayChunkSize;

            logger.LogInformation(
                "Imported {Count} arcade replays (total: {TotalCount})",
                oldArcadeReplays.Count, totalCount);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Arcade replay import completed. Total: {TotalCount}, Time: {ElapsedTime}",
            totalCount, stopwatch.Elapsed);

        return totalCount;
    }

    private IQueryable<ArcadeReplayDto> GetOldArcadeReplays(DateTime latestImported)
    {
        return oldContext.ArcadeReplays
            .Where(x => x.Imported > latestImported)
            .OrderBy(o => o.ArcadeReplayId)
            .Select(s => new ArcadeReplayDto()
            {
                RegionId = s.RegionId,
                BnetBucketId = s.BnetBucketId,
                BnetRecordId = s.BnetRecordId,
                GameMode = s.GameMode,
                CreatedAt = s.CreatedAt,
                Duration = s.Duration,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.ArcadeReplayDsPlayers.Select(s => new ArcadeReplayPlayerDto()
                {
                    SlotNumber = s.SlotNumber,
                    Team = s.Team,
                    Player = new()
                    {
                        Name = s.Name,
                        ToonId = new()
                        {
                            Region = s.Player!.RegionId,
                            Realm = s.Player!.RealmId,
                            Id = s.Player!.ToonId
                        }
                    }
                }).ToList()
            });
    }

    private IQueryable<ReplayV2Dto> GetOldReplays(DateTime latestImported)
    {
        return oldContext.Replays
            .Where(x => x.Imported > latestImported)
            .OrderBy(o => o.ReplayId)
            .Select(s => new ReplayV2Dto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                Bunker = s.Bunker,
                Cannon = s.Cannon,
                Maxkillsum = s.Maxkillsum,
                Playercount = s.Playercount,
                Middle = s.Middle,
                TournamentEdition = s.TournamentEdition,
                CompatHash = s.ReplayHash,
                ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerV2Dto()
                {
                    Name = t.Name,
                    Clan = t.Clan,
                    GamePos = t.GamePos,
                    Team = t.Team,
                    PlayerResult = t.PlayerResult,
                    Duration = t.Duration,
                    Race = t.Race,
                    APM = t.APM,
                    Kills = t.Kills,
                    TierUpgrades = t.TierUpgrades,
                    Refineries = t.Refineries,
                    Player = new PlayerV2Dto()
                    {
                        Name = t.Player.Name,
                        RealmId = t.Player.RealmId,
                        RegionId = t.Player.RegionId,
                        ToonId = t.Player.ToonId,
                    },
                    Upgrades = t.Upgrades.Select(u => new PlayerUpgradeV2Dto()
                    {
                        Gameloop = u.Gameloop,
                        Upgrade = new UpgradeV2Dto()
                        {
                            Name = u.Upgrade!.Name
                        }
                    }).ToList(),
                    Spawns = t.Spawns.Select(v => new SpawnV2Dto()
                    {
                        Gameloop = v.Gameloop,
                        Breakpoint = v.Breakpoint,
                        Income = v.Income,
                        GasCount = v.GasCount,
                        ArmyValue = v.ArmyValue,
                        KilledValue = v.KilledValue,
                        UpgradeSpent = v.UpgradeSpent,
                        Units = v.Units.Select(w => new SpawnUnitV2Dto()
                        {
                            Count = w.Count,
                            Poss = w.Poss,
                            Unit = new UnitV2Dto()
                            {
                                Name = w.Unit!.Name
                            }
                        }).ToList()
                    }).ToList()
                }).ToList()
            });
    }

    public async Task FixHashes()
    {
        var replayHashes = await context.Replays.OrderBy(o => o.Gametime)
            // .Where(x => x.Gametime >= new DateTime(2024, 2, 6))
            .Select(s => s.ReplayHash)
            .ToListAsync();

        int diff = 0;
        int deleted = 0;
        int progress = 0;
        foreach (var replayHash in replayHashes)
        {
            progress++;
            if (progress % 100 == 0)
            {
                logger.LogInformation($"{progress}/{replayHashes.Count} ({diff}/{deleted})");
            }
            var minimalReplay = await GetMinimalReplayDto(replayHash, context);
            if (minimalReplay is null)
            {
                continue;
            }
            var computedHash = minimalReplay.ComputeHash();
            if (!computedHash.Equals(replayHash))
            {
                diff++;
                var existing = await GetMinimalReplayDto(computedHash, context);
                if (existing is null)
                {
                    // change hash
                    await context.Replays
                        .Where(x => x.ReplayHash == replayHash)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.ReplayHash, computedHash));
                }
                else
                {
                    if ((existing.Duration < minimalReplay.Duration)
                        || (existing.Version == "v2" && minimalReplay.Version != "v2"))
                    {
                        await DeleteReplay(computedHash, context);
                        await context.Replays
                        .Where(x => x.ReplayHash == replayHash)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.ReplayHash, computedHash));
                    }
                    else
                    {
                        await DeleteReplay(replayHash, context);
                    }
                    deleted++;
                }
            }
        }
        logger.LogWarning($"Hashes fixed: {diff}, Deletel: {deleted}");
    }

    private static async Task<ReplayDto?> GetMinimalReplayDto(string replayHash, DsstatsContext context)
    {
        return await context.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => new ReplayDto()
            {
                Title = s.Title,
                Version = s.Version,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayerDto()
                {
                    GamePos = t.GamePos,
                    Player = new PlayerDto()
                    {
                        ToonId = new()
                        {
                            Id = t.Player!.ToonId.Id,
                            Realm = t.Player!.ToonId.Realm,
                            Region = t.Player!.ToonId.Region
                        }
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    private static async Task DeleteReplay(string replayHash, DsstatsContext context)
    {
        var replay = await context.Replays
            .Include(i => i.Ratings)
            .Include(i => i.Players)
                .ThenInclude(i => i.Ratings)
            .Include(i => i.Players)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.Players)
                .ThenInclude(i => i.Upgrades)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);
        if (replay is null)
        {
            return;
        }
        context.Replays.Remove(replay);
        await context.SaveChangesAsync();
    }
}