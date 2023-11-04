
using dsstats.db8;
using dsstats.shared;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.db8services;

public partial class CalcRepository
{
    public async Task PlayerRatingsFromCsv2MySql(RatingCalcType ratingCalcType)
    {
        var name = ratingCalcType switch
        {
            RatingCalcType.Dsstats => "PlayerRatings",
            RatingCalcType.Arcade => "ArcadePlayerRatings",
            RatingCalcType.Combo => "ComboPlayerRatings",
            _ => throw new IndexOutOfRangeException(nameof(ratingCalcType))
        };

        var csvFile = $"{csvBasePath}/{name}.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            var tempTable = $"{name}_temp";

            // Create temporary table
            command.CommandText = $@"
            DROP TABLE IF EXISTS {tempTable};
            DROP TABLE IF EXISTS {name}_old;
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {name};
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
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();

            var foreignKeyFix = ratingCalcType switch
            {
                RatingCalcType.Dsstats => $@"ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} DROP FOREIGN KEY FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId;
ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} ADD CONSTRAINT FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId
    FOREIGN KEY (PlayerRatingId) REFERENCES {nameof(ReplayContext.PlayerRatings)} (PlayerRatingId);",
                RatingCalcType.Arcade => $@"ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} DROP FOREIGN KEY `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`;
ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} ADD CONSTRAINT `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`
    FOREIGN KEY (ArcadePlayerRatingId) REFERENCES {nameof(ReplayContext.ArcadePlayerRatings)} (ArcadePlayerRatingId);",
                _ => ""
            };

            // Rename temporary table to original table
            command.CommandText =
    $@"SET FOREIGN_KEY_CHECKS = 0;
RENAME TABLE {name} TO {name}_old, {tempTable} TO {name};
DROP TABLE {name}_old;
{foreignKeyFix}
SET FOREIGN_KEY_CHECKS = 1;
";
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed importing {name} from csv: {error}", name, ex.Message);
        }
        File.Delete(csvFile);
    }

    public async Task ReplayRatingsFromCsv2MySql(RatingCalcType ratingCalcType)
    {
        var name = ratingCalcType switch
        {
            RatingCalcType.Dsstats => "ReplayRatings",
            RatingCalcType.Arcade => "ArcadeReplayRatings",
            RatingCalcType.Combo => "ComboReplayRatings",
            _ => throw new IndexOutOfRangeException(nameof(ratingCalcType))
        };

        var csvFile = $"{csvBasePath}/{name}.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            var tempTable = $"{name}_temp";

            // Create temporary table
            command.CommandText = $@"
            DROP TABLE IF EXISTS {name}_old;
            DROP TABLE IF EXISTS {tempTable};
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {name};
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
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();

            // Rename temporary table to original table
            command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {name} TO {name}_old, {tempTable} TO {name};
            DROP TABLE {name}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed storing {name} from csv: {error}", name, ex.Message);
        }
        File.Delete(csvFile);
    }

    public async Task ReplayPlayerRatingsFromCsv2MySql(RatingCalcType ratingCalcType)
    {
        var name = ratingCalcType switch
        {
            RatingCalcType.Dsstats => "RepPlayerRatings",
            RatingCalcType.Arcade => "ArcadeReplayPlayerRatings",
            RatingCalcType.Combo => "ComboReplayPlayerRatings",
            _ => throw new IndexOutOfRangeException(nameof(ratingCalcType))
        };

        var csvFile = $"{csvBasePath}/{name}.csv";
        if (!File.Exists(csvFile))
        {
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            var tempTable = $"{name}_temp";

            // Create temporary table
            command.CommandText = $@"
            DROP TABLE IF EXISTS {tempTable};
            DROP TABLE IF EXISTS {name}_old;
            CREATE TABLE IF NOT EXISTS {tempTable} LIKE {name};
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
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();

            // Rename temporary table to original table
            command.CommandText = $@"
            SET FOREIGN_KEY_CHECKS = 0;
            RENAME TABLE {name} TO {name}_old, {tempTable} TO {name};
            DROP TABLE {name}_old;
            SET FOREIGN_KEY_CHECKS = 1;
        ";
            command.CommandTimeout = 360;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed storing {name} from csv: {error}", name, ex.Message);
        }
        File.Delete(csvFile);
    }
}