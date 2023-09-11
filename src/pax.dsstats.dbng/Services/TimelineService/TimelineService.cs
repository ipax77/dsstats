
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.dbng.Services;

public class TimelineService : ITimelineService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public TimelineService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<TimelineResponse> GetTimeline(TimelineRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out TimelineResponse response))
        {
            response = await ProduceTimeline(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<TimelineResponse> ProduceTimeline(TimelineRequest request, CancellationToken token = default)
    {
        var data = await GetData(request, token);
        
        return new()
        {
            TimeLineEnts = data,
        };
    }

    public List<TimelineEnt> SetStrength(List<TimelineQueryData> data, TimelineWeights weights)
    {
        if (!data.Any())
        {
            return new();
        }

        //var weightGain = 1.0;
        //var weightRating = 0.1;
        //var weightPlDiff = -0.025;
        //var weightTeamDiff = -0.075;
        //var weightWinrate = 0.02;

        var weightGain = weights.WeightGain;
        var weightRating = weights.WeightRating;
        var weightPlDiff = weights.WeightPlDiff;
        var weightTeamDiff = weights.WeightTeamDiff;
        var weightWinrate = weights.WeightWinrate;

        List<TimelineEnt> ents = new();

        for (int i = 0; i < data.Count; i++)
        {
            var ent = data[i];
            ents.Add(new()
            {
                Commander = (Commander)ent.Race,
                Time = new DateTime(ent.Ryear, ent.Rmonth, 1),
                Count = ent.Count,
                Wins = ent.Wins,
                AvgGain = ent.AvgGain,
                AvgRating = ent.AvgRating,
                Strength = (weightGain * ent.AvgGain)
                    + (weightRating * ent.AvgRating)
                    + (weightPlDiff * (ent.AvgRating - ent.AvgOppRating))
                    + (weightTeamDiff * (ent.AvgTeamRating - ent.AvgOppTeamRating))
                    + (weightWinrate * (ent.Wins * 100.0 / ent.Count))
            });
        }

        var minStrength = ents.Min(m => m.Strength);
        var maxStrength = ents.Max(m => m.Strength);

        var strengthDiv = maxStrength - minStrength;

        ents.ForEach(f => f.Strength = (f.Strength - minStrength) / strengthDiv);
        return ents;
    }

    public List<TimelineEnt> SetNormalizedStrength(List<TimelineQueryData> data)
    {
        if (!data.Any())
        {
            return new();
        }

        var minGain = data.Min(m => m.AvgGain);
        var maxGain = data.Max(m => m.AvgGain);
        var minPlDiff = data.Min(m => m.AvgRating - m.AvgOppRating);
        var maxPlDiff = data.Max(m => m.AvgRating - m.AvgOppRating);
        var minTeamDiff = data.Min(m => m.AvgTeamRating - m.AvgOppTeamRating);
        var maxTeamDiff = data.Max(m => m.AvgTeamRating - m.AvgOppTeamRating);
        //var minPlDiff = data.Min(m => m.AvgOppRating - m.AvgRating);
        //var maxPlDiff = data.Max(m => m.AvgOppRating - m.AvgRating);
        //var minTeamDiff = data.Min(m => m.AvgOppTeamRating - m.AvgTeamRating);
        //var maxTeamDiff = data.Max(m => m.AvgOppTeamRating - m.AvgTeamRating);
        var minWinrate = data.Min(m => m.Wins * 100.0 / m.Count);
        var maxWinrate = data.Max(m => m.Wins * 100.0 / m.Count);

        var gainDiv = maxGain - minGain;
        var plDiv = maxPlDiff - minPlDiff;
        var teamDiv = maxTeamDiff - minTeamDiff;
        var winDiv = maxWinrate - minWinrate;

        var nGains = data.Select(s => (s.AvgGain - minGain) / gainDiv).ToArray();
        var nPlDiffs = data.Select(s => (minPlDiff - (s.AvgRating - s.AvgOppRating)) / plDiv).ToArray();
        var nTeamDiffs = data.Select(s => (minTeamDiff - (s.AvgTeamRating - s.AvgOppTeamRating) / teamDiv)).ToArray();
        //var nPlDiffs = data.Select(s => (minPlDiff - (s.AvgOppRating - s.AvgRating)) / plDiv).ToArray();
        //var nTeamDiffs = data.Select(s => (minTeamDiff - (s.AvgOppTeamRating - s.AvgTeamRating) / teamDiv)).ToArray();
        var nWinrates = data.Select(s => (minWinrate - (s.Wins * 100.0 / s.Count) / winDiv)).ToArray();

        var weightGain = 6;
        var weightPlDiff = 1;
        var weightTeamDiff = 1;
        var weightWinrate = 1;

        List<TimelineEnt> ents = new();

        for (int i = 0; i < data.Count; i++)
        {
            var ent = data[i];
            ents.Add(new()
            {
                Commander = (Commander)ent.Race,
                Time = new DateTime(ent.Ryear, ent.Rmonth, 1),
                Count = ent.Count,
                Wins = ent.Wins,
                AvgGain = ent.AvgGain,
                AvgRating = ent.AvgRating,
                Strength = (weightGain * nGains[i])
                    + (weightPlDiff * nPlDiffs[i])
                    + (weightTeamDiff * nTeamDiffs[i])
                    + (weightWinrate * nWinrates[i])
            }); 
        }

        var minStrength = ents.Min(m => m.Strength);
        var maxStrength = ents.Max(m => m.Strength);

        var strengthDiv = maxStrength - minStrength;

        ents.ForEach(f => f.Strength = (f.Strength - minStrength) / strengthDiv);
        return ents;
    }

    private async Task<List<TimelineEnt>> GetData(TimelineRequest request, CancellationToken token)
    {
        if (request.ComboRating)
        {
            return await GetComboData(request, token);
        }

        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        toDate = new DateTime(toDate.Year, toDate.Month, 1);

        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                    group new { r, rp } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
                    select new TimelineEnt
                    {
                        Commander = g.Key.Race,
                        Time = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.RatingChange), 2),
                        Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win)
                    };

        return await group.ToListAsync();
    }

    private async Task<List<TimelineEnt>> GetComboData(TimelineRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        toDate = new DateTime(toDate.Year, toDate.Month, 1);

        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && r.ComboReplayRating!.RatingType == request.RatingType
                        && rp.Duration >= 300
                    group new { r, rp } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
                    select new TimelineEnt
                    {
                        Commander = g.Key.Race,
                        Time = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rp.ComboReplayPlayerRating!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rp.ComboReplayPlayerRating!.Change), 2),
                        Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win)
                    };

        return await group.ToListAsync();
    }

    //public async Task<List<TimelineQueryData>> GetData2(TimelineRequest request, CancellationToken token)
    //{
    //    (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

    //    toDate = new DateTime(toDate.Year, toDate.Month, 1);

    //    var group = from r in context.Replays
    //                from rp in r.ReplayPlayers
    //                from rpr in r.ReplayRatingInfo.RepPlayerRatings
    //                    //join rp in context.ReplayPlayers on r.ReplayId equals rp.ReplayId
    //                    //join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
    //                    //join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
    //                where r.GameTime > fromDate
    //                    && r.GameTime < toDate
    //                    && r.ReplayRatingInfo!.RatingType == request.RatingType
    //                    && rp.Duration >= 300
    //                group new { r, rp, rpr } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
    //                select new TimelineQueryData
    //                {
    //                    Commander = g.Key.Race,
    //                    Time = new DateTime(g.Key.Year, g.Key.Month, 1),
    //                    Count = g.Count(),
    //                    AvgRating = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.Rating), 2),
    //                    AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo!.RatingChange), 2),
    //                    Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win),
    //                    AvgOppRating = Math.Round(g.Average(a =>
    //                        a.rp.GamePos == 1 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 4).Rating :
    //                        a.rp.GamePos == 2 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 5).Rating :
    //                        a.rp.GamePos == 3 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 6).Rating :
    //                        a.rp.GamePos == 4 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 1).Rating :
    //                        a.rp.GamePos == 5 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 2).Rating :
    //                        a.rp.GamePos == 6 ? context.RepPlayerRatings.First(f => f.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && f.GamePos == 3).Rating :
    //                        0
    //                        ), 2),
    //                    AvgTeamRating = Math.Round(g.Average(a =>
    //                        a.rp.GamePos <= 3 ? context.RepPlayerRatings.Where(x => x.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && x.GamePos <= 3).Sum(s => s.Rating) :
    //                        context.RepPlayerRatings.Where(x => x.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && x.GamePos > 3).Sum(s => s.Rating)
    //                    ), 2),
    //                    AvgOppTeamRating = Math.Round(g.Average(a =>
    //                        a.rp.GamePos <= 3 ? context.RepPlayerRatings.Where(x => x.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && x.GamePos > 3).Sum(s => s.Rating) :
    //                        context.RepPlayerRatings.Where(x => x.ReplayRatingInfoId == a.rpr.ReplayRatingInfoId && x.GamePos <= 3).Sum(s => s.Rating)
    //                    ), 2),
    //                };

    //    return await group.ToListAsync();
    //}


    public async Task<List<TimelineQueryData>> GetDataFromRaw(TimelineRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        toDate = new DateTime(toDate.Year, toDate.Month, 1);


        var sql = $@"
           select
           rp.Race,
           year(r.GameTime) as ryear,
           month(r.GameTime) as rmonth,
           count(*) as count,
           sum(CASE WHEN rp.PlayerResult = 1 THEN 1 END) as wins,
           round(AVG(rpr.RatingChange), 2) as avggain,
           round(AVG(rpr.Rating), 2) as avgrating,
           round(AVG(CASE WHEN rpr.GamePos = 1 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 4)

               WHEN rpr.GamePos = 2 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 5)
               WHEN rpr.GamePos = 3 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 6)
               WHEN rpr.GamePos = 4 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 1)
               WHEN rpr.GamePos = 5 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 2)
               WHEN rpr.GamePos = 6 THEN (select Rating from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos = 3)
               END), 2) as avgopprating,    
           round(AVG(CASE WHEN rpr.GamePos <= 3 THEN

               (SELECT sum(Rating) from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos <= 3) ELSE
               (SELECT sum(Rating) from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos > 3) END), 2) as avgteamrating,
           round(AVG(CASE WHEN rpr.GamePos <= 3 THEN

               (SELECT sum(Rating) from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos > 3) ELSE
               (SELECT sum(Rating) from RepPlayerRatings where ReplayRatingInfoId = rr.ReplayRatingId AND GamePos <= 3) END), 2) as avgoppteamrating
           from Replays as r
           inner join ReplayPlayers as rp on rp.ReplayId = r.ReplayId
           inner join ReplayRatings as rr on rr.ReplayId = r.ReplayId
           inner join RepPlayerRatings as rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
           where r.GameTime > '{fromDate.ToString(@"yyyy-MM-dd")}' AND r.GameTime < '{toDate.ToString(@"yyyy-MM-dd")}'
            AND rr.RatingType = {(int)request.RatingType}
           AND rp.Duration >= 300
           GROUP BY rp.Race, ryear, rmonth
            ;
        ";
        var result = await context.TimelineQueryDatas
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }
}

public record TimelineQueryData
{
    public int Race { get; set; }
    public int Ryear { get; set; }
    public int Rmonth { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
    public double AvgRating { get; set; }
    public double AvgOppRating { get; set; }
    public double AvgTeamRating { get; set; }
    public double AvgOppTeamRating { get; set; }
}