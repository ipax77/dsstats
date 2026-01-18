using CsvHelper;
using CsvHelper.Configuration;
using dsstats.db;
using dsstats.shared;
using MySqlConnector;
using System.Globalization;

namespace dsstats.ratings;

public partial class RatingService
{
    private static readonly string csvDir = "/data/mysqlfiles";

    private async Task Csv2MySQL(string csvPath, string tableName, string connectionString)
    {
        var fixedPath = csvPath.Replace("\\", "/");
        if (!File.Exists(fixedPath))
        {
            throw new FileNotFoundException(fixedPath);
        }

        using var connection = new MySqlConnection(connectionString);

        var command = connection.CreateCommand();
        command.CommandTimeout = 420;
        command.CommandText = @$"
            SET foreign_key_checks=0;
            SET unique_checks=0;
            ALTER TABLE {tableName} DISABLE KEYS;
            ALTER INSTANCE DISABLE INNODB REDO_LOG;
            LOAD DATA INFILE '{fixedPath}' INTO TABLE {tableName}
            COLUMNS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '""' ESCAPED BY '""' LINES TERMINATED BY '\r\n';
            ALTER INSTANCE ENABLE INNODB REDO_LOG;
            ALTER TABLE {tableName} ENABLE KEYS;
            SET foreign_key_checks=1;
            SET unique_checks=1;";

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
        File.Delete(fixedPath);
    }

    private async Task SaveCsvPlayerRatings(Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> playerRatingsDict, string connectionString)
    {
        CsvInfo<PlayerRatingCsv> csvInfo = new()
        {
            FileName = Path.Combine(csvDir, "playerRatings.csv"),
            FileMode = FileMode.Create,
        };

        List<PlayerRating> playerRatings = [];

        foreach (var kvp in playerRatingsDict)
        {
            int playerId = kvp.Key;
            foreach (var ent in kvp.Value)
            {
                var ratingType = ent.Key;

                Commander mainCmdr = Commander.None;
                int mainCount = 0;

                if (ent.Value.CmdrCounts.Count > 0)
                {
                    var main = ent.Value.CmdrCounts
                        .Where(w => w.Key != Commander.None)
                        .OrderByDescending(o => o.Value).FirstOrDefault();
                    if (main.Key != Commander.None)
                    {
                        mainCmdr = main.Key;
                        mainCount = main.Value;
                    }
                }

                playerRatings.Add(new()
                {
                    PlayerId = playerId,
                    RatingType = ratingType,
                    Games = ent.Value.Games,
                    Wins = ent.Value.Wins,
                    Mvps = ent.Value.Mvps,
                    Main = mainCmdr,
                    MainCount = mainCount,
                    Change = Convert.ToInt32(ent.Value.Change),
                    Rating = ent.Value.Rating,
                    Confidence = ent.Value.Confidence,
                    Consistency = ent.Value.Consistency,
                    LastGame = ent.Value.LastGame,
                });
            }
        }
        var groups = playerRatings.GroupBy(g => g.RatingType);
        foreach (var group in groups)
        {
            int pos = 1;
            foreach (var rating in group.OrderByDescending(o => o.Rating))
            {
                rating.Position = pos++;
                csvInfo.Records.Add(new PlayerRatingCsv(rating));
            }
        }

        await WriteToCsvFile(csvInfo);
        await Csv2MySQL(csvInfo.FileName, "PlayerRatings_tmp", connectionString);
    }

    private async Task<int> SaveCsvArcadeReplayRatings(List<ReplayRating> replayRatings, int replayRatingId)
    {
        if (replayRatings.Count == 0)
        {
            return replayRatingId;
        }
        CsvInfo<ArcadeReplayRatingCsv> csvInfo = new()
        {
            FileName = Path.Combine(csvDir, "arcadeReplayRatings.csv"),
            FileMode = replayRatingId == 0 ? FileMode.Create : FileMode.Append,
        };
        int id = replayRatingId;
        foreach (var rating in replayRatings)
        {
            csvInfo.Records.Add(new ArcadeReplayRatingCsv(rating, ++id));
        }
        await WriteToCsvFile(csvInfo);
        return id;
    }

    private async Task<(int, int)> SaveCsvStepResult(List<ReplayRating> replayRatings, int replayRatingId, int replayPlayerRatingId)
    {
        if (replayRatings.Count == 0)
        {
            return (replayRatingId, replayPlayerRatingId);
        }

        CsvInfo<ReplayRatingCsv> replayRatingsInfo = new()
        {
            FileName = Path.Combine(csvDir, "replayRatings.csv"),
            FileMode = replayPlayerRatingId == 0 ? FileMode.Create : FileMode.Append,
        };

        CsvInfo<ReplayPlayerRatingCsv> replayPlayerRatingsInfo = new()
        {
            FileName = Path.Combine(csvDir, "replayPlayerRatings.csv"),
            FileMode = replayPlayerRatingId == 0 ? FileMode.Create : FileMode.Append
        };


        foreach (var rating in replayRatings)
        {
            int id = ++replayRatingId;
            replayRatingsInfo.Records.Add(new ReplayRatingCsv(rating, id));

            foreach (var rpRating in rating.ReplayPlayerRatings)
            {
                int rpId = ++replayPlayerRatingId;
                replayPlayerRatingsInfo.Records.Add(new ReplayPlayerRatingCsv(rpRating, rpId, id));
            }
        }

        await WriteToCsvFile(replayRatingsInfo);
        await WriteToCsvFile(replayPlayerRatingsInfo);

        return (replayRatingId, replayPlayerRatingId);
    }

