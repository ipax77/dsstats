
using MySqlConnector;

namespace dsstats.ratings.db;

public partial class CalcRepository
{
    public async Task DsstatsPlayerRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/ComboPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"ComboPlayerRatings_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE ComboPlayerRatings;
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
        command.CommandText =
$@"SET FOREIGN_KEY_CHECKS = 0;
RENAME TABLE ComboPlayerRatings TO ComboPlayerRatings_old, {tempTable} TO ComboPlayerRatings;
DROP TABLE ComboPlayerRatings_old;
SET FOREIGN_KEY_CHECKS = 1;
";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }

    public async Task DsstatsReplayRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/ComboReplayRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"ComboReplayRatings_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE ComboReplayRatings;
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
            RENAME TABLE ComboReplayRatings TO ComboReplayRatings_old, {tempTable} TO ComboReplayRatings;
            DROP TABLE ComboReplayRatings_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();

        File.Delete(csvFile);
    }

    public async Task DsstatsReplayPlayerRatingsFromCsv2MySql()
    {
        var csvFile = $"{csvBasePath}/ComboReplayPlayerRatings.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var tempTable = $"ComboReplayPlayerRatings_temp";

        // Create temporary table
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE ComboReplayPlayerRatings;
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
            RENAME TABLE ComboReplayPlayerRatings TO ComboReplayPlayerRatings_old, {tempTable} TO ComboReplayPlayerRatings;
            DROP TABLE ComboReplayPlayerRatings_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
        await command.ExecuteNonQueryAsync();
        File.Delete(csvFile);
    }
}