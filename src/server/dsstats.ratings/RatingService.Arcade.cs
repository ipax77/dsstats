using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

public partial class RatingService
{
    private Dictionary<ReplayMatchDto, string> _arcadeCache = [];
    private DateTime _arcadeCacheStart = DateTime.MinValue;
    private DateTime _arcadeCacheEnd = DateTime.MinValue;


    private const int ArcadeChunkSize = 25_000;
    private const int take = 5000;
    private static readonly TimeSpan ArcadeOffset = TimeSpan.FromHours(12);


    public async Task FindSc2ArcadeMatches()
    {

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        DateTime startDate = new DateTime(2021, 2, 1);
        Dictionary<int, int> matchesDict = [];

        int totalDsstatsReplays = 0;
        bool shouldBreak = false;

        var existingMatches = await context.ReplayArcadeMatches
            .ToDictionaryAsync(k => k.ReplayId, v => v.ArcadeReplayId);
        HashSet<int> matches = [];

        while (true)
        {
            var dsstatsReplays = await GetDsstatsChunk(startDate, take, context, false);
            if (dsstatsReplays.Count == 0)
            {
                break;
            }
            else
            {
                logger.LogInformation("dsstats chunk: {count}, {date}", dsstatsReplays.Count, startDate.ToShortDateString());
            }
            totalDsstatsReplays += dsstatsReplays.Count;

            var dsKeys = dsstatsReplays.ToDictionary(r => r, r => GetOrderedToonKey(r));

            foreach (var dsstatsReplay in dsstatsReplays)
            {
                var dsKey = dsKeys[dsstatsReplay];
                var arcadeReplays = await GetArcadeChunkCached(dsstatsReplay.Gametime, context, true);
                if (arcadeReplays.Count == 0)
                {
                    shouldBreak = true;
                    break;
                }
                var reasonableReplays = arcadeReplays
                    .Where(r => r.Value == dsKey
                        && !matches.Contains(r.Key.ReplayId))
                    .Select(s => s.Key);
                if (!reasonableReplays.Any())
                {
                    continue;
                }
                var match = reasonableReplays.MaxBy(o => GetMatchScore(dsstatsReplay, o));
                if (match != null)
                {
                    matches.Add(match.ReplayId);
                    matchesDict[dsstatsReplay.ReplayId] = match.ReplayId;
                }
            }
            var lastReplay = dsstatsReplays.Last();
            startDate = lastReplay.Gametime;

            if (shouldBreak)
            {
                break;
            }
            await SaveMatches(matchesDict, existingMatches, context);
            logger.LogWarning("Arcade matches found {matches}/{total} {per}",
    matchesDict.Count, totalDsstatsReplays, ((matchesDict.Count == 0 || totalDsstatsReplays == 0) ? 0 : (double)matchesDict.Count / totalDsstatsReplays).ToString("P2"));
            matchesDict.Clear();
        }

        _arcadeCache.Clear();
        _arcadeCacheStart = DateTime.MinValue;
        _arcadeCacheEnd = DateTime.MinValue;

        await SaveMatches(matchesDict, existingMatches, context);
        logger.LogWarning("Arcade matches found {matches}/{total} {per}",
            matchesDict.Count, totalDsstatsReplays, (totalDsstatsReplays == 0) ? "0.00%" : ((double)matchesDict.Count / totalDsstatsReplays).ToString("P2"));
    }

    public async Task MatchNewDsstatsReplays(DateTime? dsstatsImportedAfter = null)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        // Default to last check time or last 24 hours
        dsstatsImportedAfter ??= DateTime.UtcNow.AddHours(-24);

        logger.LogInformation("Matching DsstatsReplays imported after {date}", dsstatsImportedAfter);

        // Get unmatched dsstats replays imported recently
        var newDsstatsReplays = await context.Replays
            .Where(r => r.Imported > dsstatsImportedAfter)
            .Where(r => !context.ReplayArcadeMatches.Any(m => m.ReplayId == r.ReplayId))
            .OrderBy(r => r.Gametime)
            .Select(s => new ReplayMatchDto()
            {
                ReplayId = s.ReplayId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                GameMode = s.GameMode,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.Players.Select(x => new PlayerMatchDto()
                {
                    ReplayPlayerId = x.ReplayPlayerId,
                    Team = x.TeamId,
                    ToonId = new()
                    {
                        Region = x.Player!.ToonId.Region,
                        Realm = x.Player.ToonId.Realm,
                        Id = x.Player.ToonId.Id,
                    }
                }).ToList(),
            })
            .ToListAsync();

