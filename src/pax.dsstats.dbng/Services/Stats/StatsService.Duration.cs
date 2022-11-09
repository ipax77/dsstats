using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    private async Task<StatsResponse> GetDuration(StatsRequest request)
    {
        var replays = GetCustomRequestReplays(request);

        var list = request.Uploaders ?
             from r in replays
             from rp in r.ReplayPlayers
             where rp.Race == request.Interest && rp.IsUploader
             select new
             {
                 r.Duration,
                 rp.PlayerResult,
             }
            :
            from r in replays
            from rp in r.ReplayPlayers
            where rp.Race == request.Interest
            select new
            {
                r.Duration,
                rp.PlayerResult,
            };

        var group = list.GroupBy(g => new
        {
            Min8 = g.Duration > 300 && g.Duration <= 480,
            Min11 = g.Duration > 480 && g.Duration <= 660,
            Min14 = g.Duration > 660 && g.Duration <= 840,
            Min17 = g.Duration > 840 && g.Duration <= 1020,
            Min21 = g.Duration > 1020 && g.Duration <= 1260,
            Min24 = g.Duration > 1260 && g.Duration <= 1440,
            Min27 = g.Duration > 1440 && g.Duration <= 1620,
            Min30 = g.Duration > 1620 && g.Duration <= 1800,
            Min33 = g.Duration > 1800,
        })
            .Select(s => new
            {
                Key = s.Key,
                Count = s.Count(),
                Wins = s.Count(c => c.PlayerResult == PlayerResult.Win)
            });


        var groupData = await group.ToListAsync();

        var data = groupData.Select(s => new StatsResponseItem()
        {
            Label = s.Key switch
            {
                _ when s.Key.Min8 => "Min08",
                _ when s.Key.Min11 => "Min11",
                _ when s.Key.Min14 => "Min14",
                _ when s.Key.Min17 => "Min17",
                _ when s.Key.Min21 => "Min21",
                _ when s.Key.Min24 => "Min24",
                _ when s.Key.Min27 => "Min27",
                _ when s.Key.Min30 => "Min30",
                _ => "Min33+"
            },
            Matchups = s.Count,
            Wins = s.Wins
        }).OrderBy(o => o.Label).ToList();

        return new StatsResponse()
        {
            Request = request,
            Items = data,
            CountResponse = await GetCount(request),
            AvgDuration = !data.Any() ? 0 : Convert.ToInt32(data.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }

}
