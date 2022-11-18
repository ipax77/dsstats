using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<StatsResponse> GetSynergy(StatsRequest request)
    {
        var replays = GetCustomRequestReplays(request);

        var synergy = request.Uploaders ?
                        from r in replays
                        from rp1 in r.ReplayPlayers
                        from rp2 in r.ReplayPlayers
                        where rp1.IsUploader && rp1.Race == request.Interest
                        where rp2.Team == rp1.Team && rp2.ReplayPlayerId != rp1.ReplayPlayerId
                        group new { r, rp1 } by rp2.Race into g
                        select new StatsResponseItem
                        {
                            Label = g.Key.ToString(),
                            Matchups = g.Count(),
                            Wins = g.Count(c => c.rp1.PlayerResult == PlayerResult.Win)
                        }
                      : from r in replays
                        from rp1 in r.ReplayPlayers
                        from rp2 in r.ReplayPlayers
                        where rp1.Race == request.Interest
                        where rp2.Team == rp1.Team && rp2.ReplayPlayerId != rp1.ReplayPlayerId
                        group new { r, rp1 } by rp2.Race into g
                        select new StatsResponseItem
                        {
                            Label = g.Key.ToString(),
                            Matchups = g.Count(),
                            Wins = g.Count(c => c.rp1.PlayerResult == PlayerResult.Win)
                        };
        var items = await synergy.ToListAsync();

        if (!items.Any())
        {
            return new StatsResponse()
            {
                Request = request,
                Items = new List<StatsResponseItem>()
            };
        }

        if ((int)request.Interest > 3)
        {
            items = items.Where(x => (int)x.Cmdr > 3).ToList();
        }
        else
        {
            items = items.Where(x => (int)x.Cmdr <= 3).ToList();
        }

        return new StatsResponse()
        {
            Request = request,
            Items = items,
            CountResponse = await GetCount(request),
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }
}
