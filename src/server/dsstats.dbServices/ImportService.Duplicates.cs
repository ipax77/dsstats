
using dsstats.db;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class ImportService
{
    private static async Task<DuplicateResult> HandleDuplicates(List<Replay> replays, DsstatsContext context)
    {
        var dedupedReplays = replays
            .GroupBy(r => r.ReplayHash)
            .Select(g =>
            {
                var keeper = g.OrderByDescending(r => r.Duration).First();
                // Merge IsUploader info from all duplicates in this batch
                foreach (var duplicate in g.Where(x => x != keeper))
                {
                    SetUploaders(keeper, duplicate);
                }
                return keeper;
            })
            .ToList();

        var importReplayHashes = dedupedReplays.Select(s => s.ReplayHash).ToHashSet();
        var dbReplays = await context.Replays
            .Include(i => i.Players)
            .Where(x => importReplayHashes.Contains(x.ReplayHash))
            .ToDictionaryAsync(k => k.ReplayHash, v => v);

        DuplicateResult result = new();

        foreach (var replay in dedupedReplays)
        {
            if (dbReplays.TryGetValue(replay.ReplayHash, out var dbReplay))
            {
                if (replay.Duration > dbReplay.Duration)
                {
                    SetUploaders(replay, dbReplay);
                    await DeleteReplay(dbReplay.ReplayHash, context);
                    result.ReplaysToImport.Add(replay);
                    result.Replaced++;
                }
                else
                {
                    SetUploaders(dbReplay, replay);
                    result.Duplicates++;
                }
            }
            else
            {
                result.ReplaysToImport.Add(replay);
            }
        }

        return result;
    }

    private static void SetUploaders(Replay keepReplay, Replay dupReplay)
    {
        var dupPlayers = dupReplay.Players.Where(x => x.IsUploader).ToList();
        if (dupPlayers.Count == 0)
        {
            return;
        }

        foreach (var replayPlayer in keepReplay.Players.Where(x => !x.IsUploader))
        {
            var dupPlayer = dupPlayers.FirstOrDefault(f => f.PlayerId == replayPlayer.PlayerId);
            if (dupPlayer is not null)
            {
                replayPlayer.IsUploader = true;
            }
        }
    }

    private static async Task DeleteReplay(string replayHash, DsstatsContext context)
    {
        try
        {
            var replay = await context.Replays
                .Include(i => i.Players)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.Ratings)
                    .ThenInclude(i => i.ReplayPlayerRatings)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Ratings)
                .Include(i => i.Players)
                    .ThenInclude(i => i.Upgrades)
                .FirstOrDefaultAsync(f => f.ReplayHash == replayHash);

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

internal sealed class DuplicateResult
{
    public int Duplicates { get; set; }
    public int Replaced { get; set; }
    public List<Replay> ReplaysToImport { get; set; } = [];
}