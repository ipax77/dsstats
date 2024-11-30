using dsstats.db8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services.Import;

public partial class ImportService
{
    private async Task<int> HandleDuplicates(List<Replay> replays, ReplayContext context)
    {
        var replayHashes = replays.Select(s => s.ReplayHash).ToList();
        var dupReplays = await context.Replays
            .Include(i => i.ReplayPlayers)
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .ToListAsync();

        var lastSpawnHashes = replays.SelectMany(s => s.ReplayPlayers)
            .Where(x => !string.IsNullOrEmpty(x.LastSpawnHash))
            .Select(s => s.LastSpawnHash ?? "xxx")
            .ToList();

        List<Replay> lsDupReplays = [];
        if (!IsMaui)
        {
            var lsDupReplaysQuery = context.Replays
                .Include(i => i.ReplayPlayers)
                .Where(x => !replayHashes.Contains(x.ReplayHash));

            var lsDupReplaysQuery2 = from r in lsDupReplaysQuery
                                     from rp in r.ReplayPlayers
                                     where rp.LastSpawnHash != null && lastSpawnHashes.Contains(rp.LastSpawnHash)
                                     select r;

            lsDupReplays = await lsDupReplaysQuery2
                .Distinct()
                .ToListAsync();

            dupReplays.AddRange(lsDupReplays);
        }

        if (dupReplays.Count == 0)
        {
            return 0;
        }

        int dupsHandled = 0;

        foreach (var dbReplay in dupReplays)
        {
            var importReplay = replays
                .FirstOrDefault(f => f.ReplayHash == dbReplay.ReplayHash);

            if (importReplay is null && lsDupReplays.Count > 0)
            {
                var repLastSpawnHashes = dbReplay.ReplayPlayers
                    .Where(x => x.LastSpawnHash != null)
                    .Select(s => s.LastSpawnHash)
                    .ToList();

                var query = from r in replays
                            from rp in r.ReplayPlayers
                            where !string.IsNullOrEmpty(rp.LastSpawnHash) && repLastSpawnHashes.Contains(rp.LastSpawnHash)
                            select r;

                importReplay = query.FirstOrDefault();
            }

            ArgumentNullException.ThrowIfNull(importReplay);

            if (await HandleDuplicate(dbReplay, importReplay, context))
            {
                replays.Remove(importReplay);
                dupsHandled++;
            }
        }
        await context.SaveChangesAsync();
        return dupsHandled;
    }

    private async Task<bool> HandleDuplicate(Replay dbReplay, Replay importReplay, ReplayContext context)
    {
        if (!DuplicateIsPlausible(dbReplay, importReplay))
        {
            return true;
        }

        var keepReplay = dbReplay;
        var throwReplay = importReplay;

        if (dbReplay.Duration < importReplay.Duration)
        {
            keepReplay = importReplay;
            throwReplay = dbReplay;
            logger.LogWarning("deleting replay due to lastSpawnHash: {hash1} for {hash2}", throwReplay.ReplayHash, keepReplay.ReplayHash);
            await DeleteReplay(dbReplay.ReplayHash, context);
        }

        foreach (var rp in throwReplay.ReplayPlayers)
        {
            if (rp.IsUploader)
            {
                var dbRp = keepReplay.ReplayPlayers
                    .FirstOrDefault(f => f.PlayerId == rp.PlayerId);
                if (dbRp is not null)
                {
                    dbRp.IsUploader = true;
                }
            }
        }
        return keepReplay == dbReplay;
    }

    private bool DuplicateIsPlausible(Replay dbReplay, Replay dupReplay)
    {
        if ((dupReplay.GameTime - dbReplay.GameTime).Duration() > TimeSpan.FromDays(3))
        {
            return false;
        }
        return true;
    }

    private async Task DeleteReplay(string replayHash, ReplayContext context)
    {
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replay = await context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayRatingInfo)
                    .ThenInclude(i => i.RepPlayerRatings)
                .Include(i => i.ComboReplayRating)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.ComboReplayPlayerRating)
                .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (replay is not null)
            {
                context.Replays.Remove(replay);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}
