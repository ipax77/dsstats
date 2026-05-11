
using dsstats.db;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class ImportService
{
    private const double ParserCompatDuplicateWindowMinutes = 3.0;

    private static async Task<DuplicateResult> HandleDuplicates(List<Replay> replays, DsstatsContext context)
    {
        DuplicateResult result = new();
        var dedupedReplays = DeduplicateIncomingReplays(replays, result);
        var dbReplays = await GetExistingDuplicateCandidates(dedupedReplays, context);

        foreach (var replay in dedupedReplays)
        {
            var dbDuplicates = dbReplays
                .Where(dbReplay => IsDuplicate(replay, dbReplay))
                .ToList();

            if (dbDuplicates.Count == 0)
            {
                result.ReplaysToImport.Add(replay);
                continue;
            }

            await ResolveDuplicateGroup(replay, dbDuplicates, dbReplays, context, result);
        }

        return result;
    }

    private static List<Replay> DeduplicateIncomingReplays(List<Replay> replays, DuplicateResult result)
    {
        var exactDeduped = new List<Replay>();
        foreach (var group in replays.GroupBy(r => r.ReplayHash))
        {
            var keeper = GetBestReplay(group);
            foreach (var duplicate in group.Where(r => !ReferenceEquals(r, keeper)))
            {
                if (SetUploaders(keeper, duplicate))
                {
                    result.UploaderUpdates++;
                }
                result.Duplicates++;
            }
            exactDeduped.Add(keeper);
        }

        var parserDeduped = new List<Replay>();
        foreach (var replay in exactDeduped.OrderBy(r => r.Gametime))
        {
            if (string.IsNullOrEmpty(replay.ParserCompatHash))
            {
                parserDeduped.Add(replay);
                continue;
            }

            var match = parserDeduped.FirstOrDefault(candidate => IsParserCompatDuplicate(replay, candidate));
            if (match is null)
            {
                parserDeduped.Add(replay);
                continue;
            }

            var keeper = GetBestReplay([match, replay]);
            var duplicate = ReferenceEquals(keeper, match) ? replay : match;
            if (SetUploaders(keeper, duplicate))
            {
                result.UploaderUpdates++;
            }

            if (!ReferenceEquals(keeper, match))
            {
                var index = parserDeduped.IndexOf(match);
                parserDeduped[index] = keeper;
            }

            result.Duplicates++;
        }

        return parserDeduped;
    }

    private static async Task<List<Replay>> GetExistingDuplicateCandidates(List<Replay> replays, DsstatsContext context)
    {
        var importReplayHashes = replays.Select(s => s.ReplayHash).ToHashSet();
        var importParserCompatHashes = replays
            .Where(x => !string.IsNullOrEmpty(x.ParserCompatHash))
            .Select(s => s.ParserCompatHash!)
            .ToHashSet();

        DateTime? fromTime = null;
        DateTime? toTime = null;
        if (importParserCompatHashes.Count > 0)
        {
            fromTime = replays
                .Where(x => !string.IsNullOrEmpty(x.ParserCompatHash))
                .Min(x => x.Gametime)
                .AddMinutes(-ParserCompatDuplicateWindowMinutes);
            toTime = replays
                .Where(x => !string.IsNullOrEmpty(x.ParserCompatHash))
                .Max(x => x.Gametime)
                .AddMinutes(ParserCompatDuplicateWindowMinutes);
        }

        return await context.Replays
            .Include(i => i.Players)
            .Where(x =>
                importReplayHashes.Contains(x.ReplayHash)
                || (fromTime != null
                    && toTime != null
                    && x.ParserCompatHash != null
                    && importParserCompatHashes.Contains(x.ParserCompatHash)
                    && x.Gametime >= fromTime
                    && x.Gametime <= toTime))
            .ToListAsync();
    }

    private static async Task ResolveDuplicateGroup(
        Replay importReplay,
        List<Replay> dbDuplicates,
        List<Replay> dbReplays,
        DsstatsContext context,
        DuplicateResult result)
    {
        var group = dbDuplicates.Append(importReplay).ToList();
        var keeper = GetBestReplay(group);

        foreach (var duplicate in group.Where(r => !ReferenceEquals(r, keeper)))
        {
            if (SetUploaders(keeper, duplicate))
            {
                result.UploaderUpdates++;
            }
        }

        if (ReferenceEquals(keeper, importReplay))
        {
            foreach (var duplicate in dbDuplicates)
            {
                await DeleteReplay(duplicate.ReplayHash, context);
                dbReplays.Remove(duplicate);
                result.Replaced++;
            }
            result.ReplaysToImport.Add(importReplay);
            return;
        }

        foreach (var duplicate in dbDuplicates.Where(r => !ReferenceEquals(r, keeper)).ToList())
        {
            await DeleteReplay(duplicate.ReplayHash, context);
            dbReplays.Remove(duplicate);
            result.Replaced++;
        }

        result.Duplicates++;
    }

    private static Replay GetBestReplay(IEnumerable<Replay> replays)
    {
        return replays
            .OrderByDescending(r => r.Duration)
            .ThenByDescending(r => r.Version.StartsWith("5.", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(r => r.ReplayId)
            .First();
    }

    private static bool IsDuplicate(Replay left, Replay right)
    {
        return left.ReplayHash == right.ReplayHash || IsParserCompatDuplicate(left, right);
    }

    private static bool IsParserCompatDuplicate(Replay left, Replay right)
    {
        return !string.IsNullOrEmpty(left.ParserCompatHash)
            && left.ParserCompatHash == right.ParserCompatHash
            && Math.Abs((left.Gametime - right.Gametime).TotalMinutes) <= ParserCompatDuplicateWindowMinutes;
    }

    private static bool SetUploaders(Replay keepReplay, Replay dupReplay)
    {
        var dupPlayers = dupReplay.Players.Where(x => x.IsUploader).ToList();
        if (dupPlayers.Count == 0)
        {
            return false;
        }

        var updated = false;
        foreach (var replayPlayer in keepReplay.Players.Where(x => !x.IsUploader))
        {
            var dupPlayer = dupPlayers.FirstOrDefault(f =>
                !string.IsNullOrEmpty(f.CompatHash)
                && f.CompatHash == replayPlayer.CompatHash);

            dupPlayer ??= dupPlayers.FirstOrDefault(f => f.PlayerId == replayPlayer.PlayerId);

            if (dupPlayer is not null)
            {
                replayPlayer.IsUploader = true;
                updated = true;
            }
        }
        return updated;
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
    public int UploaderUpdates { get; set; }
    public List<Replay> ReplaysToImport { get; set; } = [];
}
