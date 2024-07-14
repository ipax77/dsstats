using dsstats.db8;
using dsstats.shared.Calc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace dsstats.ratings;

public class ComboRatings(ReplayContext context, ILogger<ComboRatings> logger)
{
    private Dictionary<ReplayKey, int> RegionDict = [];
    private Dictionary<ReplayKey, List<int>> ToonIdsDict = [];

    public async Task InitDb()
    {
        var matches = JsonSerializer.Deserialize<List<KeyValuePair<int, int>>>(File.ReadAllText("/data/ds/replaymatches.json")) ?? [];
        var matchTime = DateTime.UtcNow;

        List<ReplayArcadeMatch> dbMatches = matches.Select(s => new ReplayArcadeMatch()
        {
            ReplayId = s.Key,
            ArcadeReplayId = s.Value,
            MatchTime = matchTime,
        }).ToList();

        context.ReplayArcadeMatches.AddRange(dbMatches);
        await context.SaveChangesAsync();
    }

    public async Task CombineDsstatsSc2ArcadeReplays()
    {
        Stopwatch sw = Stopwatch.StartNew();
        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2021, 2, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 1000
        };
        HashSet<int> matchedArcadeIds = [];
        var matchesInfo = await GetProcessedReplayIds();
        dsstatsRequest.Imported = matchesInfo.LatestUpdate;
        int matches = 0;
        var dsstatsReplays = await GetComboDsstatsCalcDtos(dsstatsRequest, context);

