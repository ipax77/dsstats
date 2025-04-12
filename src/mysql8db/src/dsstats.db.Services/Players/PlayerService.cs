using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.shared;
using dsstats.shared8;
using dsstats.shared8.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace dsstats.db.Services.Players;

public class PlayerService(DsstatsContext context, IMapper mapper, ILogger<PlayerService> logger) : IPlayerService
{
    private readonly bool DEBUG = false;
    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerId playerId, RatingNgType ratingNgType, CancellationToken token)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var ratings = await context.PlayerRatings
            .Where(x => x.Player!.RegionId == playerId.RegionId
                && x.Player.RealmId == playerId.RealmId
                && x.Player.ToonId == playerId.ToonId)
            .ProjectTo<dsstats.shared8.PlayerRatingDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        var avgGains = await GetPlayerIdPlayerCmdrAvgGain(playerId, ratingNgType, TimePeriod.Past90Days, token);

        sw.Stop();
        logger.LogInformation("GetPlayerStats took {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        return new PlayerStatsResponse()
        {
            RatingType = ratingNgType,
            PlayerRatings = ratings.Where(x => x.RatingType == ratingNgType).ToList(),
            PlayerCmdrAvgGains = avgGains
        };
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId,
                                                                            RatingNgType ratingType,
                                                                            TimePeriod timePeriod,
                                                                            CancellationToken token)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(timePeriod);
        bool noEnd = endTime < DateTime.Today.AddDays(-2);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && r.GameTime > startTime
                        && (noEnd || rp.Replay!.GameTime < endTime)
                        && rr.RatingType == ratingType
                        && rpr.RatingType == ratingType
                    group new { rp, rpr } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = (double)Math.Round(g.Average(a => a.rpr.Change), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                    };

        var items = await group.ToListAsync(token);
        return items;
    }
}
