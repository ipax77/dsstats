using MySqlConnector;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using System.Globalization;
using System.Text;

namespace pax.dsstats.dbng.Services;
public partial class RatingRepository
{
    private static readonly string csvBasePath = "/data/mysqlfiles";
    public static void CreatePlayerRatingCsv(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
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
        sb.Append($"{nameof(PlayerRating.MmrOverTime)},");
        sb.Append($"{nameof(PlayerRating.Consistency)},");
        sb.Append($"{nameof(PlayerRating.Confidence)},");
        sb.Append($"{nameof(PlayerRating.IsUploader)},");
        sb.Append($"{nameof(PlayerRating.PlayerId)}");
        sb.Append(Environment.NewLine);
        int i = 0;

        

        foreach (var ent in mmrIdRatings)
        {
            foreach (var entCalc in ent.Value.Values)
            {
                i++;
                var main = entCalc.CmdrCounts.OrderByDescending(o => o.Value).FirstOrDefault();
                sb.Append($"{i},");
                sb.Append($"{(int)ent.Key},");
                sb.Append($"{entCalc.Mmr.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Games},");
                sb.Append($"{entCalc.Wins},");
                sb.Append($"{entCalc.Mvp},");
                sb.Append($"{entCalc.TeamGames},");
                sb.Append($"{main.Value},");
                sb.Append($"{(int)main.Key},");

                sb.Append($"\"{GetDbMmrOverTime(entCalc.MmrOverTime)}\",");
                sb.Append($"{entCalc.Consistency.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{entCalc.Confidence.ToString(CultureInfo.InvariantCulture)},");
                sb.Append($"{(entCalc.IsUploader ? 1 : 0)},");

                sb.Append($"{entCalc.PlayerId}");
                sb.Append(Environment.NewLine);
            }
        }
        File.WriteAllText($"{csvBasePath}/PlayerRatings.csv", sb.ToString());
    }

    public int WriteMmrChangeCsv(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        bool append = appendId > 0;

        StringBuilder sb = new();
        if (!append)
        {
            sb.Append($"{nameof(ReplayPlayerRating.ReplayPlayerRatingId)},");
            sb.Append($"{nameof(ReplayPlayerRating.MmrChange)},");
            sb.Append($"{nameof(ReplayPlayerRating.Pos)},");
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
                sb.Append($"{plChange.Pos},");
                sb.Append($"{plChange.ReplayPlayerId},");
                sb.Append($"{change.ReplayId}");
                sb.Append(Environment.NewLine);
            }
        }

        if (!append)
        {
            File.WriteAllText($"{csvBasePath}/ReplayPlayerRatings.csv", sb.ToString());
        }
        else
        {
            File.AppendAllText($"{csvBasePath}/ReplayPlayerRatings.csv", sb.ToString());
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
            return $"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)}";
        }

        StringBuilder sb = new();
        sb.Append($"{Math.Round(timeRatings[0].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[0].Date)}");

        if (timeRatings.Count > 2)
        {
            string timeStr = GetShortDateString(timeRatings[0].Date);
            for (int i = 1; i < timeRatings.Count - 1; i++)
            {
                string currentTimeStr = GetShortDateString(timeRatings[i].Date);
                if (currentTimeStr != timeStr)
                {
                    sb.Append('|');
                    sb.Append($"{Math.Round(timeRatings[i].Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings[i].Date)}");
                }
                timeStr = currentTimeStr;
            }
        }

        sb.Append('|');
        sb.Append($"{Math.Round(timeRatings.Last().Mmr, 1).ToString(CultureInfo.InvariantCulture)},{GetShortDateString(timeRatings.Last().Date)}");

        return sb.ToString();
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

    public async Task Csv2MySql()
    {
        await PlayerRatingsFromCsv2MySql();
        await ReplayPlayerRatingsFromCsv2MySql();
    }

    private async Task PlayerRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/PlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
        $@"
            TRUNCATE TABLE {nameof(ReplayContext.PlayerRatings)};
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.PlayerRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n'
            IGNORE 1 LINES;
        ";
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private async Task ReplayPlayerRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/ReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(Data.MysqlConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
        $@"
            TRUNCATE TABLE {nameof(ReplayContext.ReplayPlayerRatings)};
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {nameof(ReplayContext.ReplayPlayerRatings)}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n'
            IGNORE 1 LINES;
        ";
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
}
