using dsstats.shared;

namespace dsstats.dbServices.Stats;

public sealed class ResolvedStatsFilter
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool HasToDate { get; init; }

    public int? RatingFrom { get; init; }
    public int? RatingTo { get; init; }

    public int? DurationFrom { get; init; }
    public int? DurationTo { get; init; }

    public double? Exp2WinFrom { get; init; }  // already normalized [0,1]
    public double? Exp2WinTo { get; init; }
}

public static class StatsFilterResolver
{
    public static ResolvedStatsFilter Resolve(StatsRequest request)
    {
        // Resolve time period
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        var fromDate = timeInfo.Start;
        var toDate = timeInfo.End;
        var hasToDate = true;

        // Override from filter if present
        if (request.Filter != null)
        {
            fromDate = request.Filter.DateRange.From;
            toDate = request.Filter.DateRange.To;
            hasToDate = true; // custom always has end
        }

        // Boundary resolution helper
        static int? MinNull(int val, int min) => val <= min ? null : val;
        static int? MaxNull(int val, int max) => val >= max ? null : val;

        int? ratingFrom = null, ratingTo = null, durationFrom = null, durationTo = null;
        double? expFrom = null, expTo = null;

        if (request.Filter != null)
        {
            ratingFrom = MinNull(request.Filter.RatingRange.From, Data.MinBuildRating);
            ratingTo = MaxNull(request.Filter.RatingRange.To, Data.MaxBuildRating);

            durationFrom = MinNull(request.Filter.DurationRange.From, Data.MinDuration);
            durationTo = MaxNull(request.Filter.DurationRange.To, Data.MaxDuration);

            expFrom = request.Filter.Exp2WinRange.From <= 0 ? null : request.Filter.Exp2WinRange.From / 100.0;
            expTo = request.Filter.Exp2WinRange.To >= 100 ? null : request.Filter.Exp2WinRange.To / 100.0;
        }

        return new ResolvedStatsFilter
        {
            FromDate = fromDate,
            ToDate = toDate,
            HasToDate = hasToDate,
            RatingFrom = ratingFrom,
            RatingTo = ratingTo,
            DurationFrom = durationFrom,
            DurationTo = durationTo,
            Exp2WinFrom = expFrom,
            Exp2WinTo = expTo
        };
    }
}

