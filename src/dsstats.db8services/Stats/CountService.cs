using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public class CountService : ICountService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public CountService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<CountResponse> GetCount(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey("Count");
        if (!memoryCache.TryGetValue(memKey, out CountResponse? response)
            || response is null)
        {
            response = await ProduceCount(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(6));
        }
        return response;
    }

    private async Task<CountResponse> ProduceCount(StatsRequest request, CancellationToken token)
    {
        var response = await GetReplayCountsRepsonse(request, token);
        response.CountEnts = await GetMatchupEnts(request, token);
        return response;
    }

    private async Task<List<CountEnt>> GetMatchupEnts(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        List<GameMode> gameModes = new();

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            gameModes.Add(GameMode.Standard);
        }
        else
        {
            gameModes.Add(GameMode.Commanders);
            gameModes.Add(GameMode.CommandersHeroic);
        }

        bool noTournamentEdition = true;
        if (request.RatingType == RatingType.CmdrTE || request.RatingType == RatingType.StdTE)
        {
            noTournamentEdition = false;
        }

        var limits = request.GetFilterLimits();

        bool withLimits = true;
        if (limits.FromRating == 0 && limits.ToRating == 0
            && limits.FromExp2Win == 0 && limits.ToExp2Win == 0)
        {
            withLimits = false;
        }

        var query = withLimits ?
                        request.ComboRating ?
                        from r in context.Replays
                        from rp in r.ReplayPlayers
                        join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                        join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                        where r.GameTime >= fromDate
                            && (toDate > tillDate || r.GameTime <= toDate)
                            && gameModes.Contains(r.GameMode)
                            && rr.RatingType == request.RatingType
                            && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                            && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                            && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                            && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                            && (noTournamentEdition == true || r.TournamentEdition == true)
                        group new { r, rp } by rp.Race into g
                        select new CountEnt()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                        }
                        : from r in context.Replays
                          from rp in r.ReplayPlayers
                          join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                          join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                          where r.GameTime >= fromDate
                              && (toDate > tillDate || r.GameTime <= toDate)
                              && gameModes.Contains(r.GameMode)
                              && rr.RatingType == request.RatingType
                              && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                              && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                              && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                              && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                              && (noTournamentEdition == true || r.TournamentEdition == true)
                          group new { r, rp } by rp.Race into g
                          select new CountEnt()
                          {
                              Commander = g.Key,
                              Matchups = g.Count(),
                              Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                          }
                  : from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime >= fromDate
                        && (toDate > tillDate || r.GameTime <= toDate)
                        && gameModes.Contains(r.GameMode)
                        && (noTournamentEdition == true || r.TournamentEdition == true)
                    group new { r, rp } by rp.Race into g
                    select new CountEnt()
                    {
                        Commander = g.Key,
                        Matchups = g.Count(),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };

        var list = await query.ToListAsync(token);

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            return list.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
        else
        {
            return list.Where(x => (int)x.Commander > 3).ToList();
        }
    }

    private async Task<CountResponse> GetReplayCountsRepsonse(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        List<GameMode> gameModes = new();

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            gameModes.Add(GameMode.Standard);
        }
        else
        {
            gameModes.Add(GameMode.Commanders);
            gameModes.Add(GameMode.CommandersHeroic);
        }

        bool noTournamentEdition = true;
        if (request.RatingType == RatingType.CmdrTE || request.RatingType == RatingType.StdTE)
        {
            noTournamentEdition = false;
        }

        var limits = request.GetFilterLimits();

        bool withLimits = true;
        if (limits.FromRating == 0 && limits.ToRating == 0
            && limits.FromExp2Win == 0 && limits.ToExp2Win == 0)
        {
            withLimits = false;
        }

        var query = withLimits ?
                        request.ComboRating ?
                        from r in context.Replays
                        from rp in r.ReplayPlayers
                        join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                        join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                        where r.GameTime >= fromDate
                            && (toDate > tillDate || r.GameTime <= toDate)
                            && gameModes.Contains(r.GameMode)
                            && rr.RatingType == request.RatingType
                            && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                            && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                            && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                            && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                            && (noTournamentEdition == true || r.TournamentEdition == true)
                        group r by new { Leaver = r.Maxleaver > 90, NoQuit = r.WinnerTeam > 0 } into g
                        select new
                        {
                            g.Key.Leaver,
                            g.Key.NoQuit,
                            Matchups = g.Count(),
                            Replays = g.Select(s => s.ReplayId).Distinct().Count(),
                            AvgDuration = (int)g.Average(a => a.Duration)
                        }
                        : from r in context.Replays
                          from rp in r.ReplayPlayers
                          join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                          join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                          where r.GameTime >= fromDate
                              && (toDate > tillDate || r.GameTime <= toDate)
                              && gameModes.Contains(r.GameMode)
                              && rr.RatingType == request.RatingType
                              && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                              && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                              && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                              && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                              && (noTournamentEdition == true || r.TournamentEdition == true)
                          group r by new { Leaver = r.Maxleaver > 90, NoQuit = r.WinnerTeam > 0 } into g
                          select new
                          {
                              g.Key.Leaver,
                              g.Key.NoQuit,
                              Matchups = g.Count(),
                              Replays = g.Select(s => s.ReplayId).Distinct().Count(),
                              AvgDuration = (int)g.Average(a => a.Duration)
                          }
                  : from r in context.Replays
                    where r.GameTime >= fromDate
                        && (toDate > tillDate || r.GameTime <= toDate)
                        && gameModes.Contains(r.GameMode)
                        && (noTournamentEdition == true || r.TournamentEdition == true)
                    group r by new { Leaver = r.Maxleaver > 90, NoQuit = r.WinnerTeam > 0 } into g
                    select new
                    {
                        g.Key.Leaver,
                        g.Key.NoQuit,
                        Matchups = 0,
                        Replays = g.Count(),
                        AvgDuration = (int)g.Average(a => a.Duration)
                    };

        var result = await query.ToListAsync(token);

        int sum = result.Sum(s => s.Replays);

        return new()
        {
            Replays = sum,
            Matchups = result.Sum(s => s.Matchups),
            LeaverReplays = result.Where(x => x.Leaver).Sum(s => s.Replays),
            Quits = result.Where(x => !x.NoQuit).Sum(s => s.Replays),
            Duration = sum == 0 ? 0 : result.Sum(s => s.Replays * s.AvgDuration) / sum
        };
    }
}

