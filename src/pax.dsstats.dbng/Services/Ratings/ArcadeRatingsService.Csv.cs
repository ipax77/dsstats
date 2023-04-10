using MySqlConnector;
using pax.dsstats.dbng;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class ArcadeRatingsService
{
    private async Task PlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ArcadePlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.ArcadePlayerRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.ArcadePlayerRatings)};
        ";
        await command.ExecuteNonQueryAsync();

        // Load data into temporary table
        command.CommandText = $@"
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {tempTable}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
        ";
        command.CommandTimeout = 500;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.ArcadePlayerRatings)} TO {nameof(ReplayContext.ArcadePlayerRatings)}_old, {tempTable} TO {nameof(ReplayContext.ArcadePlayerRatings)};
            DROP TABLE {nameof(ReplayContext.ArcadePlayerRatings)}_old;
            ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} DROP FOREIGN KEY `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`;
            ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} ADD CONSTRAINT `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`
                FOREIGN KEY (ArcadePlayerRatingId) REFERENCES {nameof(ReplayContext.ArcadePlayerRatings)} (ArcadePlayerRatingId);
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ArcadeReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.ArcadeReplayRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.ArcadeReplayRatings)};
        ";
        await command.ExecuteNonQueryAsync();

        command.CommandText =
        $@"
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {tempTable}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
        ";
        command.CommandTimeout = 500;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.ArcadeReplayRatings)} TO {nameof(ReplayContext.ArcadeReplayRatings)}_old, {tempTable} TO {nameof(ReplayContext.ArcadeReplayRatings)};
            DROP TABLE {nameof(ReplayContext.ArcadeReplayRatings)}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();

        File.Delete(csvFile);
    }

    private async Task ReplayPlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ArcadeReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.ArcadeReplayPlayerRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.ArcadeReplayPlayerRatings)};
        ";
        await command.ExecuteNonQueryAsync();

        command.CommandText =
        $@"
            LOAD DATA INFILE '{csvFile}'
            INTO TABLE {tempTable}
            COLUMNS TERMINATED BY ','
            OPTIONALLY ENCLOSED BY '""'
            ESCAPED BY '""'
            LINES TERMINATED BY '\n';
        ";
        command.CommandTimeout = 500;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.ArcadeReplayPlayerRatings)} TO {nameof(ReplayContext.ArcadeReplayPlayerRatings)}_old, {tempTable} TO {nameof(ReplayContext.ArcadeReplayPlayerRatings)};
            DROP TABLE {nameof(ReplayContext.ArcadeReplayPlayerRatings)}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

}
