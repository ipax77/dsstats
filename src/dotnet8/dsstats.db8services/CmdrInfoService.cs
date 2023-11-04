using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class CmdrInfoService : ICmdrInfoService
{
    private readonly ReplayContext context;

    public CmdrInfoService(ReplayContext context)
    {
        this.context = context;
    }

    public async Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);
        if (toDate == DateTime.Today)
        {
            toDate = toDate.AddDays(2);
        }

        int limit = 10 * 2;
        if (request.RatingType == RatingType.CmdrTE)
        {
            limit = 2 * 2;
        }

        var group = from pr in context.PlayerRatings
                    from rp in pr.Player!.ReplayPlayers
                    join tm in context.ReplayPlayers on new { rp.ReplayId, rp.Team } equals new { tm.ReplayId, tm.Team }
                    join rpr in context.ComboReplayPlayerRatings on tm.ReplayPlayerId equals rpr.ReplayPlayerId
                    where pr.RatingType == request.RatingType
                      && rp.Replay.ComboReplayRating!.RatingType == request.RatingType
                      && rp.Race == request.Interest
                      && rp.Replay.GameTime >= fromDate
                      && rp.Replay.GameTime < toDate
                      && tm.ReplayPlayerId != rp.ReplayPlayerId
                      && (!request.Uploaders || pr.IsUploader)
                    group new { pr.Player, rp, rpr } by new { rp.Player.Name, rp.Player.ToonId, rp.Player.RegionId, rp.Player.RealmId } into g
                    where g.Count() >= limit
                    select new CmdrPlayerInfo()
                    {
                        Name = g.Key.Name,
                        PlayerId = new(g.Key.ToonId, g.Key.RealmId, g.Key.RegionId),
                        Count = g.Count() / 2,
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win) / 2,
                        AvgRating = Math.Round(g.Average(s => s.rp.ComboReplayPlayerRating!.Rating)),
                        AvgGain = Math.Round(g.Average(a => a.rp.ComboReplayPlayerRating!.Change), 2),
                        TeamRating = Math.Round(g.Average(a => a.rpr.Rating))
                    };


        var data = await group
            .OrderByDescending(o => o.Count)
            .ToListAsync(token);
        return data;
    }
}
