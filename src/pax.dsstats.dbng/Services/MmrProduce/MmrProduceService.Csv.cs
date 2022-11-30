
using System.Globalization;
using System.Text;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class MmrProduceService
{
    public static void CreateCsv(Dictionary<int, CalcRating> mmrIdRatings, RatingType ratingType)
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(PlayerRating.PlayerRatingId)},{nameof(PlayerRating.Rating)},{nameof(PlayerRating.Games)},{nameof(PlayerRating.Wins)},{nameof(PlayerRating.Mvp)},{nameof(PlayerRating.TeamGamges)},{nameof(PlayerRating.MainCount)},{nameof(PlayerRating.Main)},{nameof(PlayerRating.PlayerId)}");
        sb.Append(Environment.NewLine);
        int i = 0;
        foreach (var ent in mmrIdRatings)
        {
            i++;
            var main = ent.Value.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
            sb.Append($"{i},");
            sb.Append($"{ent.Value.Mmr.ToString(CultureInfo.InvariantCulture)},");
            sb.Append($"{ent.Value.Games},");
            sb.Append($"{ent.Value.Wins},");
            sb.Append($"{ent.Value.Mvp},");
            sb.Append("0,");
            sb.Append($"{main.Value},");
            sb.Append($"{(int)main.Key},");
            sb.Append($"{ent.Value.PlayerId}");
            sb.Append(Environment.NewLine);
        }
        File.WriteAllText($"/data/ds/{ratingType}Rating.csv", sb.ToString());
    }

    public async Task SaveCsv()
    {
        // TRUNCATE TABLE playerratings;

        // LOAD DATA INFILE "/data/mysqlfiles/Cmdr__efmigrationshistoryRating.csv"
        // INTO TABLE playerratings
        // COLUMNS TERMINATED BY ','
        // OPTIONALLY ENCLOSED BY '"'
        // ESCAPED BY '"'
        // LINES TERMINATED BY '\n'
        // IGNORE 1 LINES;
    }
}