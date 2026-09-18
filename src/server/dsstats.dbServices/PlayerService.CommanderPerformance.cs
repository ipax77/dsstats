using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<CmdrAvgGainResponse> GetCommandersPerformance(PlayerStatsRequest request, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);

        int playerId = importService.GetPlayerId(request.ToonId);
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, rr.RatingType }
                        equals new { rpr.ReplayPlayerId, rpr.RatingType }
                    where p.PlayerId == playerId
                        && r.Gametime >= timeInfo.Start
                        && (!timeInfo.HasEnd || rp.Replay!.Gametime < timeInfo.End)
                        && rr.RatingType == request.RatingType
                    group new
                    {
                        rp,
                        rpr
                    } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.Result == PlayerResult.Win)
                    };

        var items = await group.ToListAsync(token);
        return new()
        {
            TimePeriod = request.TimePeriod,
            AvgGains = items
        };
    }

}
