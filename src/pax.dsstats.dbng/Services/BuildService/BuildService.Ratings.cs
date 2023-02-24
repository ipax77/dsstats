

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class BuildService
{
    public async Task<BuildRatingResponse> GetBuildByRating(BuildRatingRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out BuildRatingResponse result))
        {
            result = await ProduceBuildRating(request, token);
            memoryCache.Set(memKey, result, TimeSpan.FromHours(24));
        }
        return result;
    }

    private async Task<BuildRatingResponse> ProduceBuildRating(BuildRatingRequest request, CancellationToken token = default)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        if (end == DateTime.Today)
        {
            end.AddDays(2);
        }

        if (request.ToRating >= Data.MaxBuildRating)
        {
            request.ToRating = 5000;
        }

        if (request.FromRating <= Data.MinBuildRating)
        {
            request.FromRating = 0;
        }

        (var count, var wins, var cupgrades) = await GetCountAndWins(context, request, start, end, token);
        var list = await GetUnitSums(context, request, start, end, token);
        // var upgradesSpent = await GetUpgradesSpent(context, request, start, end, token);

        return new()
        {
            Count = (int)count,
            Winrate = count == 0 ? 0 : Math.Round(wins * 100.0 / count, 2),
            UpgradesSpent = Math.Round(cupgrades, 2),
            Units = list.Select(s => new BuildRatingUnit()
            {
                Name = s.Name,
                Avg = Math.Round(s.Sum / count, 2),
            }).ToList(),
        };
    }

    private static async Task<double> GetUpgradesSpent(ReplayContext context, BuildRatingRequest request, DateTime start, DateTime end, CancellationToken token)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    from sp in rp.Spawns
                    where r.ReplayRatingInfo.RatingType == request.RatingType
                        && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
                        && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
                        && rp.Race == request.Interest
                        && r.GameTime >= start && r.GameTime < end
                        && sp.Breakpoint == request.Breakpoint
                    select sp;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return await group
            .Select(s => s.UpgradeSpent)
            .DefaultIfEmpty()
            .AverageAsync(token);
    }

    private static async Task<List<SumHelper>> GetUnitSums(ReplayContext context, BuildRatingRequest request, DateTime start, DateTime end, CancellationToken token)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = request.Vs == Commander.None ?
            from rp in context.ReplayPlayers
            from sp in rp.Spawns
            from su in sp.Units
            where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
                && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
                && rp.Race == request.Interest
                && rp.Replay.GameTime >= start && rp.Replay.GameTime < end
                && sp.Breakpoint == request.Breakpoint
            group su by su.Unit.Name into g
            select new SumHelper()
            {
                Name = g.Key,
                Sum = g.Sum(s => s.Count),
            }
         : from rp in context.ReplayPlayers
           from sp in rp.Spawns
           from su in sp.Units
           where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
               && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
               && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
               && rp.Race == request.Interest && rp.OppRace == request.Vs
               && rp.Replay.GameTime >= start && rp.Replay.GameTime < end
               && sp.Breakpoint == request.Breakpoint
           group su by su.Unit.Name into g
           select new SumHelper()
           {
               Name = g.Key,
               Sum = g.Sum(s => s.Count),
           };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return await group.ToListAsync(token);
    }

    private static async Task<(double, double, double)> GetCountAndWins(ReplayContext context, BuildRatingRequest request, DateTime start, DateTime end, CancellationToken token)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replayPlayers = request.Vs == Commander.None ?
             from rp in context.ReplayPlayers
             from sp in rp.Spawns
             from su in sp.Units
             where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                 && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
                 && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
                 && rp.Race == request.Interest
                 && rp.Replay.GameTime >= start && rp.Replay.GameTime < end
                 && sp.Breakpoint == request.Breakpoint
              group rp by rp.PlayerResult into g
              select new 
              {
                g.Key,
                Count = (from p in g select p.ReplayPlayerId).Distinct().Count(),
                Upgrades = g.Average(a => a.Spawns.Where(x => x.Breakpoint == request.Breakpoint).Select(s => s.UpgradeSpent).Average())
              }
            : from rp in context.ReplayPlayers
              from sp in rp.Spawns
              from su in sp.Units
              where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                  && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
                  && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
                  && rp.Race == request.Interest && rp.OppRace == request.Vs
                  && rp.Replay.GameTime >= start && rp.Replay.GameTime < end
                  && sp.Breakpoint == request.Breakpoint
              group rp by rp.PlayerResult into g
              select new 
              {
                g.Key,
                Count = (from p in g select p.ReplayPlayerId).Distinct().Count(),
                Upgrades = g.Average(a => a.Spawns.Where(x => x.Breakpoint == request.Breakpoint).Select(s => s.UpgradeSpent).Average())
              };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        var clist = await replayPlayers.ToListAsync(token);

        double count = clist.Select(s => s.Count).Sum();
        double wins = clist.FirstOrDefault(f => f.Key == PlayerResult.Win)?.Count ?? 0;
        double upgrades = 0;

        foreach (var ent in clist)
        {
            upgrades += ent.Count * ent.Upgrades;
        }
        return (count, wins, count == 0 ? 0 : upgrades / count);
    }

    public void PresentDiff(BuildRatingRequest requestA, BuildRatingResponse responseA, BuildRatingRequest requestB, BuildRatingResponse responseB)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{requestA.Interest} vs {requestA.Vs}");
        sb.AppendLine($"Rating Range {requestA.FromRating} - {requestA.ToRating}    | {requestB.FromRating} - {requestB.ToRating}");
        sb.AppendLine("------------------------------------------------------------------");

        List<string> sumsB = responseB.Units.Select(s => s.Name).ToList();

        foreach (var sumA in responseA.Units.OrderBy(o => o.Name))
        {
            var sumB = responseB.Units.FirstOrDefault(f => f.Name == sumA.Name);
            if (sumB == null)
            {
                sb.AppendLine($"{sumA.Name}: {sumA.Avg}     | {sumA.Name}: 0");
            }
            else
            {
                sb.AppendLine($"{sumA.Name}: {sumA.Avg}     | {sumB.Name}: {sumB.Avg}");
                sumsB.Remove(sumB.Name);
            }
        }

        if (sumsB.Any())
        {
            foreach (var nameB in sumsB)
            {
                var sumB = responseB.Units.FirstOrDefault(f => f.Name == nameB);
                if (sumB == null)
                {
                    continue;
                }
                sb.AppendLine($"{nameB}: 0     | {nameB}: {sumB.Avg}");
            }
        }
        sb.AppendLine("------------------------------------------------------------------");
        sb.AppendLine($"Games {responseA.Count} wr: {responseA.Winrate}%   | Games {responseB.Count} wr: {responseB.Winrate}% ");
        Console.WriteLine(sb.ToString());
    }
}


internal record SumHelper
{
    public string Name { get; set; } = string.Empty;
    public int Sum { get; set; }
    public long Upgrades { get; set; }
}

