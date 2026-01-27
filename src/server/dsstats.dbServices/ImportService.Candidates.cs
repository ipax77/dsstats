
using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ImportService
{
    public async Task CheckDuplicateCandidates()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var computed = await ComputeMissingCandidateHashes(context);
        DateTime fromTime = computed ? DateTime.MinValue : DateTime.UtcNow.AddHours(-25);

        var duplicateHashes = await GetDuplicateCandidateHashes(context, fromTime);
        logger.LogWarning("Found {count} candidate duplicate hashes.", duplicateHashes.Count);

        foreach (var hash in duplicateHashes)
        {
            var replays = await GetMinimalReplays(hash, context);
            await HandleCandidateDuplicates(replays, context);
        }
    }

    public async Task CheckRealmDuplicateCandidates()
    {
        int startYear = 2018;
        int currentYear = DateTime.UtcNow.Year;
        int countTotal = 0;
        int deletedTotal = 0;

        for (int i = startYear; i <= currentYear; i++)
        {
            var from = new DateTime(i, 1, 1);
            var to = new DateTime(i, 12, 31);

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            context.Database.SetCommandTimeout(180);


            var results = await context.Set<ReplayIdResult>()
                .FromSqlRaw("CALL FindDuplicateReplayClustersInRange({0}, {1});", from, to)
                .ToListAsync();

            countTotal += results.Count;

            // optional: preload all replays in one shot
            var allIds = results
                .SelectMany(r => r.ReplayIdsCsv.Split(','))
                .Select(int.Parse)
                .Distinct()
                .ToList();

            var allReplays = await GetMinimalReplays(allIds, context);

            foreach (var result in results)
            {
                var replayIds = result.ReplayIdsCsv.Split(',')
                    .Select(int.Parse)
                    .ToList();

                if (replayIds.Count <= 1)
                    continue;

                var replays = allReplays
                    .Where(r => replayIds.Contains(r.ReplayId))
                    .ToList();

                deletedTotal += await HandleCandidateDuplicates(replays, context);
            }
        }
        logger.LogWarning("Found {count} realm-based candidate duplicate clusters.", countTotal);
        logger.LogWarning("Deleted {count} realm-based candidate duplicate replays.", deletedTotal);
    }


    private async Task<int> HandleCandidateDuplicates(List<Replay> replays, DsstatsContext context)
    {
        // Step 1: Cluster by start time within candidate hash
        var matchGroups = new List<List<Replay>>();
        int deletedCount = 0;

        foreach (var replay in replays)
        {
            var match = matchGroups.FirstOrDefault(g =>
                Math.Abs((g[0].Gametime - replay.Gametime).TotalMinutes) <= 1.5);

            if (match == null)
                matchGroups.Add([replay]);
            else
                match.Add(replay);
        }

        // Step 2: Resolve duplicates inside each cluster
        foreach (var group in matchGroups.Where(x => x.Count > 1))
        {
            // Rank candidates
            var keeper = group
                .OrderByDescending(r => r.Duration)
                .ThenByDescending(r => r.Version.StartsWith("5.", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(r => r.ReplayId) // optional deterministic fallback
                .First();

            foreach (var duplicate in group.Where(r => r != keeper))
            {
                SetUploaders(keeper, duplicate);
                logger.LogInformation("Duplicate candidate: Keeper {keeperId}, Duplicate {dupId}", keeper.ReplayId, duplicate.ReplayId);
                await DeleteReplay(duplicate.ReplayHash, context);
                deletedCount++;
            }

            await context.SaveChangesAsync();
        }
        return deletedCount;
    }

    private static async Task<List<string>> GetDuplicateCandidateHashes(DsstatsContext context, DateTime fromTime)
    {
        if (fromTime == DateTime.MinValue)
        {
            List<string> duplicateHashes = await context.Replays
                .OrderBy(o => o.Gametime)
                .Where(x => !string.IsNullOrEmpty(x.CompatHash))
                .GroupBy(x => x.CompatHash)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key!)
                .ToListAsync();
            return duplicateHashes;
        }
        else
        {
            var compatHashes = await context.Replays
                .Where(x => x.Imported >= fromTime)
                .Select(s => s.CompatHash)
                .ToListAsync();
            var compatHashesHashSet = compatHashes.ToHashSet();

            List<string> duplicateHashes = await context.Replays
                .OrderBy(o => o.Gametime)
                .Where(x => compatHashesHashSet.Contains(x.CompatHash))
                .GroupBy(x => x.CompatHash)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key!)
                .ToListAsync();
            return duplicateHashes;
        }
    }

    private async Task<bool> ComputeMissingCandidateHashes(DsstatsContext context)
    {
        var count = await context.Replays
            .Where(x => string.IsNullOrEmpty(x.CompatHash))
            .CountAsync();

        if (count == 0)
        {
            return false;
        }
        var batchSize = 1000;
        int skip = 0;

        while (true)
        {
            var replays = await context.Replays
            .OrderBy(o => o.ReplayId)
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .Select(s => new Replay()
            {
                ReplayId = s.ReplayId,
                GameMode = s.GameMode,
                RegionId = s.RegionId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayer()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
                    Name = t.Name,
                    IsUploader = t.IsUploader,
                    Race = t.Race,
                    Player = new Player()
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
            .Skip(skip)
            .Take(batchSize)
            .ToListAsync();

            if (replays.Count == 0)
            {
                break;
            }

            foreach (var replay in replays)
            {
                var replayDto = GetMinimalReplayDto(replay);
                var compatHash = replayDto.ComputeCandidateHash();
                await context.Replays
                    .Where(r => r.ReplayId == replay.ReplayId)
                    .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.CompatHash, compatHash));
            }

            skip += batchSize;
            logger.LogInformation("Computed candidate hashes for {count}/{total} replays.", skip, count);
        }
        return true;
    }

    private static async Task<List<Replay>> GetMinimalReplays(string compatHash, DsstatsContext context)
    {
        return await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .Where(x => x.CompatHash == compatHash)
            .Select(s => new Replay()
            {
                ReplayId = s.ReplayId,
                Version = s.Version,
                ReplayHash = s.ReplayHash,
                GameMode = s.GameMode,
                RegionId = s.RegionId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayer()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
                    Name = t.Name,
                    IsUploader = t.IsUploader,
                    Race = t.Race,
                    Player = new Player()
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
            .ToListAsync();
    }

    private static async Task<List<Replay>> GetMinimalReplays(List<int> replayIds, DsstatsContext context)
    {
        return await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .Where(x => replayIds.Contains(x.ReplayId))
            .Select(s => new Replay()
            {
                ReplayId = s.ReplayId,
                ReplayHash = s.ReplayHash,
                GameMode = s.GameMode,
                RegionId = s.RegionId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayer()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
                    Name = t.Name,
                    IsUploader = t.IsUploader,
                    Race = t.Race,
                    Player = new Player()
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
            .ToListAsync();
    }

    private static ReplayDto GetMinimalReplayDto(Replay replay)
    {
        return new ReplayDto()
        {
            GameMode = replay.GameMode,
            RegionId = replay.RegionId,
            Gametime = replay.Gametime,
            Duration = replay.Duration,
            Players = replay.Players.Select(t => new ReplayPlayerDto()
            {
                GamePos = t.GamePos,
                Name = t.Name,
                Race = t.Race,
                IsUploader = t.IsUploader,
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
        };
    }
}