    private static async Task WriteToCsvFile<T>(CsvInfo<T> info) where T : ICsvRecord
    {
        if (info.Records.Count == 0) return;

        using var stream = File.Open(info.FileName, info.FileMode);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });

        await csv.WriteRecordsAsync(info.Records);
    }
}

internal class CsvInfo<T> where T : ICsvRecord
{
    public string FileName { get; set; } = string.Empty;
    public FileMode FileMode { get; set; }
    public List<T> Records { get; set; } = new();
}

internal interface ICsvRecord;

internal class ReplayRatingCsv : ICsvRecord
{
    public ReplayRatingCsv(ReplayRating rating, int id)
    {
        ReplayRatingId = id;
        RatingType = (int)rating.RatingType;
        LeaverType = (int)rating.LeaverType;
        ExpectedWinProbability = rating.ExpectedWinProbability;
        IsPreRating = rating.IsPreRating ? 1 : 0;
        AvgRating = rating.AvgRating;
        ReplayId = rating.ReplayId;
    }
    public int ReplayRatingId { get; set; }
    public int RatingType { get; set; }
    public int LeaverType { get; set; }
    public double ExpectedWinProbability { get; set; }
    public int IsPreRating { get; set; }
    public int AvgRating { get; set; }
    public int ReplayId { get; set; }
}


internal class ReplayPlayerRatingCsv : ICsvRecord
{
    public ReplayPlayerRatingCsv(ReplayPlayerRating rating, int id, int replayRatingId)
    {
        ReplayPlayerRatingId = id;
        RatingType = (int)rating.RatingType;
        RatingBefore = rating.RatingBefore;
        RatingDelta = rating.RatingDelta;
        ExpectedDelta = rating.ExpectedDelta;
        Games = rating.Games;
        ReplayRatingId = replayRatingId;
        ReplayPlayerId = rating.ReplayPlayerId;
        PlayerId = rating.PlayerId;
    }
    public int ReplayPlayerRatingId { get; set; }
    public int RatingType { get; set; }

    public double RatingBefore { get; set; }
    public double RatingDelta { get; set; }
    public double ExpectedDelta { get; set; }
    public int Games { get; set; }
    public int ReplayRatingId { get; set; }
    public int ReplayPlayerId { get; set; }
    public int PlayerId { get; set; }
}

public class PlayerRatingCsv : ICsvRecord
{
    public PlayerRatingCsv(PlayerRating rating)
    {
        PlayerRatingId = rating.PlayerRatingId;
        RatingType = (int)rating.RatingType;
        Games = rating.Games;
        Wins = rating.Wins;
        Mvps = rating.Mvps;
        Main = (int)rating.Main;
        MainCount = rating.MainCount;
        Change = rating.Change;
        Rating = rating.Rating;
        Consistency = rating.Consistency;
        Confidence = rating.Confidence;
        LastGame = rating.LastGame.ToString("yyyy-MM-dd");
        Position = rating.Position;
        PlayerId = rating.PlayerId;
        Main = (int)rating.Main;
        MainCount = rating.MainCount;
    }

    public int PlayerRatingId { get; set; }
    public int RatingType { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public int Main { get; set; }
    public int MainCount { get; set; }
    public int Change { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public string LastGame { get; set; }
    public int Position { get; set; }
    public int PlayerId { get; set; }
}

public class ArcadeReplayRatingCsv : ICsvRecord
{
    public ArcadeReplayRatingCsv(ReplayRating rating, int id)
    {
        ArcadeReplayRatingId = id;
        ExpectedWinProbability = Convert.ToInt32(rating.ExpectedWinProbability * 100.0);
        PlayerRatings = "[" + string.Join(",", rating.ReplayPlayerRatings.Select(s => ((int)s.RatingBefore).ToString())) + "]";
        PlayerRatingDeltas = "[" + string.Join(",", rating.ReplayPlayerRatings.Select(s => ((int)s.RatingDelta).ToString())) + "]";
        AvgRating = rating.AvgRating;
        ArcadeReplayId = rating.ReplayId;
    }
    public int ArcadeReplayRatingId { get; set; }
    public int ExpectedWinProbability { get; set; }
    public string PlayerRatings { get; set; } = string.Empty;
    public string PlayerRatingDeltas { get; set; } = string.Empty;
    public int AvgRating { get; set; }
    public int ArcadeReplayId { get; set; }
}