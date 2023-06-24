
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.dbng.Services;

public class TimelineService : ITimelineService
{
    private readonly ReplayContext context;

    public TimelineService(ReplayContext context)
    {
        this.context = context;
    }

    public async Task<TimelineResponse> GetTimeline(TimelineRequest request, CancellationToken token = default)
    {
        var data = await GetData(request, token);
        SetStrength(data);

        return new()
        {
            TimeLineEnts = data
        };
    }

    private void SetStrength(List<TimelineEnt> data)
    {
        if (!data.Any())
        {
            return;
        }

        double maxRating = data.Max(d => d.AvgRating);
        double maxGain = data.Max(d => d.AvgGain);

        if (maxRating == 0)
        {
            maxRating = 1;
        }

        if (maxGain == 0)
        {
            maxGain = 1;
        }

        foreach (var ent in data)
        {
            ent.Strength = (ent.AvgGain / maxGain) + (ent.AvgRating / maxRating) / 2;
        }

        var maxStrenght = data.Max(d => d.Strength);

        if (maxStrenght == 0)
        {
            maxStrenght = 1;
        }

        foreach (var ent in data)
        {
            ent.Strength = Math.Round((ent.Strength / maxStrenght) * 100, 2);
        }
    }

    private async Task<List<TimelineEnt>> GetData(TimelineRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        toDate = new DateTime(toDate.Year, toDate.Month, 1);

        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                    group new { r, rp } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
                    select new TimelineEnt
                    {
                        Commander = g.Key.Race,
                        Time = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.RatingChange), 2),
                        Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win)
                    };

        return await group.ToListAsync();
    }
}

