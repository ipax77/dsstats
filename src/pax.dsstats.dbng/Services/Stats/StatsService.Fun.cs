
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<FunStats> GetFunStats(List<int> toonIds)
    {
        FunStats funStats = new();

        var playersIds = await context.Players
            .Where(x => toonIds.Contains(x.ToonId))
            .Select(s => s.PlayerId)
            .ToListAsync();

        var replayPlayers = context.ReplayPlayers
            .Where(x => playersIds.Contains(x.PlayerId));

        var sumDuration = await replayPlayers.SumAsync(s => s.Duration);
        var avgDuration = await replayPlayers.AverageAsync(a => a.Duration);

        funStats.TotalDuration = TimeSpan.FromSeconds(sumDuration);
        funStats.AvgDuration = TimeSpan.FromSeconds(avgDuration);

        funStats.PosInfos = await GetPosInfo(replayPlayers);
        funStats.FirstReplay = await GetFirstReplayInfo(toonIds);
        funStats.GreatestArmyReplay = await GetGreatestArmyReplayInfo(toonIds);
        funStats.MostUpgradesReplay = await GetMostUpgradesReplayInfo(toonIds);
        funStats.MostCompetitiveReplay = await GetMostCompetitiveReplayInfo(toonIds);
        funStats.GreatestComebackReplay = await GetGreatestComebackReplayInfo(replayPlayers);

        (var firstUnitInfo, var lastUnitInfo) = await GetUnitsBuild(replayPlayers);
        funStats.MostBuildUnit = lastUnitInfo;
        funStats.LeastBuildUnit = firstUnitInfo;

        return funStats;
    }

    private async Task<(UnitInfo?, UnitInfo?)> GetUnitsBuild(IQueryable<ReplayPlayer> replayPlayers)
    {
        var unitGroup = from rp in replayPlayers
                        from s in rp.Spawns
                        from su in s.Units
                        group su by su.UnitId into g
                        orderby g.Sum(s => s.Count)
                        select new
                        {
                            UnitId = g.Key,
                            Count = g.Sum(s => s.Count)
                        };

        var first = await unitGroup.FirstOrDefaultAsync();
        var last = await unitGroup.LastOrDefaultAsync();

        if (first == null || last == null)
        {
            return (null, null);
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
        return (firstInfo, lastInfo);
    }

    private async Task<ReplayDto?> GetFirstReplayInfo(List<int> toonIds)
    {
        var replayHash = await context.ReplayPlayers
            .Where(x => toonIds.Contains(x.Player.ToonId))
            .OrderBy(o => o.Replay.GameTime)
            .Select(s => s.Replay.ReplayHash)
            .FirstOrDefaultAsync();

        if (replayHash == null)
        {
            return null;
        }
        return await replayRepository.GetReplay(replayHash);
    }

    private async Task<ReplayDto?> GetGreatestArmyReplayInfo(List<int> toonIds)
    {
        var replayHash = await context.ReplayPlayers
            .Where(x => toonIds.Contains(x.Player.ToonId))
            .OrderByDescending(o => o.Army)
            .Select(s => s.Replay.ReplayHash)
            .FirstOrDefaultAsync();

        if (replayHash == null)
        {
            return null;
        }
        return await replayRepository.GetReplay(replayHash);
    }

    private async Task<ReplayDto?> GetGreatestComebackReplayInfo(IQueryable<ReplayPlayer> replayPlayers)
    {
        var infos = from rp in replayPlayers
                    where rp.PlayerResult == PlayerResult.Win && rp.Replay.Duration > 360 && rp.Replay.Middle.Length > 0
                    select new
                    {
                        rp.Replay.Middle,
                        rp.Replay.PlayerPos,
                        rp.Replay.ReplayHash,
                        rp.Replay.Duration
                    };
        var linfos = await infos.ToListAsync();

        double minMid = 100;
        string? replayHash = null;
        foreach (var info in linfos)
        {
            (int startTeam, int[] gameloops, int totalGameloops) = GetMiddleInfo(info.Middle, info.Duration);
            (var mid1, var mid2) = GetChartMiddle(startTeam, gameloops, totalGameloops);

            var plMid = info.PlayerPos < 4 ? mid1 : mid2;
            if (plMid > 0 && plMid < minMid)
            {
                minMid = plMid;
                replayHash = info.ReplayHash;
            }
        }

        if (replayHash == null)
        {
            return null;
        }
        return await replayRepository.GetReplay(replayHash);
    }

    private async Task<ReplayDto?> GetMostUpgradesReplayInfo(List<int> toonIds)
    {
        var replayHash = await context.ReplayPlayers
            .Where(x => toonIds.Contains(x.Player.ToonId))
            .OrderByDescending(o => o.UpgradesSpent)
            .Select(s => s.Replay.ReplayHash)
            .FirstOrDefaultAsync();

        if (replayHash == null)
        {
            return null;
        }
        return await replayRepository.GetReplay(replayHash);
    }

    private async Task<ReplayDto?> GetMostCompetitiveReplayInfo(List<int> toonIds)
    {
        var replayHash = await context.ReplayPlayers
            .Where(x => toonIds.Contains(x.Player.ToonId))
            .OrderByDescending(o => o.Replay.Middle.Length)
            .Select(s => s.Replay.ReplayHash)
            .FirstOrDefaultAsync();

        if (replayHash == null)
        {
            return null;
        }
        return await replayRepository.GetReplay(replayHash);
    }

    private async Task<List<PosInfo>> GetPosInfo(IQueryable<ReplayPlayer> replayPlayers)
    {
        var posInfo = from rp in replayPlayers
                      where rp.GamePos > 0
                      group rp by rp.GamePos into g
                      select new PosInfo()
                      {
                          Pos = g.Key,
                          Count = g.Count(),
                          Wins = g.Count(c => c.PlayerResult == PlayerResult.Win)
                      };

        return await posInfo
            .ToListAsync();
    }

    public static (int, int[], int) GetMiddleInfo(string middleString, int duration)
    {
        int totalGameloops = (int)(duration * 22.4);

        if (!String.IsNullOrEmpty(middleString))
        {
            var ents = middleString.Split('|').Where(x => !String.IsNullOrEmpty(x)).ToArray();
            var ients = ents.Select(s => int.Parse(s)).ToList();
            ients.Add(totalGameloops);
            int startTeam = ients[0];
            ients.RemoveAt(0);
            return (startTeam, ients.ToArray(), totalGameloops);
        }
        return (0, Array.Empty<int>(), totalGameloops);
    }

    public static (double, double) GetChartMiddle(int startTeam, int[] gameloops, int gameloop)
    {
        if (gameloops.Length < 2)
        {
            return (0, 0);
        }

        int sumTeam1 = 0;
        int sumTeam2 = 0;
        bool isFirstTeam = startTeam == 1;
        int lastLoop = 0;
        bool hasInfo = false;

        for (int i = 0; i < gameloops.Length; i++)
        {
            if (lastLoop > gameloop)
            {
                hasInfo = true;
                break;
            }

            isFirstTeam = !isFirstTeam;
            if (lastLoop > 0)
            {
                if (isFirstTeam)
                {
                    sumTeam1 += gameloops[i] - lastLoop;
                }
                else
                {
                    sumTeam2 += gameloops[i] - lastLoop;
                }
            }
            lastLoop = gameloops[i];
        }

        if (hasInfo)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= lastLoop - gameloop;
            }
            else
            {
                sumTeam2 -= lastLoop - gameloop;
            }
        }
        else if (gameloops.Length > 0)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= gameloops[^1] - gameloop;
            }
            else
            {
                sumTeam2 -= gameloops[^1] - gameloop;
            }
        }

        sumTeam1 = Math.Max(sumTeam1, 0);
        sumTeam2 = Math.Max(sumTeam2, 0);

        return (Math.Round(sumTeam1 * 100.0 / (double)gameloops[^1], 2), Math.Round(sumTeam2 * 100.0 / (double)gameloops[^1], 2));
    }
}

