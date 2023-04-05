using System.Globalization;
using System.Text;

namespace pax.dsstats.shared.Ratings;

public static class ArcadeRatingsCsvService
{
    public const string csvBasePath = "/data/mysqlfiles";
    public static void CreatePlayerRatingCsv(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        StringBuilder sb = new();
        int i = 0;
        foreach (var ent in mmrIdRatings)
        {
            foreach (var entCalc in ent.Value.Values)
            {
                i++;
                // var main = entCalc.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
                var main = new KeyValuePair<Commander, int>(Commander.None, 0);
                sb.Append($"{i},"); // Id
                sb.Append($"{(int)ent.Key},"); // RatingType
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},"); // Rating
                sb.Append($"0,"); // Pos
                sb.Append($"{entCalc.Games},");
                sb.Append($"{entCalc.Wins},");
                sb.Append($"0,"); // Mvp
                sb.Append($"0,"); // TeamGames
                sb.Append($"0,"); // MainCount
                sb.Append($"0,"); // Main
                sb.Append($"\"{GetDbMmrOverTime(entCalc.MmrOverTime)}\",");
                sb.Append($"{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{(entCalc.IsUploader ? 1 : 0)},");
                sb.Append($"{entCalc.PlayerId}");
                sb.Append(Environment.NewLine);
            }
        }
        File.WriteAllText($"{csvBasePath}/ArcadePlayerRatings.csv", sb.ToString());
    }

    public static (int, int) CreateOrAppendReplayAndReplayPlayerRatingsCsv(List<ReplayRatingDto> replayRatingDtos,
                                                                           int replayAppendId,
                                                                           int replayPlayerAppendId)
    {
        StringBuilder sbReplay = new();
        StringBuilder sbPlayer = new();

        bool append = replayAppendId > 0;

        foreach (var replayRatingDto in replayRatingDtos)
        {
            if (!replayRatingDto.RepPlayerRatings.Any())
            {
                continue;
            }

            replayAppendId++;

            sbReplay.Append($"{replayAppendId},");
            sbReplay.Append($"{(int)replayRatingDto.RatingType},");
            sbReplay.Append($"{(int)replayRatingDto.LeaverType},");
            sbReplay.Append($"{replayRatingDto.ExpectationToWin.ToString(CultureInfo.InvariantCulture)},");
            sbReplay.Append($"{replayRatingDto.ReplayId}");
            sbReplay.Append(Environment.NewLine);

            foreach (var rpr in replayRatingDto.RepPlayerRatings)
            {
                replayPlayerAppendId++;
                sbPlayer.Append($"{replayPlayerAppendId},");
                sbPlayer.Append($"{rpr.GamePos},");
                sbPlayer.Append($"{rpr.Rating.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.RatingChange.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.Games},");
                sbPlayer.Append($"{rpr.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sbPlayer.Append($"{rpr.ReplayPlayerId},");
                sbPlayer.Append($"{replayAppendId}");
                sbPlayer.Append(Environment.NewLine);
            }
        }

        if (!append)
        {
            File.WriteAllText($"{csvBasePath}/ArcadeReplayRatings.csv", sbReplay.ToString());
            File.WriteAllText($"{csvBasePath}/ArcadeReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        else
        {
            File.AppendAllText($"{csvBasePath}/ArcadeReplayRatings.csv", sbReplay.ToString());
            File.AppendAllText($"{csvBasePath}/ArcadeReplayPlayerRatings.csv", sbPlayer.ToString());
        }
        return (replayAppendId, replayPlayerAppendId);
    }

    public static string GetDbMmrOverTime(List<TimeRating> timeRatings)
    {
        if (!timeRatings.Any())
        {
            return "";
        }

        if (timeRatings.Count == 1)
        {
            return $"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)},{timeRatings[0].Count}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)},{timeRatings[0].Count}");

        if (timeRatings.Count > 2)
        {
            string timeStr = GetShortDateString(timeRatings[0].Date);
            for (int i = 1; i < timeRatings.Count - 1; i++)
            {
                string currentTimeStr = GetShortDateString(timeRatings[i].Date);
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(timeRatings[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[i].Date)},{timeRatings[i].Count}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(timeRatings.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings.Last().Date)},{timeRatings.Last().Count}");

        if (sb.Length > 3999)
        {
            return GetShortenedMmrOverTime(sb);
        }

        return sb.ToString();
    }

    private static string GetShortenedMmrOverTime(StringBuilder sb)
    {
        var mmrString = sb.ToString();
        while (mmrString.Length > 3999)
        {
            int index = mmrString.IndexOf('|');
            if (index == -1)
            {
                return "";
            }
            else
            {
                mmrString = mmrString[(index + 1)..];
            }
        }
        return mmrString;
    }

    private static string GetShortDateString(string date)
    {
        if (date.Length >= 6)
        {
            return date[2..6];
        }
        else
        {
            return date;
        }
    }
}
