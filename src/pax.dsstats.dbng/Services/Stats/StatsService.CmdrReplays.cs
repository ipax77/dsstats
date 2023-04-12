
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<int> GetCmdrReplaysCount(CmdrInfosRequest request, CancellationToken token = default)
    {
        var replays = GetCmdrInfosRequestReplays(request);
        replays = FilterReplays(request, replays);

        return await replays.CountAsync(token);
    }

    public async Task<List<ReplayCmdrListDto>> GetCmdrReplays(CmdrInfosRequest request, CancellationToken token = default)
    {
        var replays = GetCmdrInfosRequestReplays(request);

        replays = FilterReplays(request, replays);
        replays = SortReplays(request, replays);

        return await replays
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<ReplayCmdrListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }

    private IQueryable<Replay> FilterReplays(CmdrInfosRequest request, IQueryable<Replay> replays)
    {
        if (String.IsNullOrEmpty(request.SearchNames) && String.IsNullOrEmpty(request.SearchCmdrs))
        {
            return replays;
        }

        var searchStrings = request.SearchCmdrs?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList() ?? new List<string>();
        var searchPlayers = request
            .SearchNames?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList() ?? new List<string>();
        var searchCmdrs = searchStrings.SelectMany(s => GetSearchCommanders(s)).Distinct().ToList();

        if (request.LinkCmdrName)
        {
            return LinkReplays(replays, searchCmdrs, searchPlayers);
        }

        replays = FilterCommanders(replays, searchCmdrs);
        replays = FilterNames(replays, searchPlayers, false);


        return replays;
    }

    private IQueryable<Replay> LinkReplays(IQueryable<Replay> replays, List<Commander> searchCmdrs, List<string> searchPlayers)
    {
        int links = Math.Min(searchCmdrs.Count, searchPlayers.Count);
        if (links > 0)
        {
            for (int i = 0; i < links; i++)
            {
                var cmdr = searchCmdrs[i];
                var name = searchPlayers[i].ToUpper();
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Race == cmdr && a.Name.ToUpper().Contains(name)));
            }
        }

        if (searchCmdrs.Count > links)
        {
            replays = FilterCommanders(replays, searchCmdrs.Skip(links).ToList());
        }

        if (searchPlayers.Count > links)
        {
            replays = FilterNames(replays, searchPlayers.Skip(links).ToList());
        }

        return replays;
    }

    private IQueryable<Replay> FilterCommanders(IQueryable<Replay> replays, List<Commander> searchCmdrs)
    {
        foreach (var cmdr in searchCmdrs)
        {
            replays = replays
                .Where(x => x.CommandersTeam1.Contains($"|{(int)cmdr}|")
                    || x.CommandersTeam2.Contains($"|{(int)cmdr}|")
                );
        }
        return replays;
    }

    private IQueryable<Replay> FilterNames(IQueryable<Replay> replays, List<string> searchPlayers, bool withEvent = false)
    {
        foreach (var player in searchPlayers)
        {
            if (withEvent)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                replays = replays
                    .Where(x => x.ReplayEvent.WinnerTeam.ToUpper().Contains(player.ToUpper())
                    || x.ReplayEvent.RunnerTeam.ToUpper().Contains(player.ToUpper())
                    || x.ReplayPlayers.Any(a => a.Name.ToUpper().Contains(player.ToUpper())));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Name.ToUpper().Contains(player.ToUpper())));
            }
        }
        return replays;
    }

    private List<Commander> GetSearchCommanders(string searchString)
    {
        List<Commander> cmdrs = new();
        foreach (var cmdr in Enum.GetValues(typeof(Commander)).Cast<Commander>())
        {
            if (cmdr.ToString().ToUpper().Contains(searchString.ToUpper()))
            {
                //commanders.Add($"|{(int)cmdr}|");
                cmdrs.Add(cmdr);
            }
        }
        return cmdrs;
    }

    private IQueryable<Replay> SortReplays(CmdrInfosRequest request, IQueryable<Replay> replays)
    {
        if (!request.Orders.Any())
        {
            return replays.OrderBy(o => o.ReplayId);
        }

        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                replays = replays.AppendOrderBy(order.Property);
            }
            else
            {
                replays = replays.AppendOrderByDescending(order.Property);
            }
        }
        return replays;
    }

    private IQueryable<Replay> GetCmdrInfosRequestReplays(CmdrInfosRequest request)
    {
        if (request.PlayerId != null)
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
                            && rp.Player.ToonId == request.PlayerId.ToonId
                            && rp.Player.RealmId == request.PlayerId.RealmId
                            && rp.Player.RegionId == request.PlayerId.RegionId
                         select rp.Replay
                        : from rp in context.ReplayPlayers
                          where rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                            && rp.Replay.GameTime >= fromDate
                            && rp.Replay.GameTime < toDate
                            && rp.Player.ToonId == request.PlayerId.ToonId
                            && rp.Player.RealmId == request.PlayerId.RealmId
                            && rp.Player.RegionId == request.PlayerId.RegionId
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

