using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class StatsService
{
    private async Task<StatsResponse> GetTimeline(StatsRequest request)
    {
        if (!request.DefaultFilter)
        {
            return await GetCustomTimeline(request);
        }

        var firstNotSecond = request.GameModes.Except(defaultGameModes).ToList();
        var secondNotFirst = defaultGameModes.Except(request.GameModes).ToList();

        if (firstNotSecond.Any() || secondNotFirst.Any())
        {
            return await GetCustomTimeline(request);
        }

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        var cmdrstats = await GetRequestStats(request);

        var stats = cmdrstats.Where(x => x.Time >= startTime && x.Time <= endTime);

        int tcount = stats.Sum(s => s.Count);

        StatsResponse response = new()
        {
            Request = request,
            CountResponse = await GetCount(request),
            AvgDuration = tcount == 0 ? 0 : (int)stats.Sum(s => s.Duration) / tcount,
            Items = new List<StatsResponseItem>()
        };

        DateTime requestTime = startTime;

        while (requestTime < endTime)
        {
            var timeResults = stats.Where(f => f.Year == requestTime.Year && f.Month == requestTime.Month && f.Race == request.Interest);

            if (!timeResults.Any())
            {
                response.Items.Add(new()
                {
                    Label = $"{requestTime.ToString("yyyy-MM")} (0)",
                    Matchups = 0,
                    Wins = 0
                });
            }
            else
            {
                int ccount = timeResults.Sum(s => s.Count);
                response.Items.Add(new()
                {
                    Label = $"{requestTime.ToString("yyyy-MM")} ({ccount})",
                    Matchups = ccount,
                    Wins = timeResults.Sum(s => s.Wins)
                });
            }
            requestTime = requestTime.AddMonths(1);
        }

        var lastItem = response.Items.LastOrDefault();
        if (lastItem != null && lastItem.Matchups < 10)
        {
            response.Items.Remove(lastItem);
        }

        return response;
    }

    //private DateTime GetAdjustedEndTime(StatsRequest request)
    //{
    //    return request.EndTime == DateTime.MinValue || request.EndTime == DateTime.Today ?
    //        DateTime.Today.Day > 15 ?
    //            new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
    //            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1)
    //        : request.EndTime;
    //}


    public async Task<StatsResponse> GetCustomTimeline(StatsRequest request)
    {
        var lresults = await GetTimelineData(request);

        StatsResponse response = new StatsResponse()
        {
            Request = request,
            CountResponse = await GetCount(request),
            AvgDuration = lresults.Count == 0 ? 0 : (int)(lresults.Sum(s => s.Duration) / (double)lresults.Count),
            Items = new List<StatsResponseItem>()
        };

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        DateTime _dateTime = startTime;

        while (_dateTime < endTime)
        {
            DateTime dateTime = _dateTime.AddMonths(1);
            var stepResults = lresults.Where(x => x.GameTime >= _dateTime && x.GameTime < dateTime).ToList();

            response.Items.Add(new()
            {
                Label = $"{_dateTime.ToString("yyyy-MM-dd")} ({stepResults.Count})",
                Matchups = stepResults.Count,
                Wins = stepResults.Where(x => x.Win == true).Count()
            });
            _dateTime = dateTime;
        }
        return response;
    }

    private async Task<List<DbStatsResult>> GetTimelineData(StatsRequest request)
    {
        var replays = GetCustomRequestReplays(request);

        var results = from r in replays
                      from p in r.ReplayPlayers
                      where p.Race == request.Interest
                      select new DbStatsResult()
                      {
                          Id = r.ReplayId,
                          Win = p.PlayerResult == PlayerResult.Win,
                          GameTime = r.GameTime,
                          IsUploader = p.IsUploader,
                          OppRace = p.OppRace
                      };
        if (request.Uploaders)
        {
            results = results.Where(x => x.IsUploader);
        }

        if (request.Versus != Commander.None)
        {
            results = results.Where(x => x.OppRace == request.Versus);
        }

        return await results.ToListAsync();
    }
}

public record DbStatsResult
{
    public int DbStatsResultId { get; init; }
    public int Id { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Duration { get; init; }
    public bool Win { get; init; }
    public bool IsUploader { get; init; }
    public int Army { get; init; }
    public int Kills { get; init; }
    public bool MVP { get; init; }
    public DateTime GameTime { get; init; }
}