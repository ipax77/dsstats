using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.Services;

public class TransitionService(DsstatsContext context,
                               ILogger<TransitionService> logger)
{
    public async Task FixHashes()
    {
        var replayHashes = await context.Replays
            .OrderBy(o => o.Gametime)
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
                logger.LogWarning($"{progress}/{replayHashes.Count} ({diff}/{deleted})");
            }
            var minimalReplay = await GetMinimalReplay(replayHash, context);
            if (minimalReplay is null)
            {
                continue;
            }
            var computedHash = GetMinimalReplayDto(minimalReplay).ComputeHash();
            if (!computedHash.Equals(replayHash))
            {
                diff++;
                var existing = await GetMinimalReplay(computedHash, context);
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
                        await SetUploader(minimalReplay, existing, context);
                        await DeleteReplay(computedHash, context);
                        await context.Replays
                        .Where(x => x.ReplayHash == replayHash)
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.ReplayHash, computedHash));
                    }
                    else
                    {
                        await SetUploader(existing, minimalReplay, context);
                        await DeleteReplay(replayHash, context);
                    }
                    deleted++;
                }
            }
        }
        logger.LogWarning($"Hashes fixed: {diff}, Deleted: {deleted}");
    }

    private static async Task SetUploader(Replay keepReplay, Replay deleteReplay, DsstatsContext context)
    {
        foreach (var player in keepReplay.Players.Where(x => !x.IsUploader))
        {
            var deletePlayers = deleteReplay.Players.Where(x => x.IsUploader).ToList();
            if (deletePlayers.Count == 0)
            {
                return;
            }
            var deletePlayer = deletePlayers.FirstOrDefault(f =>
                   f.Player!.ToonId.Id == player.Player!.ToonId.Id
                && f.Player.ToonId.Region == player.Player.ToonId.Region
                && f.Player.ToonId.Realm == player.Player.ToonId.Realm);
            if (deletePlayer != null)
            {
                player.IsUploader = true;
                await context.ReplayPlayers
                    .Where(x => x.ReplayPlayerId == player.ReplayPlayerId)
                    .ExecuteUpdateAsync(e => e.SetProperty(p => p.IsUploader, true));
            }
        }
    }

    private static async Task<Replay?> GetMinimalReplay(string replayHash, DsstatsContext context)
    {
        return await context.Replays
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => new Replay()
            {
                ReplayId = s.ReplayId,
                Version = s.Version,
                Gametime = s.Gametime,
                Duration = s.Duration,
                Players = s.Players.Select(t => new ReplayPlayer()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
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
            .FirstOrDefaultAsync();
    }

    private static ReplayDto GetMinimalReplayDto(Replay replay)
    {
        return new()
        {
            Title = replay.Title,
            Version = replay.Version,
            Gametime = replay.Gametime,
            Duration = replay.Duration,
            Players = replay.Players.Select(t => new ReplayPlayerDto()
            {
                GamePos = t.GamePos,
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