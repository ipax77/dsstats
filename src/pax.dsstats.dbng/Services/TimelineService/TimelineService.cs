
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared.Services;

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
        // SetStrength(data);
        // SetStrengthPerMonth(data);
        //SetWeightedStrengthPerMonth(data);
        SetWeightedStrength(data);

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

        var nRatings = Normalize.NormalizeList(data.Select(s => s.AvgRating).ToList());
        var nGains = Normalize.NormalizeList(data.Select(s => s.AvgGain).ToList());

        for (int i = 0; i < data.Count; i++)
        {
            data[i].Strength = (nGains[i] + nRatings[i]) / 2;
        }

        var maxStrenght = data.Max(d => d.Strength);

        if (maxStrenght == 0)
        {
            maxStrenght = 1;
        }
    }

    private void SetStrengthPerMonth(List<TimelineEnt> data)
    {
        if (!data.Any())
        {
            return;
        }

        var cmdrs = data.Select(s => s.Commander).Distinct().ToList();

        foreach (var cmdr in cmdrs)
        {
            var cmdrData = data.Where(x => x.Commander == cmdr).ToList();

            var nRatings = Normalize.NormalizeList(cmdrData.Select(s => s.AvgRating).ToList());
            var nGains = Normalize.NormalizeList(cmdrData.Select(s => s.AvgGain).ToList());

            for (int i = 0; i < cmdrData.Count; i++)
            {
                cmdrData[i].Strength = (nGains[i] + nRatings[i]) / 2;
            }


            var nStrength = Normalize.NormalizeList(cmdrData.Select(s => s.Strength).ToList());
            for (int i = 0; i < cmdrData.Count; i++)
            {
                cmdrData[i].Strength = Math.Round(nStrength[i] * 100.0, 2);
            }
        }
    }

    private void SetWeightedStrengthPerMonth(List<TimelineEnt> data)
    {
        if (!data.Any())
        {
            return;
        }

        double weightMatchups = 1;
        double weightWinrate = 5;
        double weightRating = 8;
        double weightGain = 8;

        var cmdrs = data.Select(s => s.Commander).Distinct().ToList();

        foreach (var cmdr in cmdrs)
        {
            var cmdrData = data.Where(x => x.Commander == cmdr).ToList();

            double minMatchups = cmdrData.Min(m => m.Count);
            double maxMatchups = cmdrData.Max(m => m.Count);

            var winrates = cmdrData.Select(s => s.Count == 0 ? 0 : s.Wins * 100.0 / (double)s.Count).ToList();
            double minWinrate = winrates.Min();
            double maxWinrate = winrates.Max();

            double minRating = cmdrData.Min(m => m.AvgRating);
            double maxRating = cmdrData.Max(m => m.AvgRating);

            double minGain = cmdrData.Min(m => m.AvgGain);
            double maxGain = cmdrData.Max(m => m.AvgGain);

            foreach (var item in cmdrData)
            {
                var normalizedMatchups = (item.Count - minMatchups) / (maxMatchups - minMatchups);
                var normalizedWinrate = (item.Wins * 100.0 / (double)item.Count - minWinrate) / (maxWinrate - minWinrate);
                var normalizedRating = (item.AvgRating - minRating) / (maxRating - minRating);
                var normalizedGain = (item.AvgGain - minGain) / (maxGain - minGain);

                item.Strength =
                      weightMatchups * normalizedMatchups
                    + weightWinrate * normalizedWinrate
                    + weightRating * normalizedRating
                    + weightGain * normalizedGain;
            }
        }
    }

    private void SetWeightedStrength(List<TimelineEnt> data)
    {
        if (!data.Any())
        {
            return;
        }

        double weightMatchups = 1;
        double weightWinrate = 5;
        double weightRating = 8;
        double weightGain = 8;


        double minMatchups = data.Min(m => m.Count);
        double maxMatchups = data.Max(m => m.Count);

        var winrates = data.Select(s => s.Count == 0 ? 0 : s.Wins * 100.0 / (double)s.Count).ToList();
        double minWinrate = winrates.Min();
        double maxWinrate = winrates.Max();

        double minRating = data.Min(m => m.AvgRating);
        double maxRating = data.Max(m => m.AvgRating);

        double minGain = data.Min(m => m.AvgGain);
        double maxGain = data.Max(m => m.AvgGain);

        foreach (var item in data)
        {
            var normalizedMatchups = (item.Count - minMatchups) / (maxMatchups - minMatchups);
            var normalizedWinrate = (item.Wins * 100.0 / (double)item.Count - minWinrate) / (maxWinrate - minWinrate);
            var normalizedRating = (item.AvgRating - minRating) / (maxRating - minRating);
            var normalizedGain = (item.AvgGain - minGain) / (maxGain - minGain);

            item.Strength = 0
                //   weightMatchups * normalizedMatchups
                // + weightWinrate * normalizedWinrate
                // + weightRating * normalizedRating
                + weightGain * normalizedGain;
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

