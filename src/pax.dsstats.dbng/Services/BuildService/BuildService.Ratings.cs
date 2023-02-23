

using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class BuildService
{
    public async Task<BuildRatingResponse> GetBuildByRating(BuildRatingRequest request, CancellationToken token = default)
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
             select rp
            : from rp in context.ReplayPlayers
              from sp in rp.Spawns
              from su in sp.Units
              where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                  && rp.ReplayPlayerRatingInfo.Rating >= request.FromRating
                  && rp.ReplayPlayerRatingInfo.Rating < request.ToRating
                  && rp.Race == request.Interest && rp.OppRace == request.Vs
                  && rp.Replay.GameTime >= start && rp.Replay.GameTime < end
                  && sp.Breakpoint == request.Breakpoint

              select rp;

        replayPlayers = replayPlayers.Distinct();

        var cgroup = from rp in replayPlayers
                     group rp by rp.PlayerResult into g
                     select new
                     {
                         g.Key,
                         Count = g.Count()
                     };

        var clist = await cgroup.ToListAsync(token);

        double count = clist.Select(s => s.Count).Sum();
        double wins = clist.FirstOrDefault(f => f.Key == PlayerResult.Win)?.Count ?? 0;

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

        var list = await group.ToListAsync(token);

        return new()
        {
            Count = (int)count,
            Winrate = count == 0 ? 0 : Math.Round(wins * 100.0 / count, 2),
            Units = list.Select(s => new BuildRatingUnit()
            {
                Name = s.Name,
                Avg = Math.Round(s.Sum / count, 2),
            }).ToList(),
        };
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
}

