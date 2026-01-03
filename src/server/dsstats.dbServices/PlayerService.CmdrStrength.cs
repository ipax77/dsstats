using dsstats.db;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<CmdrStrenghtResponse> GetCmdrPlayerInfos(CmdrStrenghtRequest request, CancellationToken token = default)
    {
        try
        {
            var memKey = request.GenMemKey();
            if (memoryCache.TryGetValue(memKey, out var info)
                && info is CmdrStrenghtResponse response)
            {
                return response;
            }
            var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);

            int limit = 10 * 2;
            if (request.RatingType == RatingType.CommandersTE || request.RatingType == RatingType.StandardTE)
            {
                limit = 2 * 2;
            }

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            var group = from pr in context.PlayerRatings
                        from rp in pr.Player!.ReplayPlayers
                        join tm in context.ReplayPlayers on new { rp.ReplayId, rp.TeamId } equals new { tm.ReplayId, tm.TeamId }
                        join rr in context.ReplayRatings on tm.ReplayId equals rr.ReplayId
                        join rpr in context.ReplayPlayerRatings on new { tm.ReplayPlayerId, rr.ReplayRatingId } equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
                        where pr.RatingType == request.RatingType
                          && rr.RatingType == request.RatingType
                          && rp.Race == request.Interest
                          && rr.Replay!.Gametime >= timeInfo.Start
                          && (!timeInfo.HasEnd || rr.Replay.Gametime < timeInfo.End)
                          && tm.ReplayPlayerId != rp.ReplayPlayerId
                        group new { pr.Player, rp, rpr } by new { rp.Player!.ToonId } into g
                        where g.Count() >= limit
                        select new CmdrStrengthItem()
                        {
                            ToonId = new() { Id = g.Key.ToonId.Id, Realm = g.Key.ToonId.Realm, Region = g.Key.ToonId.Region },
                            Count = g.Count() / 2,
                            Wins = g.Count(c => c.rp.Result == PlayerResult.Win) / 2,
                            AvgRating = Math.Round(g.Average(s => s.rpr.RatingBefore + s.rpr.RatingDelta)),
                            AvgGain = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                            TeamRating = Math.Round(g.Average(a => a.rpr.RatingBefore))
                        };


            var data = await group
                .OrderByDescending(o => o.Count)
                .ToListAsync(token);

            foreach (var item in data)
            {
                item.Name = importService.GetPlayerName(item.ToonId);
            }

            response = new()
            {
                Items = data
            };
            memoryCache.Set(memKey, response, TimeSpan.FromHours(12));
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetCmdrPlayerInfos");
            throw;
        }
    }
}
