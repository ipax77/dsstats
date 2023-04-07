using MySqlConnector;
using pax.dsstats.dbng;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    private async Task PlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/PlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.PlayerRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.PlayerRatings)};
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
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.PlayerRatings)} TO {nameof(ReplayContext.PlayerRatings)}_old, {tempTable} TO {nameof(ReplayContext.PlayerRatings)};
            DROP TABLE {nameof(ReplayContext.PlayerRatings)}_old;
            ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} DROP FOREIGN KEY FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId;
            ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} ADD CONSTRAINT FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId
                FOREIGN KEY (PlayerRatingId) REFERENCES {nameof(ReplayContext.PlayerRatings)} (PlayerRatingId);
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    private async Task ReplayRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.ReplayRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.ReplayRatings)};
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
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.ReplayRatings)} TO {nameof(ReplayContext.ReplayRatings)}_old, {tempTable} TO {nameof(ReplayContext.ReplayRatings)};
            DROP TABLE {nameof(ReplayContext.ReplayRatings)}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();

        File.Delete(csvFile);
    }

    private async Task ReplayPlayerRatingsFromCsv2MySql(string csvBasePath)
    {
        var csvFile = $"{csvBasePath}/ReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"{nameof(ReplayContext.RepPlayerRatings)}_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {nameof(ReplayContext.RepPlayerRatings)};
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
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();

        // Rename temporary table to original table
        command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {nameof(ReplayContext.RepPlayerRatings)} TO {nameof(ReplayContext.RepPlayerRatings)}_old, {tempTable} TO {nameof(ReplayContext.RepPlayerRatings)};
            DROP TABLE {nameof(ReplayContext.RepPlayerRatings)}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

}