        while (dsstatsReplays.Count > 0)
        {
            dsstatsReplays = dsstatsReplays.Where(x => !matchesInfo.ReplayDict.ContainsKey(x.ReplayId))
                .ToList();

            var arcadeReplays = await GetComboArcadeCalcDtos(dsstatsReplays, matchedArcadeIds, context);
            arcadeReplays = arcadeReplays.Where(x => !matchesInfo.ArcadeDict.ContainsKey(x.ReplayId)).ToList();

            List<ReplayArcadeMatch> replayMatches = [];

            foreach (var dsstatsReplay in dsstatsReplays)
            {
                var arcadeReplay = await FindSc2ArcadeReplay(dsstatsReplay, arcadeReplays);
                if (arcadeReplay is not null)
                {
                    arcadeReplays.Remove(arcadeReplay);
                    matchedArcadeIds.Add(arcadeReplay.ReplayId);
                    replayMatches.Add(new()
                    {
                        ReplayId = dsstatsReplay.ReplayId,
                        ArcadeReplayId = arcadeReplay.ReplayId,
                        MatchTime = DateTime.UtcNow
                    });
                    matches++;
                }
            }
            await StoreReplayMatches(replayMatches);
            dsstatsRequest.Skip += dsstatsRequest.Take;
            dsstatsReplays = await GetComboDsstatsCalcDtos(dsstatsRequest, context);
            RegionDict.Clear();
            ToonIdsDict.Clear();
            logger.LogInformation("skip: {skip}", dsstatsRequest.Skip);
        }
        sw.Stop();
        logger.LogInformation("Matches: {matches} in {elapsed}sec", matches, Math.Round(sw.Elapsed.TotalSeconds, 2));
    }

    private async Task<List<CalcDto>> GetReasonableReplays(CalcDto dsstatsReplay, List<CalcDto> arcadeReplays)
    {
        if (arcadeReplays.Count > 0)
        {
            return arcadeReplays
                .Where(x => x.GameTime > dsstatsReplay.GameTime.AddDays(-0.5)
                    && x.GameTime < dsstatsReplay.GameTime.AddDays(0.5)
                    && x.GameMode == dsstatsReplay.GameMode
                    && GetReplayRegionId(x) == GetReplayRegionId(dsstatsReplay)
                ).ToList();
        }
        else
        {

        }
        return [];
    }

    private async Task StoreReplayMatches(List<ReplayArcadeMatch> replayMatches)
    {
        if (replayMatches.Count == 0)
        {
            return;
        }

        context.ReplayArcadeMatches.AddRange(replayMatches);
        await context.SaveChangesAsync();
    }

    private async Task<ReplayMathInfo> GetProcessedReplayIds()
    {
        var matches = await context.ReplayArcadeMatches
            .ToListAsync();

        return new()
        {
            ReplayDict = matches.ToDictionary(k => k.ReplayId, v => true),
            ArcadeDict = matches.ToDictionary(k => k.ArcadeReplayId, v => true),
            LatestUpdate = matches.OrderByDescending(o => o.MatchTime).FirstOrDefault()?.MatchTime ?? DateTime.MinValue,
        };
    }

    private async Task<CalcDto?> FindSc2ArcadeReplay(CalcDto dsstatsReplay, List<CalcDto> arcadeReplays)
    {
        var reasonableReplays = await GetReasonableReplays(dsstatsReplay, arcadeReplays);

        if (reasonableReplays.Count == 0)
        {
            return null;
        }

        var dsstatsPlayerIds = GetOrderedToonIds(dsstatsReplay);
        CalcDto? bestMatch = null;
        int minBestScore = dsstatsPlayerIds.Count;
        int bestMatchScore = 0;

        foreach (var arcadeReplay in reasonableReplays)
        {
            var arcadePlayerIds = GetOrderedToonIds(arcadeReplay);
            var key = new ReplayKey(arcadeReplay.ReplayId, arcadeReplay.IsArcade);
            int lcsLength = CalculateLCSLength(dsstatsPlayerIds, arcadePlayerIds);

            if (lcsLength > bestMatchScore)
            {
                bestMatchScore = lcsLength;
                bestMatch = arcadeReplay;
            }
        }
        return bestMatchScore >= minBestScore ? bestMatch : null;
    }

    private static bool IsDurationWithinThreshold(int dsstatsDuration, int arcadeDuration, double thresholdPercentage)
    {
        double difference = Math.Abs(dsstatsDuration - arcadeDuration);
        double maxAllowedDifference = dsstatsDuration * thresholdPercentage + 300;
        return difference <= maxAllowedDifference;
    }

    private List<int> GetOrderedToonIds(CalcDto replay)
    {
        var key = new ReplayKey(replay.ReplayId, replay.IsArcade);
        if (!ToonIdsDict.TryGetValue(key, out var ids))
        {
            ToonIdsDict[key] = ids = replay.Players
                .OrderBy(o => o.Team)
                .ThenBy(o => o.GamePos)
                .Select(s => s.PlayerId.ToonId)
                .ToList();
        }
        return ids;
    }

    private static int CalculateLCSLength(List<int> a, List<int> b)
    {
        int[,] dp = new int[a.Count + 1, b.Count + 1];

        for (int i = 1; i <= a.Count; i++)
        {
            for (int j = 1; j <= b.Count; j++)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[a.Count, b.Count];
    }

    private int GetReplayRegionId(CalcDto replay)
    {
        var key = new ReplayKey(replay.ReplayId, replay.IsArcade);
        if (!RegionDict.TryGetValue(key, out var regionId))
        {
            RegionDict[key] = replay.Players
            .GroupBy(p => p.PlayerId.RegionId)
            .OrderByDescending(g => g.Count())
            .First().Key;
        }
        return regionId;
    }

    private static async Task<List<CalcDto>> GetComboDsstatsCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        var query = from r in context.Replays
                    join m in context.ReplayArcadeMatches on r.ReplayId equals m.ReplayId into grouping
                    from m in grouping.DefaultIfEmpty()
                    where m == null
                        && r.Playercount == 6
                        && r.Duration >= 300
                        && r.WinnerTeam > 0
                        && request.GameModes.Contains((int)r.GameMode)
                        && r.TournamentEdition == false
                        && r.GameTime >= request.FromDate
                        && (request.Continue ? r.ReplayRatingInfo == null : true)
                    // && (r.Imported == DateTime.MinValue || r.Imported > request.Imported)
                    orderby r.GameTime, r.ReplayId
                    select new RawCalcDto
                    {
                        DsstatsReplayId = r.ReplayId,
                        GameTime = r.GameTime,
                        Duration = r.Duration,
                        Maxkillsum = r.Maxkillsum,
                        GameMode = (int)r.GameMode,
                        TournamentEdition = false,
                        Players = r.ReplayPlayers.Select(t => new RawPlayerCalcDto
                        {
                            ReplayPlayerId = t.ReplayPlayerId,
                            GamePos = t.GamePos,
                            PlayerResult = (int)t.PlayerResult,
                            Race = t.Race,
                            Duration = t.Duration,
                            Kills = t.Kills,
                            Team = t.Team,
                            IsUploader = t.Player.UploaderId != null,
                            PlayerId = new(t.Player.ToonId, t.Player.RealmId, t.Player.RegionId)
                        }).ToList()
                    };

        var rawDtos = await query
            .AsSplitQuery()
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        return rawDtos.Select(s => s.GetCalcDto()).ToList();
    }

    private static async Task<List<CalcDto>> GetComboArcadeCalcDtos(List<CalcDto> dsstatsCalcDtos, HashSet<int> processedReplayIds, ReplayContext context)
    {
        if (dsstatsCalcDtos.Count == 0)
        {
            return [];
        }

        var oldestReplayDate = dsstatsCalcDtos.First().GameTime.AddDays(-1);
        var latestReplayDate = dsstatsCalcDtos.Last().GameTime.AddDays(1);

        var startId = await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt > oldestReplayDate)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .FirstOrDefaultAsync();

        var endId = await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt < latestReplayDate)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .LastOrDefaultAsync();

        if (startId == endId || startId > endId || endId - startId > 250000)
        {
            return [];
        }

        var query = from r in context.MaterializedArcadeReplays
                    orderby r.MaterializedArcadeReplayId
                    where r.MaterializedArcadeReplayId >= startId
                        && r.MaterializedArcadeReplayId <= endId
                    select new
                    {
                        r.ArcadeReplayId,
                        CalcDto = new CalcDto()
                        {
                            ReplayId = r.ArcadeReplayId,
                            GameTime = r.CreatedAt,
                            Duration = r.Duration,
                            GameMode = (int)r.GameMode,
                            WinnerTeam = r.WinnerTeam,
                            TournamentEdition = false,
                            IsArcade = true,
                            Players = context.ArcadeReplayPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new PlayerCalcDto()
                                {
                                    ReplayPlayerId = t.ArcadeReplayPlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = (int)t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = new(t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId)
                                }).ToList()
                        }
                    };

        var data = await query
            .AsSplitQuery()
            .ToListAsync();

        return data.Where(x => !processedReplayIds.Contains(x.ArcadeReplayId))
            .Select(s => s.CalcDto)
            .ToList();
    }
}

internal record ReplayKey
{
    public ReplayKey(int replayId, bool isArcade)
    {
        ReplayId = replayId;
        IsArcade = isArcade;
    }
    public int ReplayId { get; set; }
    public bool IsArcade { get; set; }
}

internal record ReplayPartKey
{
    public ReplayPartKey(int gameMode, int regionId)
    {
        GameMode = gameMode;
        RegionId = regionId;
    }
    public int GameMode { get; set; }
    public int RegionId { get; set; }
}

internal record ReplayMathInfo
{
    public Dictionary<int, bool> ReplayDict { get; set; } = [];
    public Dictionary<int, bool> ArcadeDict { get; set; } = [];
    public DateTime LatestUpdate { get; set; }
}