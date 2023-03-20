
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetCmdrReplaysCount(CmdrInfosRequest request, CancellationToken token = default)
    {
        var replays = GetCmdrInfosRequestReplays(request);
        return await replays.CountAsync(token);
    }

    public async Task<List<ReplayCmdrListDto>> GetCmdrReplays(CmdrInfosRequest request, CancellationToken token = default)
    {
        var replays = GetCmdrInfosRequestReplays(request);

        return await replays
            .OrderByDescending(o => o.GameTime)
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<ReplayCmdrListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    private IQueryable<Replay> GetCmdrInfosRequestReplays(CmdrInfosRequest request)
    {
        if (request.ToonId > 0)
        {
            return GetCmdrInfosRequestToonIdReplays(request);
        }

        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);
        if (toDate == DateTime.Today)
        {
            toDate = toDate.AddDays(2);
        }

        var replayRatings = from rr in context.ReplayRatings
                      from rpr in rr.RepPlayerRatings
                      where rr.RatingType == request.RatingType
                        && rr.Replay.GameTime >= fromDate
                        && rr.Replay.GameTime < toDate
                      select rr;

        if (request.WithoutLeavers)
        {
            replayRatings = replayRatings
                .Where(x => x.LeaverType == LeaverType.None);
        }

        if (request.MinExp2Win > 0)
        {
            replayRatings = replayRatings.Where(x => x.ExpectationToWin >= request.MinExp2Win);
        }

        if (request.MaxExp2Win > 0)
        {
            replayRatings = replayRatings.Where(x => x.ExpectationToWin <= request.MaxExp2Win);
        }

        if (request.Interest != Commander.None)
        {
            replayRatings = replayRatings
                .Where(x => x.Replay.CommandersTeam1.Contains($"|{(int)request.Interest}|")
                     || x.Replay.CommandersTeam2.Contains($"|{(int)request.Interest}|"));
        }

        var replays = from rr in replayRatings
                      select rr.Replay;

        return replays.Distinct();
    }

    private IQueryable<Replay> GetCmdrInfosRequestToonIdReplays(CmdrInfosRequest request)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);
        if (toDate == DateTime.Today)
        {
            toDate = toDate.AddDays(2);
        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replays = request.Interest == Commander.None ?
                         from rp in context.ReplayPlayers
                          where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                            && rp.Replay.GameTime >= fromDate
                            && rp.Replay.GameTime < toDate
                            && rp.Player.ToonId == request.ToonId
                          select rp.Replay
                        : from rp in context.ReplayPlayers
                          where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                            && rp.Replay.GameTime >= fromDate
                            && rp.Replay.GameTime < toDate
                            && rp.Player.ToonId == request.ToonId
                            && rp.Race == request.Interest
                          select rp.Replay;

        if (request.WithoutLeavers)
        {
            replays = replays
                .Where(x => x.ReplayRatingInfo.LeaverType == LeaverType.None);
        }

        if (request.MinExp2Win > 0)
        {
            replays = replays.Where(x => x.ReplayRatingInfo.ExpectationToWin >= request.MinExp2Win);
        }

        if (request.MaxExp2Win > 0)
        {
            replays = replays.Where(x => x.ReplayRatingInfo.ExpectationToWin <= request.MaxExp2Win);
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return replays;
    }
}

