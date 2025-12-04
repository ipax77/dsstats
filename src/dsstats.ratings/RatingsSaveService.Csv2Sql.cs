
using dsstats.db8;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.ratings;

public partial class RatingsSaveService
{
    private async Task Csv2Mysql(string fileName,
                            string tableName,
                            string connectionString)
    {
        if (!File.Exists(fileName))
        {
            logger.LogWarning("file not found: {filename}", fileName);
            return;
        }

        var tempTable = tableName + "_temp";
        var oldTable = tempTable + "_old";

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"
DROP TABLE IF EXISTS {tempTable};
DROP TABLE IF EXISTS {oldTable};
CREATE TABLE {tempTable} LIKE {tableName};
SET FOREIGN_KEY_CHECKS = 0;
LOAD DATA INFILE '{fileName}' INTO TABLE {tempTable}
COLUMNS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '""' ESCAPED BY '""' LINES TERMINATED BY '\r\n';

RENAME TABLE {tableName} TO {oldTable}, {tempTable} TO {tableName};
DROP TABLE {oldTable};
SET FOREIGN_KEY_CHECKS = 1;";

            await command.ExecuteNonQueryAsync();

            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            logger.LogError("failed writing csv2sql {filename}: {error}", fileName, ex.Message);
        }
    }

    private async Task ContinueCsv2Mysql(string fileName,
                        string tableName,
                        string connectionString)
    {
        if (!File.Exists(fileName))
        {
            logger.LogWarning("file not found: {filename}", fileName);
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"
SET FOREIGN_KEY_CHECKS = 0;
LOAD DATA INFILE '{fileName}' INTO TABLE {tableName}
COLUMNS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '""' ESCAPED BY '""' LINES TERMINATED BY '\r\n';

SET FOREIGN_KEY_CHECKS = 1;";

            await command.ExecuteNonQueryAsync();

            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            logger.LogError("failed continue writing csv2sql {filename}: {error}", fileName, ex.Message);
        }
    }

    private async Task FixDsstatsForeignKey(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} DROP FOREIGN KEY FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId;
ALTER TABLE {nameof(ReplayContext.PlayerRatingChanges)} ADD CONSTRAINT FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId
FOREIGN KEY (PlayerRatingId) REFERENCES {nameof(ReplayContext.PlayerRatings)} (PlayerRatingId);";

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed fixing dsstats foreign key: {error}", ex.Message);
        }
    }

    private async Task FixArcadeForeignKey(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} DROP FOREIGN KEY `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`;
ALTER TABLE {nameof(ReplayContext.ArcadePlayerRatingChanges)} ADD CONSTRAINT `FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~`
FOREIGN KEY (ArcadePlayerRatingId) REFERENCES {nameof(ReplayContext.ArcadePlayerRatings)} (ArcadePlayerRatingId);";

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed fixing arcade foreign key: {error}", ex.Message);
        }
    }

    private async Task DropDsstatsIndexes(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"ALTER TABLE {nameof(ReplayContext.RepPlayerRatings)} DROP INDEX `IX_RepPlayerRatings_ReplayPlayerId`;
ALTER TABLE {nameof(ReplayContext.RepPlayerRatings)} DROP INDEX `IX_RepPlayerRatings_ReplayRatingInfoId`;
";

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed dropping dsstats indexes: {error}", ex.Message);
        }
    }

    private async Task ReCreateDsstatsIndexes(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandTimeout = 840;

            command.CommandText = @$"CREATE UNIQUE INDEX `IX_RepPlayerRatings_ReplayPlayerId` ON {nameof(ReplayContext.RepPlayerRatings)} ({nameof(RepPlayerRating.ReplayPlayerId)});
CREATE INDEX `IX_RepPlayerRatings_ReplayRatingInfoId` ON {nameof(ReplayContext.RepPlayerRatings)} ({nameof(RepPlayerRating.ReplayRatingInfoId)});";

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed recreating dsstats indexes: {error}", ex.Message);
        }
    }
}