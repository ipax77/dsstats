using pax.dsstats.shared.Raven;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;
public partial class RatingRepository
{
    public static async Task CreatePlayerRatingCsv(Dictionary<int, CalcRating> mmrIdRatings, RatingType ratingType)
    {
        StringBuilder sb = new();
        sb.Append($"{nameof(PlayerRating.PlayerRatingId)},");
        sb.Append($"{nameof(PlayerRating.RatingType)},");
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
            sb.Append($"{(int)ratingType},");
            sb.Append($"{ent.Value.Mmr.ToString(CultureInfo.InvariantCulture)},");
            sb.Append($"{ent.Value.Games},");
            sb.Append($"{ent.Value.Wins},");
            sb.Append($"{ent.Value.Mvp},");
            sb.Append("0,");
            sb.Append($"{main.Value},");
            sb.Append($"{(int)main.Key},");

            sb.Append($"\"{GetDbMmrOverTime(ent.Value.MmrOverTime)}\",");
            sb.Append($"{ent.Value.Consistency.ToString(CultureInfo.InvariantCulture)},");
            sb.Append($"{ent.Value.Confidence.ToString(CultureInfo.InvariantCulture)},");
            sb.Append($"{(ent.Value.IsUploader ? 1 : 0)}");

            sb.Append($"{ent.Value.PlayerId}");
            sb.Append(Environment.NewLine);
        }
        File.WriteAllText($"/data/ds/{ratingType}Rating.csv", sb.ToString());
    }

    public async Task<int> WriteMmrChangeCsv(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        bool append = appendId > 0;

        StringBuilder sb = new();
        if (!append)
        {
            sb.Append($"{nameof(ReplayPlayerRating.ReplayPlayerRatingId)},");
            sb.Append($"{nameof(ReplayPlayerRating.MmrChange)},");
            sb.Append($"{nameof(ReplayPlayerRating.ReplayPlayerId)},");
            sb.Append($"{nameof(ReplayPlayerRating.ReplayId)}");
            sb.Append(Environment.NewLine);
        }

        int i = appendId;

        foreach (var change in replayPlayerMmrChanges)
        {
            foreach (var plChange in change.Changes)
            {
                i++;
                sb.Append($"{i},");
                sb.Append($"{plChange.Change.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{plChange.ReplayPlayerId},");
                sb.Append($"{change.ReplayId}");
                sb.Append(Environment.NewLine);
            }
        }

        if (!append)
        {
            File.WriteAllText($"/data/ds/MmrChanges.csv", sb.ToString());
        }
        else
        {
            File.AppendAllText($"/data/ds/MmrChanges.csv", sb.ToString());
        }
        return i;
    }

    public static string? GetDbMmrOverTime(List<TimeRating> timeRatings)
    {
        if (!timeRatings.Any())
        {
            return null;
        }

        if (timeRatings.Count == 1)
        {
            return $"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[0].Date[2..6]}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[0].Date[2..6]}");

        if (timeRatings.Count > 2)
        {
            string timeStr = timeRatings[0].Date[2..6];
            for (int i = 1; i < timeRatings.Count - 1; i++)
            {
                string currentTimeStr = timeRatings[i].Date[2..6];
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(timeRatings[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings[i].Date[2..6]}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(timeRatings.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{timeRatings.Last().Date[2..6]}");

        return sb.ToString();
    }
}
