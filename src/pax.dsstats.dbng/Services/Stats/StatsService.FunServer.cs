
using pax.dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<FunStatsResult> GetFunStats(FunStatsRequest request, CancellationToken token = default)
    {
        var result = await GetResultFromMemory(request, token);

        if (result == null)
        {
            result = await ProduceFunStats(request, token);
            await UpdateFunStatsMemory(request, result);
        }

        return result;
    }

    private async Task<FunStatsResult> ProduceFunStats(FunStatsRequest request, CancellationToken token = default)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);

        if (end == DateTime.Today)
        {
            end = end.AddDays(2);
        }

        FunStatsResult result = new()
        {
            Created = DateTime.UtcNow,
            TotalTimePlayed = await GetTotalDuration(request.RatingType, start, end, token),
            AvgGameDuration = await GetAvgDuration(request.RatingType, start, end, token),
            MostLeastBuildUnit = await GetMostLeastBuildUnit(request.RatingType, start, end, token),
            GreatestArmyReplay = await GetGreatestArmyReplay(request.RatingType, start, end, token),
            MostUpgradesReplay = await GetMostUpgradesReplay(request.RatingType, start, end, token),
            MostCompetitiveReplay = await GetMostCompetitiveReplay(request.RatingType, start, end, token),
            GreatestComebackReplay = await GetGreatestComebackReplay(request.RatingType, start, end, token),
        };
        return result;
    }

    private async Task<FunStatsResult?> GetResultFromMemory(FunStatsRequest request, CancellationToken token)
    {
        var funstats = await context.FunStatMemories
            .FirstOrDefaultAsync(f => f.RatingType == request.RatingType && f.TimePeriod == request.TimePeriod, token);

        if (funstats == null)
        {
            return null;
        }

        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);

        int limit = (int)(end - start).TotalDays switch 
        {
            < 100 => 30,
            < 400 => 90,
            _ => 120
        };


        if ((DateTime.UtcNow - funstats.Created).TotalDays > limit)
        {
            return null;
        }

        return new()
        {
            Created = funstats.Created,
            TotalTimePlayed = funstats.TotalTimePlayed,
            AvgGameDuration = funstats.AvgGameDuration,
            MostLeastBuildUnit = 
                new(
                    new() { UnitName = funstats.UnitNameMost, Count = funstats.UnitCountMost },
                    new() { UnitName = funstats.UnitNameLeast, Count = funstats.UnitCountLeast }
                ),
            FirstReplay = funstats.FirstReplay == null ?
                null : await replayRepository.GetDetailReplay(funstats.FirstReplay, true, token),
            GreatestArmyReplay = funstats.GreatestArmyReplay == null ?
                null : await replayRepository.GetDetailReplay(funstats.GreatestArmyReplay, true, token),
            MostUpgradesReplay = funstats.MostUpgradesReplay == null ?
                null : await replayRepository.GetDetailReplay(funstats.MostUpgradesReplay, true, token),
            MostCompetitiveReplay = funstats.MostCompetitiveReplay == null ?
                null : await replayRepository.GetDetailReplay(funstats.MostCompetitiveReplay, true, token),
            GreatestComebackReplay = funstats.GreatestComebackReplay == null ?
                null : await replayRepository.GetDetailReplay(funstats.GreatestComebackReplay, true, token),
        };
    }

    private async Task UpdateFunStatsMemory(FunStatsRequest request, FunStatsResult result)
    {
        var funstats = await context.FunStatMemories
            .FirstOrDefaultAsync(f => f.RatingType == request.RatingType && f.TimePeriod == request.TimePeriod);
        
        if (funstats == null)
        {
            funstats = new() 
            {
                RatingType = request.RatingType,
                TimePeriod = request.TimePeriod
            };
            context.FunStatMemories.Add(funstats);
        }

        funstats.Created = DateTime.UtcNow;
        funstats.TotalTimePlayed = result.TotalTimePlayed;
        funstats.AvgGameDuration = result.AvgGameDuration;
        funstats.UnitNameMost = result.MostLeastBuildUnit.Key?.UnitName ?? "";
        funstats.UnitCountMost = result.MostLeastBuildUnit.Key?.Count ?? 0;
        funstats.UnitNameLeast = result.MostLeastBuildUnit.Value?.UnitName ?? "";
        funstats.UnitCountLeast = result.MostLeastBuildUnit.Value?.Count ?? 0;
        funstats.FirstReplay = result.FirstReplay?.ReplayHash;
        funstats.GreatestArmyReplay = result.GreatestArmyReplay?.ReplayHash;
        funstats.MostUpgradesReplay = result.MostUpgradesReplay?.ReplayHash;
        funstats.MostCompetitiveReplay = result.MostCompetitiveReplay?.ReplayHash;
        funstats.GreatestComebackReplay = result.GreatestComebackReplay?.ReplayHash;

        await context.SaveChangesAsync();
    }

    private async Task<long> GetTotalDuration(RatingType ratingType, DateTime start, DateTime end, CancellationToken token)
    {
        var total = from rr in context.ReplayRatings
                    where rr.RatingType == ratingType
                      && rr.Replay.GameTime >= start
                      && rr.Replay.GameTime < end
                    select rr.Replay;
        return await total.SumAsync(a => a.Duration, token);
    }

    private async Task<int> GetAvgDuration(RatingType ratingType, DateTime start, DateTime end, CancellationToken token)
    {
        var total = from rr in context.ReplayRatings
                    where rr.RatingType == ratingType
                      && rr.Replay.GameTime >= start
                      && rr.Replay.GameTime < end
                    select rr.Replay;
        return Convert.ToInt32(await total
            .Select(s => s.Duration)
            .DefaultIfEmpty()
            .AverageAsync(token));
    }

    private async Task<KeyValuePair<UnitInfo, UnitInfo>> GetMostLeastBuildUnit(RatingType ratingType,
                                                                               DateTime start,
                                                                               DateTime end,
                                                                               CancellationToken token)
    {
        var unitGroup = from rr in context.ReplayRatings
                        from rp in rr.Replay.ReplayPlayers
                        from sp in rp.Spawns
                        from su in sp.Units
                        where rr.RatingType == ratingType
                          && rr.Replay.GameTime >= start
                          && rr.Replay.GameTime < end
                          && rr.Replay.DefaultFilter
                          && sp.Breakpoint == Breakpoint.All
                        group su by su.UnitId into g
                        orderby g.Sum(s => s.Count)
                        select new
                        {
                            UnitId = g.Key,
                            Count = g.Sum(s => s.Count)
                        };

        var first = await unitGroup.LastOrDefaultAsync(token);
        var last = await unitGroup.FirstOrDefaultAsync(token);

        if (first == null || last == null)
        {
            return new(new(), new());
        }

        var firstInfo = new UnitInfo()
        {
            UnitName = context.Units.FirstOrDefault(f => f.UnitId == first.UnitId)?.Name ?? "",
            Count = first.Count,
        };
        var lastInfo = new UnitInfo()
        {
            UnitName = context.Units.FirstOrDefault(f => f.UnitId == last.UnitId)?.Name ?? "",
            Count = last.Count,
        };
        return new(firstInfo, lastInfo);
    }


    public async Task<ReplayDetailsDto?> GetGreatestArmyReplay(RatingType ratingType,
                                                              DateTime start,
                                                              DateTime end,
                                                              CancellationToken token)
    {
        var hashinfos = from rr in context.ReplayRatings
                from rp in rr.Replay.ReplayPlayers
                from sp in rp.Spawns
                where rr.RatingType == ratingType
                    && rr.Replay.GameTime >= start
                    && rr.Replay.GameTime < end
                    && rr.Replay.DefaultFilter
                    && sp.Breakpoint == Breakpoint.All
                orderby sp.ArmyValue
                select rr.Replay.ReplayHash;

        var hashInfo = await hashinfos.LastOrDefaultAsync(token);

        if (hashInfo == null)
        {
            return null;
        }

        return await replayRepository.GetDetailReplay(hashInfo, true, token);
    }

    public async Task<ReplayDetailsDto?> GetMostUpgradesReplay(RatingType ratingType,
                                                              DateTime start,
                                                              DateTime end,
                                                              CancellationToken token)
    {
        var hashinfos = from rr in context.ReplayRatings
                from rp in rr.Replay.ReplayPlayers
                from sp in rp.Spawns
                where rr.RatingType == ratingType
                    && rr.Replay.GameTime >= start
                    && rr.Replay.GameTime < end
                    && rr.Replay.DefaultFilter
                    && sp.Breakpoint == Breakpoint.All
                orderby sp.UpgradeSpent
                select rr.Replay.ReplayHash; 

        var hashInfo = await hashinfos.LastOrDefaultAsync(token);

        if (hashInfo == null)
        {
            return null;
        }

        return await replayRepository.GetDetailReplay(hashInfo, true, token);
    } 

    public async Task<ReplayDetailsDto?> GetMostCompetitiveReplay(RatingType ratingType,
                                                              DateTime start,
                                                              DateTime end,
                                                              CancellationToken token)
    {
        var hashinfos = from rr in context.ReplayRatings
                where rr.RatingType == ratingType
                    && rr.Replay.GameTime >= start
                    && rr.Replay.GameTime < end
                    && rr.Replay.DefaultFilter
                orderby rr.Replay.Middle.Length
                select rr.Replay.ReplayHash; 

        var hashInfo = await hashinfos.LastOrDefaultAsync(token);

        if (hashInfo == null)
        {
            return null;
        }

        return await replayRepository.GetDetailReplay(hashInfo, true, token);
    }    

    public async Task<ReplayDetailsDto?> GetGreatestComebackReplay(RatingType ratingType,
                                                              DateTime start,
                                                              DateTime end,
                                                              CancellationToken token)
    {
        var hashinfos = from rr in context.ReplayRatings
                where rr.RatingType == ratingType
                    && rr.Replay.GameTime >= start
                    && rr.Replay.GameTime < end
                    && rr.Replay.DefaultFilter
                select new 
                {
                    rr.Replay.ReplayHash,
                    rr.Replay.Middle,
                    rr.Replay.Duration,
                    rr.Replay.WinnerTeam
                };

        var infos = await hashinfos.ToListAsync(token);

        double minMid = 100;
        string? replayHash = null;

        for (int i = 0; i < infos.Count; i++)
        {
            var info = infos[i];
            (int startTeam, int[] gameloops, int totalGameloops) = GetMiddleInfo(info.Middle, info.Duration);
            (var mid1, var mid2) = GetChartMiddle(startTeam, gameloops, totalGameloops);

            var winnerMid = info.WinnerTeam == 1 ? mid1 : mid2;
            if (winnerMid > 0 && winnerMid < minMid)
            {
                minMid = winnerMid;
                replayHash = info.ReplayHash;
            }
        }

        if (replayHash == null)
        {
            return null;
        }

        return await replayRepository.GetDetailReplay(replayHash, true, token);
    }

    public async Task SeedFunStats()
    {
        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }
            foreach (TimePeriod timePeriod in Enum.GetValues(typeof(TimePeriod)))
            {
                if ((int)timePeriod < 3)
                {
                    continue;
                }
                FunStatsRequest request = new()
                {
                    RatingType = ratingType,
                    TimePeriod = timePeriod
                };

                var result = await ProduceFunStats(request);
                await UpdateFunStatsMemory(request, result);
            }
        }
    }
}