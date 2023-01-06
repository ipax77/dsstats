using pax.dsstats.shared;
using pax.dsstats;

namespace dsstats.mmr.Extensions;

internal static class CalcExtensions
{
    public static void SetMmr(this CalcRating calcRating, double mmr, DateTime gametime)
    {
        calcRating.Mmr = mmr;
        calcRating.MmrOverTime.Add(new()
        {
            Date = gametime.ToString(@"yyyyMMdd"),
            Mmr = mmr,
            Count = calcRating.Games
        });
    }

    public static void SetCmdr(this CalcRating calcRating, Commander cmdr)
    {
        if (calcRating.CmdrCounts.ContainsKey(cmdr))
        {
            calcRating.CmdrCounts[cmdr]++;
        }
        else
        {
            calcRating.CmdrCounts[cmdr] = 1;
        }
    }

    public static (Commander cmdr, double) GetMain(this CalcRating calcRating)
    {
        if (!calcRating.CmdrCounts.Any())
        {
            return (Commander.None, 0);
        }
        var main = calcRating.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();

        return (main.Key, Math.Round(main.Value * 100.0 / calcRating.CmdrCounts.Sum(s => s.Value), 2));
    }
}
