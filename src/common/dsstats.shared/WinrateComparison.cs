namespace dsstats.shared;

public sealed record WinrateComparisonRequest
{
    public DateTime ChangeDate { get; init; }
    public DateTime AfterToDate { get; init; }
    public WinrateComparisonMetric Metric { get; init; } = WinrateComparisonMetric.AverageRatingGain;

    public WinrateComparisonWindows Resolve(DateTime utcToday)
    {
        var changeDate = ChangeDate.Date;
        var afterToDate = AfterToDate.Date;
        var today = utcToday.Date;

        if (changeDate > afterToDate)
        {
            throw new ArgumentException("The comparison change date must not be after the end date.");
        }
        if (afterToDate > today)
        {
            throw new ArgumentException("The comparison end date must not be in the future.");
        }

        var afterEndExclusive = afterToDate.AddDays(1);
        var duration = afterEndExclusive - changeDate;
        var beforeStart = changeDate - duration;

        return new WinrateComparisonWindows(
            beforeStart,
            changeDate,
            changeDate,
            afterEndExclusive);
    }
}

public enum WinrateComparisonMetric
{
    AverageRatingGain = 0,
    UnadjustedWinrate = 1
}

public sealed record WinrateComparisonWindows(
    DateTime BeforeFrom,
    DateTime BeforeToExclusive,
    DateTime AfterFrom,
    DateTime AfterToExclusive)
{
    public DateTime BeforeToDate => BeforeToExclusive.AddDays(-1);
    public DateTime AfterToDate => AfterToExclusive.AddDays(-1);
}

public enum ComparisonConfidenceStatus
{
    InsufficientData = 0,
    Inconclusive = 1,
    HigherAfter = 2,
    LowerAfter = 3
}

public sealed record WinrateComparisonPeriod
{
    public int Appearances { get; init; }
    public int Replays { get; init; }
    public int Wins { get; init; }
    public double AverageRatingGain { get; init; }
    public double RawWinrate => Appearances == 0 ? 0 : Wins / (double)Appearances;
}

public sealed record WinrateComparisonEnt
{
    public Commander Commander { get; init; }
    public WinrateComparisonPeriod Before { get; init; } = new();
    public WinrateComparisonPeriod After { get; init; } = new();
    public double AverageGainDifference { get; init; }
    public double RawWinrateDifference { get; init; }
    public double? AverageGainConfidenceLow { get; init; }
    public double? AverageGainConfidenceHigh { get; init; }
    public ComparisonConfidenceStatus ConfidenceStatus { get; init; }
}

public readonly record struct AggregateMoments(int Count, double Sum, double SumSquares);

public readonly record struct ComparisonConfidenceInterval(
    double Difference,
    double? Low,
    double? High,
    ComparisonConfidenceStatus Status);

public static class WinrateComparisonStatistics
{
    // Two-sided 97.5% Student-t critical values. Values above 30 use the next
    // lower tabulated degree of freedom, which makes the interval conservative.
    private static readonly double[] StudentTCritical95 =
    [
        double.NaN,
        12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228,
        2.201, 2.179, 2.160, 2.145, 2.131, 2.120, 2.110, 2.101, 2.093, 2.086,
        2.080, 2.074, 2.069, 2.064, 2.060, 2.056, 2.052, 2.048, 2.045, 2.042
    ];

    public static ComparisonConfidenceInterval CalculateWelch95(
        AggregateMoments before,
        AggregateMoments after)
    {
        var beforeMean = before.Count == 0 ? 0 : before.Sum / before.Count;
        var afterMean = after.Count == 0 ? 0 : after.Sum / after.Count;
        var difference = afterMean - beforeMean;

        if (before.Count < 2 || after.Count < 2)
        {
            return new(difference, null, null, ComparisonConfidenceStatus.InsufficientData);
        }

        var beforeVariance = GetSampleVariance(before);
        var afterVariance = GetSampleVariance(after);
        var beforeTerm = beforeVariance / before.Count;
        var afterTerm = afterVariance / after.Count;
        var standardErrorSquared = beforeTerm + afterTerm;

        if (standardErrorSquared <= 0)
        {
            return new(
                difference,
                difference,
                difference,
                GetStatus(difference, difference));
        }

        var denominator = beforeTerm * beforeTerm / (before.Count - 1)
            + afterTerm * afterTerm / (after.Count - 1);
        var degreesOfFreedom = denominator <= 0
            ? 120
            : standardErrorSquared * standardErrorSquared / denominator;
        var margin = GetStudentTCritical95(degreesOfFreedom) * Math.Sqrt(standardErrorSquared);
        var low = difference - margin;
        var high = difference + margin;

        return new(difference, low, high, GetStatus(low, high));
    }

    private static double GetSampleVariance(AggregateMoments moments)
    {
        var correctedSumSquares = moments.SumSquares - moments.Sum * moments.Sum / moments.Count;
        return Math.Max(0, correctedSumSquares / (moments.Count - 1));
    }

    private static ComparisonConfidenceStatus GetStatus(double low, double high)
    {
        if (low > 0)
        {
            return ComparisonConfidenceStatus.HigherAfter;
        }
        if (high < 0)
        {
            return ComparisonConfidenceStatus.LowerAfter;
        }
        return ComparisonConfidenceStatus.Inconclusive;
    }

    private static double GetStudentTCritical95(double degreesOfFreedom)
    {
        if (degreesOfFreedom < 1)
        {
            return StudentTCritical95[1];
        }
        if (degreesOfFreedom < 31)
        {
            return StudentTCritical95[(int)Math.Floor(degreesOfFreedom)];
        }
        if (degreesOfFreedom < 40)
        {
            return StudentTCritical95[30];
        }
        if (degreesOfFreedom < 60)
        {
            return 2.021;
        }
        if (degreesOfFreedom < 120)
        {
            return 2.000;
        }
        return 1.980;
    }
}
