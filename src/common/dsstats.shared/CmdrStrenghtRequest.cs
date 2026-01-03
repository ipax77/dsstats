
namespace dsstats.shared;

public sealed class CmdrStrenghtRequest
{
    public int MaxRating { get; set; }
    public int MinRating { get; set; }
    public int MaxGap { get; set; }
    public Commander Interest { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public bool WithoutLeavers { get; set; }
    public bool Uploaders { get; set; }
}

public sealed class CmdrStrenghtResponse
{
    public List<CmdrStrengthItem> Items { get; init; } = [];
}

public sealed class CmdrStrengthItem
{
    public double TeamRating { get; init; }
    public double AvgRating { get; init; }
    public double AvgGain { get; init; }
    public int Wins { get; init; }
    public int Count { get; init; }
    public ToonIdDto ToonId { get; init; } = new();
    public string Name { get; set; } = string.Empty;
}

public sealed record CmdrStrengthItemEx(
    CmdrStrengthItem Item,
    double Strength
)
{
    public string Name => Item.Name;
    public double AvgGain => Item.AvgGain;
    public double AvgRating => Item.AvgRating;
    public double TeamRating => Item.TeamRating;
    public int Wins => Item.Wins;
    public int Count => Item.Count;
    public ToonIdDto ToonId => Item.ToonId;
}

public static class CmdrStrengthExtensions
{
    public static IEnumerable<CmdrStrengthItemEx> WithStrength(this CmdrStrenghtResponse response)
    {
        if (response.Items.Count == 0)
            return [];

        double weightMatchups = 1;
        double weightWinrate = 5;
        double weightRating = 8;
        double weightGain = 8;

        double minMatchups = response.Items.Min(m => m.Count);
        double maxMatchups = response.Items.Max(m => m.Count);

        var winrates = response.Items.Select(s => s.Wins * 100.0 / (double)s.Count).ToList();
        double minWinrate = winrates.Min();
        double maxWinrate = winrates.Max();

        double minRating = response.Items.Min(m => m.AvgRating);
        double maxRating = response.Items.Max(m => m.AvgRating);

        double minGain = response.Items.Min(m => m.AvgGain);
        double maxGain = response.Items.Max(m => m.AvgGain);

        return response.Items.Select(item =>
        {
            var normalizedMatchups = (item.Count - minMatchups) / (maxMatchups - minMatchups);
            var normalizedWinrate = (item.Wins * 100.0 / (double)item.Count - minWinrate) / (maxWinrate - minWinrate);
            var normalizedRating = (item.AvgRating - minRating) / (maxRating - minRating);
            var normalizedGain = (item.AvgGain - minGain) / (maxGain - minGain);

            double strength =
                  weightMatchups * normalizedMatchups
                + weightWinrate * normalizedWinrate
                + weightRating * normalizedRating
                + weightGain * normalizedGain;

            return new CmdrStrengthItemEx(item, strength);
        });
    }

    public static IEnumerable<CmdrStrengthItemEx> WithStaticStrength(this CmdrStrenghtResponse response)
    {
        if (response.Items.Count == 0)
            return [];

        // -------------------------------
        // GLOBAL FIXED NORMALIZATION RANGES
        // -------------------------------
        const double minMatchups = 0;
        const double maxMatchups = 200; // capped

        const double minWinrate = 0;   // %
        const double maxWinrate = 100; // %

        const double minRating = 500;
        const double maxRating = 3500;

        const double minGain = -40;
        const double maxGain = 40;

        // Weights (your original ones)
        const double weightMatchups = 1;
        const double weightWinrate = 5;
        const double weightRating = 8;
        const double weightGain = 8;

        const double maxStrengthRaw =
                weightMatchups
            + weightWinrate
            + weightRating
            + weightGain;

        return response.Items.Select(item =>
        {
            // --- Compute raw values ---
            double winrate = item.Count == 0
                ? 0
                : item.Wins * 100.0 / item.Count;

            // --- Normalization with clamping ---
            double normalizedMatchups = Clamp01((item.Count - minMatchups) / (maxMatchups - minMatchups));
            double normalizedWinrate = Clamp01((winrate - minWinrate) / (maxWinrate - minWinrate));
            double normalizedRating = Clamp01((item.AvgRating - minRating) / (maxRating - minRating));
            double normalizedGain = Clamp01((item.AvgGain - minGain) / (maxGain - minGain));

            // --- Strength formula ---
            double strengthRaw =
                  weightMatchups * normalizedMatchups
                + weightWinrate * normalizedWinrate
                + weightRating * normalizedRating
                + weightGain * normalizedGain;

            double strength100 = (strengthRaw / maxStrengthRaw) * 100.0;

            return new CmdrStrengthItemEx(item, strength100);
        });
    }

    private static double Clamp01(double v)
        => v < 0 ? 0 : v > 1 ? 1 : v;

    public static string GenMemKey(this CmdrStrenghtRequest request)
    {
        return $"cmdrstrength_{request.MaxRating}_{request.MinRating}_{request.MaxGap}_{request.Interest}_{request.TimePeriod}_{request.RatingType}_{request.WithoutLeavers}_{request.Uploaders}";
    }
}
