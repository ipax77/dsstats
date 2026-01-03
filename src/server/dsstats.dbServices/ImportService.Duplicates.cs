
using dsstats.db;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class ImportService
{
    private static async Task<DuplicateResult> HandleDuplicates(List<Replay> replays, DsstatsContext context)
    {
        var dedupedReplays = replays
            .GroupBy(r => r.ReplayHash)
            .Select(g => g.OrderByDescending(r => r.Duration).First())
            .ToList();

        var importReplayHashes = dedupedReplays.Select(s => s.ReplayHash).ToHashSet();
        var dbReplays = await context.Replays
            .Where(x => importReplayHashes.Contains(x.ReplayHash))
            .ToDictionaryAsync(k => k.ReplayHash, v => v);

        DuplicateResult result = new();

        foreach (var replay in dedupedReplays)
        {
            if (dbReplays.TryGetValue(replay.ReplayHash, out var dbReplay))
            {
                if (replay.Duration > dbReplay.Duration)
                {
                    await DeleteReplay(dbReplay.ReplayHash, context);
                    result.ReplaysToImport.Add(replay);
                    result.Replaced++;
                }
                else
                {
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