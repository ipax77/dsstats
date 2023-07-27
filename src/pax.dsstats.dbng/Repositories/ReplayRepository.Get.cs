
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.Xml.Linq;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository
{
    public async Task<int> GetReplaysCountNg(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestReplaysNg(request);
        replays = GetAdvReplays(replays, request);
        return await replays.CountAsync(token);
    }

    public async Task<ICollection<ReplayListDto>> GetReplaysNg(ReplaysRequest request, CancellationToken token = default)
    {
        if (request.Take < 0 || request.Skip < 0)
        {
            return new List<ReplayListDto>();
        }

        var replays = GetRequestReplaysNg(request);
        replays = GetAdvReplays(replays, request);

        replays = SortReplays(request, replays);

        if (token.IsCancellationRequested)
        {
            return new List<ReplayListDto>();
        }

        if (request.WithMmrChange && (!String.IsNullOrEmpty(request.SearchPlayers) || Data.IsMaui))
        {
            return await GetReplaysWithMmr(request, replays, token);
        }
        else
        {
            return await replays
                .Skip(request.Skip)
                .Take(request.Take)
                .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);
        }
    }

    private async Task<ICollection<ReplayListDto>> GetReplaysWithMmr(ReplaysRequest request, IQueryable<Replay> replays, CancellationToken token)
    {
        var mmrlist = await replays
             .Skip(request.Skip)
             .Take(request.Take)
             .AsNoTracking()
             .ProjectTo<ReplayListRatingDto>(mapper.ConfigurationProvider)
             .ToListAsync(token);

        if (request.ToonId > 0)
        {
            for (int i = 0; i < mmrlist.Count; i++)
            {
                var rep = mmrlist[i];

                if (rep.ReplayRatingInfo == null)
                {
                    continue;
                }

                var pl = rep.ReplayPlayers.FirstOrDefault(f => f.Player.ToonId == request.ToonId);
                if (pl != null)
                {
                    var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                    rep.MmrChange = rat?.RatingChange ?? 0;
                    rep.Commander = pl.Race;
                }
            }
        }
        else if (Data.IsMaui && String.IsNullOrEmpty(request.SearchPlayers))
        {
            for (int i = 0; i < mmrlist.Count; i++)
            {
                var rep = mmrlist[i];

                if (rep.ReplayRatingInfo == null)
                {
                    continue;
                }

                var pl = rep.ReplayPlayers.FirstOrDefault(f => f.GamePos == rep.PlayerPos);
                if (pl != null)
                {
                    var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                    rep.MmrChange = rat?.RatingChange ?? 0;
                    rep.Commander = pl.Race;
                }
            }
        }
        else
        {
            string? interest = request.SearchPlayers?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            for (int i = 0; i < mmrlist.Count; i++)
            {
                var rep = mmrlist[i];

                if (rep.ReplayRatingInfo == null)
                {
                    continue;
                }

                var pl = rep.ReplayPlayers.FirstOrDefault(f => f.Name == interest);
                if (pl != null)
                {
                    var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                    rep.MmrChange = rat?.RatingChange ?? 0;
                    rep.Commander = pl.Race;
                }
            }
        }
        mmrlist.ForEach(f =>
        {
            f.ReplayPlayers.Clear();
            f.ReplayRatingInfo = null;
        });
        return mmrlist.Cast<ReplayListDto>().ToList();
    }

    private IQueryable<Replay> GetRequestReplaysNg(ReplaysRequest request, bool withEvent = false)
    {
        IQueryable<Replay> replays;

        // searchString only
        if (!String.IsNullOrEmpty(request.SearchString)
            && String.IsNullOrEmpty(request.SearchPlayers)
            && request.ToonId == 0)
        {
            var cmdrs = GetSearchCommandersNg(request.SearchString);

            if (!cmdrs.Any())
            {
                replays = context.Replays
                    .Where(x => x.GameTime > new DateTime(2018, 1, 1));
            }
            else if (cmdrs.Count == 1)
            {
                var cmdr = cmdrs[0];
                replays = from rp in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Race == cmdr
                          select rp.Replay;
            }
            else
            {
                replays = context.Replays
                    .Where(x => x.GameTime > new DateTime(2018, 1, 1));

                replays = FilterCommanders(replays, cmdrs);
            }
        }
        // searchPlayers only
        else if (String.IsNullOrEmpty(request.SearchString)
            && !String.IsNullOrEmpty(request.SearchPlayers)
            && request.ToonId == 0)
        {
            var names = GetSearchNamesNg(request.SearchPlayers);

            if (!names.Any())
            {
                replays = context.Replays
                    .Where(x => x.GameTime > new DateTime(2018, 1, 1));

            }
            else if (names.Count == 1)
            {
                var name = names[0];
                replays = from rp in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Name == name
                          select rp.Replay;
            } else
            {
                var name = names[0];
                replays = from rp in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Name == name
                          select rp.Replay;
                for (int i = 1; i < names.Count; i++)
                {
                    var iname = names[i];
                    replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Name == iname));
                }
            }
        }
        // searchPlayers and ToonId
        else if (!String.IsNullOrEmpty(request.SearchPlayers)
            && request.ToonId != 0)
        {
            if (request.ToonIdWith != 0)
            {
                replays = from rp in context.ReplayPlayers
                          from rp1 in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Player.ToonId == request.ToonId
                            && rp1.ReplayId == rp.ReplayId
                            && rp1.Team == rp.Team
                            && rp1.Player.ToonId == request.ToonIdWith
                          select rp.Replay;
            }
            else if (request.ToonIdVs != 0)
            {
                replays = from rp in context.ReplayPlayers
                          from rp1 in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Player.ToonId == request.ToonId
                            && rp1.ReplayId == rp.ReplayId
                            && rp1.Team != rp.Team
                            && rp1.Player.ToonId == request.ToonIdVs
                          select rp.Replay;
            }
            else
            {

                replays = from rp in context.ReplayPlayers
                          where rp.Replay.GameTime > new DateTime(2018, 1, 1)
                            && rp.Player.ToonId == request.ToonId
                          select rp.Replay;
            }

            if (!String.IsNullOrEmpty(request.SearchString))
            {
                var cmdrs = GetSearchCommandersNg(request.SearchString);
                var names = GetSearchNamesNg(request.SearchPlayers);

                if (request.LinkSearch)
                {
                    replays = LinkReplays(replays, cmdrs, names);
                }
                else
                {
                    replays = FilterCommanders(replays, cmdrs);
                }
            }
        }
        // searchString && searchPlayers
        else if (!String.IsNullOrEmpty(request.SearchString)
            && !String.IsNullOrEmpty(request.SearchPlayers)
            && request.ToonId == 0)
        {
            var names = GetSearchNamesNg(request.SearchPlayers);
            var cmdrs = GetSearchCommandersNg(request.SearchString);

            replays = context.Replays
                .Where(x => x.GameTime > new DateTime(2018, 1, 1));

            if (request.LinkSearch)
            {
                replays = LinkReplays(replays, cmdrs, names);
            }
            else
            {
                replays = FilterCommanders(replays, cmdrs);
                replays = FilterNames(replays, names, withEvent);
            }
        }
        else
        {
            replays = context.Replays
                .Where(x => x.GameTime > new DateTime(2018, 1, 1));

        }

        if (request.DefaultFilter)
        {
            replays = replays.Where(x => x.DefaultFilter);
        }

        if (request.PlayerCount != 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.ResultAdjusted)
        {
            replays = replays.Where(x => x.ResultCorrected);
        }

        if (request.TEMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        return replays;
    }

    private List<string> GetSearchNamesNg(string searchPlayers)
    {
        return searchPlayers
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct()
            .ToList();
    }

    private List<Commander> GetSearchCommandersNg(string searchString)
    {
        var searchStrings = searchString
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        List<Commander> cmdrs = new();

        foreach (var cmdrString in searchStrings)
        {
            if (Enum.TryParse(typeof(Commander), cmdrString, true, out var cmdrObj)
                && cmdrObj is Commander cmdr)
            {
                cmdrs.Add(cmdr);
            }
        }
        return cmdrs;
    }
}