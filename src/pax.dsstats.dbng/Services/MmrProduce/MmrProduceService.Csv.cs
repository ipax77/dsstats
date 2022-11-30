
using System.Globalization;
using System.Text;
using dsstats.mmr;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class MmrProduceService
{
    public static void CreatePlayerRatingCsv(Dictionary<int, CalcRating> mmrIdRatings, RatingType ratingType)
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(PlayerRating.PlayerRatingId)},");
        sb.Append($"{nameof(PlayerRating.Rating)},");
        sb.Append($"{nameof(PlayerRating.Games)},");
        sb.Append($"{nameof(PlayerRating.Wins)},");
        sb.Append($"{nameof(PlayerRating.Mvp)},");
        sb.Append($"{nameof(PlayerRating.TeamGames)},");
        sb.Append($"{nameof(PlayerRating.MainCount)},");
        sb.Append($"{nameof(PlayerRating.Main)},");

        sb.Append($"{nameof(PlayerRating.MmrOverTime)}");
        sb.Append($"{nameof(PlayerRating.Consistency)}");
        sb.Append($"{nameof(PlayerRating.Confidence)}");
        sb.Append($"{nameof(PlayerRating.IsUploader)}");

        sb.Append($"{nameof(PlayerRating.PlayerId)}");
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

            sb.Append($"\"{MmrService.GetDbMmrOverTime(ent.Value.MmrOverTime)}\"");
            sb.Append($"{ent.Value.Consistency},");
            sb.Append($"{ent.Value.Confidence},");
            sb.Append($"{(ent.Value.IsUploader ? 1 : 0)}");

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