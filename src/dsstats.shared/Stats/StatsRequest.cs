
using System.Text;

namespace dsstats.shared;

public record StatsRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public bool ComboRating { get; set; }
    public bool WithoutLeavers { get; set; }
    public bool MauiPlayers { get; set; }
    public StatsFilter Filter { get; set; } = new();
}

public record StatsFilter
{
    public StatsFilterRating? Rating { get; set; }
    public StatsFilterExp2Win? Exp2Win { get; set; }
    public StatsFilterTime? Time { get; set; }
}

public record StatsFilterRating
{
    public int FromRating { get; set; }
    public int ToRating { get; set; }
}

public record StatsFilterExp2Win
{
    public int FromExp2Win { get; set; }
    public int ToExp2Win { get; set; }
}

public record StatsFilterTime
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public record StatsFilterLimits
{
    public int FromRating { get; set; }
    public int ToRating { get; set; }
    public double FromExp2Win { get; set; }
    public double ToExp2Win { get; set; }
}

public static class StatsRequestExtension
{
    public static string GenMemKey(this StatsRequest request, string statsType)
    {
        StringBuilder sb = new();
        sb.Append($"Stats{statsType}");
        sb.Append(request.Filter.Exp2Win?.FromExp2Win.ToString());
        if (request.Filter.Time != null)
        {
            sb.Append($"{request.Filter.Time.FromDate.ToString("yyyy-MM-dd")}-{request.Filter.Time.ToDate.ToString("yyyy-MM-dd")}");
        }
        else
        {
            sb.Append(request.TimePeriod.ToString());
        }
        sb.Append(request.Filter.Exp2Win?.ToExp2Win.ToString());
        sb.Append(request.RatingType.ToString());
        sb.Append(request.Filter.Rating?.FromRating.ToString());
        sb.Append(request.Interest.ToString());
        sb.Append(request.Filter.Rating?.ToRating.ToString());
        sb.Append(request.ComboRating.ToString());
        sb.Append(request.WithoutLeavers.ToString());
        return sb.ToString();
    }

    public static (DateTime, DateTime) GetTimeLimits(this StatsRequest request)
    {
        if (request.Filter.Time is not null)
        {
            return (request.Filter.Time.FromDate, request.Filter.Time.ToDate);
        }

        return request.TimePeriod switch
        {
            TimePeriod.Past90Days => (DateTime.Today.AddDays(-90), DateTime.Today),
            TimePeriod.ThisMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Today),
            TimePeriod.LastMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1), new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
            TimePeriod.ThisYear => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today),
            TimePeriod.LastYear => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), new DateTime(DateTime.Now.Year, 1, 1)),
            TimePeriod.Last2Years => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), DateTime.Today),
            TimePeriod.Patch2_60 => (new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today),
            TimePeriod.Patch2_71 => (new DateTime(2023, 01, 22), DateTime.Today),
            _ => (new DateTime(2018, 1, 1), DateTime.Today),
        };
    }

    public static StatsFilterLimits GetFilterLimits(this StatsRequest request)
    {
        StatsFilterLimits limit = new();
        if (request.Filter.Rating is not null)
        {
            limit.FromRating = request.Filter.Rating.FromRating > Data.MinBuildRating ?
                request.Filter.Rating.FromRating : 0;
            limit.ToRating = request.Filter.Rating.ToRating < Data.MaxBuildRating ?
                request.Filter.Rating.ToRating : 0;
        }

        if (request.Filter.Exp2Win is not null)
        {
            limit.FromExp2Win = request.Filter.Exp2Win.FromExp2Win == 0 ? 0 : request.Filter.Exp2Win.FromExp2Win / 100.0;
            limit.ToExp2Win = request.Filter.Exp2Win.ToExp2Win == 0 ? 0 : request.Filter.Exp2Win.ToExp2Win / 100.0;
        }
        return limit;
    }
}