        if (newDsstatsReplays.Count == 0)
        {
            logger.LogInformation("No new unmatched DsstatsReplays found");
            return;
        }

        logger.LogInformation("Found {count} new DsstatsReplays to match", newDsstatsReplays.Count);

        await ProcessReplaysForMatching(newDsstatsReplays, context, "new DsstatsReplays");
    }

    public async Task MatchWithNewArcadeReplays(DateTime? arcadeImportedAfter = null)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        // Default to last check time or last 24 hours
        arcadeImportedAfter ??= DateTime.UtcNow.AddHours(-24);

        logger.LogInformation("Re-matching using ArcadeReplays imported after {date}", arcadeImportedAfter);

        // Find the time range of newly imported arcade replays
        var newArcadeReplayTimeRange = await context.ArcadeReplays
            .Where(ar => ar.Imported > arcadeImportedAfter)
            .GroupBy(ar => 1)
            .Select(g => new
            {
                MinGametime = g.Min(ar => ar.CreatedAt),
                MaxGametime = g.Max(ar => ar.CreatedAt),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        if (newArcadeReplayTimeRange == null || newArcadeReplayTimeRange.Count == 0)
        {
            logger.LogInformation("No new ArcadeReplays found");
            return;
        }

        logger.LogInformation("Found {count} new ArcadeReplays in time range {start} to {end}",
            newArcadeReplayTimeRange.Count,
            newArcadeReplayTimeRange.MinGametime.ToShortDateString(),
            newArcadeReplayTimeRange.MaxGametime.ToShortDateString());

        // Get unmatched dsstats replays in the same time range (with buffer)
        var bufferDays = 1; // ArcadeReplays can appear +/- a day from actual game time
        var startDate = newArcadeReplayTimeRange.MinGametime.AddDays(-bufferDays);
        var endDate = newArcadeReplayTimeRange.MaxGametime.AddDays(bufferDays);

        var unmatchedDsstatsInRange = await context.Replays
            .Where(r => r.Gametime >= startDate && r.Gametime <= endDate)
            .Where(r => !context.ReplayArcadeMatches.Any(m => m.ReplayId == r.ReplayId))
            .OrderBy(r => r.Gametime)
            .Select(s => new ReplayMatchDto()
            {
                ReplayId = s.ReplayId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                GameMode = s.GameMode,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.Players.Select(x => new PlayerMatchDto()
                {
                    ReplayPlayerId = x.ReplayPlayerId,
                    Team = x.TeamId,
                    ToonId = new()
                    {
                        Region = x.Player!.ToonId.Region,
                        Realm = x.Player.ToonId.Realm,
                        Id = x.Player.ToonId.Id,
                    }
                }).ToList(),
            }).ToListAsync();

        if (unmatchedDsstatsInRange.Count == 0)
        {
            logger.LogInformation("No unmatched DsstatsReplays in the time range of new ArcadeReplays");
            return;
        }

        logger.LogInformation("Re-checking {count} unmatched DsstatsReplays in range",
            unmatchedDsstatsInRange.Count);

        await ProcessReplaysForMatching(unmatchedDsstatsInRange, context, "DsstatsReplays with new ArcadeReplays");
    }

    public async Task ContinueFindSc2ArcadeMatches(DateTime? lastCheckTime = null)
    {
        lastCheckTime ??= DateTime.UtcNow.AddHours(-24);

        logger.LogInformation("Starting incremental arcade match finding since {date}", lastCheckTime);

        // First, match any newly imported DsstatsReplays
        await MatchNewDsstatsReplays(lastCheckTime);

        // Then, re-check old unmatched replays against newly imported ArcadeReplays
        await MatchWithNewArcadeReplays(lastCheckTime);

        logger.LogInformation("Incremental arcade match finding complete");
    }

    private async Task ProcessReplaysForMatching(
        List<ReplayMatchDto> dsstatsReplays,
        DsstatsContext context,
        string operationDescription)
    {
        var existingMatches = await context.ReplayArcadeMatches
            .ToDictionaryAsync(k => k.ReplayId, v => v.ArcadeReplayId);

        Dictionary<int, int> matchesDict = [];
        HashSet<int> usedArcadeReplays = new(existingMatches.Values);
        int matched = 0;

        try
        {
            foreach (var dsstatsReplay in dsstatsReplays)
            {
                var dsKey = GetOrderedToonKey(dsstatsReplay);
                var arcadeReplays = await GetArcadeChunkCached(dsstatsReplay.Gametime, context, true);

                if (arcadeReplays.Count == 0)
                {
                    continue;
                }

                var reasonableReplays = arcadeReplays
                    .Where(r => r.Value == dsKey
                        && !usedArcadeReplays.Contains(r.Key.ReplayId))
                    .Select(s => s.Key);

                if (!reasonableReplays.Any())
                {
                    continue;
                }

                var match = reasonableReplays.MaxBy(o => GetMatchScore(dsstatsReplay, o));
                if (match != null)
                {
                    usedArcadeReplays.Add(match.ReplayId);
                    matchesDict[dsstatsReplay.ReplayId] = match.ReplayId;
                    matched++;

                    // Save in batches of 100
                    if (matchesDict.Count >= 100)
                    {
                        await SaveMatches(matchesDict, existingMatches, context);
                        logger.LogInformation("Saved batch: {matched} matches so far for {desc}",
                            matched, operationDescription);

                        foreach (var kvp in matchesDict)
                        {
                            existingMatches[kvp.Key] = kvp.Value;
                        }
                        matchesDict.Clear();
                    }
                }
            }
        }
        finally
        {
            _arcadeCache.Clear();
            _arcadeCacheStart = DateTime.MinValue;
            _arcadeCacheEnd = DateTime.MinValue;
        }

        // Final save
        if (matchesDict.Count > 0)
        {
            await SaveMatches(matchesDict, existingMatches, context);
        }

        logger.LogWarning("Matched {matched}/{total} {desc} ({per})",
            matched,
            dsstatsReplays.Count,
            operationDescription,
            dsstatsReplays.Count == 0 ? "0.00%" : ((double)matched / dsstatsReplays.Count).ToString("P2"));
    }

    private async Task SaveMatches(Dictionary<int, int> matchesDict, Dictionary<int, int> existingMatches, DsstatsContext context)
    {

        List<ReplayArcadeMatch> matches = [];
        foreach (var kvp in matchesDict)
        {
            if (existingMatches.TryGetValue(kvp.Key, out var match))
            {
                if (match != kvp.Value)
                {
                    logger.LogWarning("New match for {id}: old: {oldId} : new: {newId}",
                        kvp.Key, match, kvp.Value);
                }
                continue;
            }
            matches.Add(new ReplayArcadeMatch()
            {
                ReplayId = kvp.Key,
                ArcadeReplayId = kvp.Value,
                MatchTime = DateTime.UtcNow,
            });
            existingMatches.Add(kvp.Key, kvp.Value);
        }
        await context.AddRangeAsync(matches);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<ReplayMatchDto, string>> GetArcadeChunkCached(DateTime dsstatsGameTime, DsstatsContext context, bool continueCalc)
    {
        var windowStart = dsstatsGameTime - ArcadeOffset;
        var windowEnd = windowStart + ArcadeOffset;
        if (_arcadeCache.Count == 0)
        {
            var cache = await GetArcadeChunk(windowStart, ArcadeChunkSize, context);
            if (cache.Count == 0)
            {
                return [];
            }
            _arcadeCache = cache.ToDictionary(k => k, v => GetOrderedToonKey(v));
            _arcadeCacheEnd = cache.Last().Gametime;
            _arcadeCacheStart = windowStart;
        }
        else if (windowStart >= _arcadeCacheStart && windowEnd <= _arcadeCacheEnd)
        {
            return _arcadeCache;
        }
        else
        {
            var cache = await GetArcadeChunk(dsstatsGameTime, ArcadeChunkSize, context);
            if (cache.Count == 0)
            {
                return [];
            }
            _arcadeCacheEnd = cache.Last().Gametime;
            var takeOver = _arcadeCache.Keys.Where(x => x.Gametime >= windowStart);
            _arcadeCache = takeOver.Concat(cache).GroupBy(k => k.ReplayId).Select(g => g.First()).ToDictionary(k => k, v => GetOrderedToonKey(v));
        }
        _arcadeCacheStart = windowStart;


        logger.LogInformation("arcade cache: {start} - {end}", _arcadeCacheStart.ToShortDateString(), _arcadeCacheEnd.ToShortDateString());
        return _arcadeCache;
    }

    public static double GetMatchScore(ReplayMatchDto a, ReplayMatchDto b)
    {
        double score = 0;
        double maxScore = 0;

        // 2. Gametime (tolerance window)
        maxScore += 20;
        var diff = (a.Gametime - b.Gametime).Duration();
        if (diff < TimeSpan.FromMinutes(5))
            score += 20;
        else if (diff < TimeSpan.FromMinutes(10))
            score += 15;
        else if (diff < TimeSpan.FromMinutes(15))
            score += 10;
        else if (diff < TimeSpan.FromMinutes(30))
            score += 5;

        // 4. Player roster overlap
        maxScore += 60;
        var aPlayers = a.Players.Select(p => (p.ToonId.Region, p.ToonId.Realm, p.ToonId.Id)).ToHashSet();
        var bPlayers = b.Players.Select(p => (p.ToonId.Region, p.ToonId.Realm, p.ToonId.Id)).ToHashSet();

        int common = aPlayers.Intersect(bPlayers).Count();
        int total = Math.Max(aPlayers.Count, bPlayers.Count);
        if (total > 0)
        {
            double ratio = (double)common / total;
            score += ratio * 60;
        }

        // 5. GameMode
        maxScore += 10;
        if (a.GameMode == b.GameMode)
            score += 10;

        return score / maxScore; // normalized between 0 and 1
    }

    private async Task<List<ReplayMatchDto>> GetDsstatsChunk(DateTime fromDate, int take, DsstatsContext context, bool continueCalc)
    {
        return await context.Replays
            .OrderBy(o => o.Gametime)
            .Where(x => continueCalc ? x.Imported > fromDate : x.Gametime > fromDate
                && x.PlayerCount == 6
                && x.Duration > 300
                && x.WinnerTeam > 0)
            .Take(take)
            .Select(s => new ReplayMatchDto()
            {
                ReplayId = s.ReplayId,
                Gametime = s.Gametime,
                Duration = s.Duration,
                GameMode = s.GameMode,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.Players.Select(x => new PlayerMatchDto()
                {
                    ReplayPlayerId = x.ReplayPlayerId,
                    Team = x.TeamId,
                    ToonId = new()
                    {
                        Region = x.Player!.ToonId.Region,
                        Realm = x.Player.ToonId.Realm,
                        Id = x.Player.ToonId.Id,
                    }
                }).ToList(),
            }).ToListAsync();
    }

    private static async Task<List<ReplayMatchDto>> GetArcadeChunk(DateTime fromDate, int take, DsstatsContext context)
    {
        return await context.ArcadeReplays
            .OrderBy(o => o.CreatedAt)
            .Where(x => x.CreatedAt >= fromDate)
            .Take(take)
            .Select(s => new ReplayMatchDto()
            {
                ReplayId = s.ArcadeReplayId,
                Gametime = s.CreatedAt,
                Duration = s.Duration,
                GameMode = s.GameMode,
                PlayerCount = s.PlayerCount,
                WinnerTeam = s.WinnerTeam,
                Players = s.Players.Select(x => new PlayerMatchDto()
                {
                    ReplayPlayerId = x.ArcadeReplayPlayerId,
                    Team = x.Team,
                    ToonId = new()
                    {
                        Region = x.Player!.ToonId.Region,
                        Realm = x.Player.ToonId.Realm,
                        Id = x.Player.ToonId.Id,
                    }
                }).ToList()
            })
            .ToListAsync();
    }

    public static string GetOrderedToonKey(ReplayMatchDto replay)
    {
        return string.Join(",", replay.Players
            .Select(p => p.ToonId.Id)
            .OrderBy(id => id));
    }
}

