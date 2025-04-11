using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.db.Services.Ratings;

public partial class RatingsService
{
    private async Task Csv2Mysql(string fileName, string tableName, int tasks = 4, CancellationToken token = default)
    {
        if (!File.Exists(fileName))
        {
            logger.LogWarning("File not found: {filename}", fileName);
            return;
        }

        var tempTable = tableName + "_temp";
        var oldTable = tempTable + "_old";
        var splitFiles = SplitCsvFile(fileName);

        try
        {
            using var connection = new MySqlConnection(importOptions.Value.ImportConnectionString);
            await connection.OpenAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 420;

            // Drop and create temp table
            command.CommandText = @$"
DROP TABLE IF EXISTS {tempTable};
DROP TABLE IF EXISTS {oldTable};
CREATE TABLE {tempTable} LIKE {tableName};
ALTER TABLE {tempTable} DISABLE KEYS;
";
            await command.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed creating temp table: {error}", ex.Message);
            return;
        }

        // Parallel import
        if (tasks <= 0) tasks = Environment.ProcessorCount;

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = token,
        };

        await Parallel.ForEachAsync(splitFiles, async (file, token) =>
        {
            try
            {
                using var conn = new MySqlConnection(importOptions.Value.ImportConnectionString);
                await conn.OpenAsync(token);

                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 420;
                cmd.CommandText = $@"
SET SESSION unique_checks = 0;
SET SESSION foreign_key_checks = 0;
LOAD DATA INFILE '{file}'
INTO TABLE {tempTable}
COLUMNS TERMINATED BY ',' 
OPTIONALLY ENCLOSED BY '""' 
ESCAPED BY '""' 
LINES TERMINATED BY '\r\n';";

                await cmd.ExecuteNonQueryAsync(token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Import cancelled for chunk {chunk}", file);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed importing chunk {chunk}: {error}", file, ex.Message);
                throw;
            }
        });

        try
        {
            using var conn = new MySqlConnection(importOptions.Value.ImportConnectionString);
            await conn.OpenAsync(token);
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 420;

            // Re-enable keys and rename tables
            cmd.CommandText = @$"
RENAME TABLE {tableName} TO {oldTable}, {tempTable} TO {tableName};
DROP TABLE {oldTable};
ALTER TABLE {tableName} ENABLE KEYS;";
            await cmd.ExecuteNonQueryAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed finalizing temp table swap: {error}", ex.Message);
        }

        // Optional: cleanup chunk files
        foreach (var part in splitFiles.Where(f => f != fileName))
        {
            try { File.Delete(part); } catch { /* log or ignore */ }
        }
    }

    private async Task Csv2MysqlParallel(string fileName, string tableName, int tasks = 4, CancellationToken token = default)
    {
        if (!File.Exists(fileName))
        {
            logger.LogWarning("File not found: {filename}", fileName);
            return;
        }

        var splitFiles = SplitCsvFile(fileName);
        var tempTables = splitFiles.Select((_, i) => $"{tableName}_temp_{i}").ToList();
        var finalTempTable = $"{tableName}_finaltemp";
        var oldTable = tableName + "_old";

        await CreateTempTable(tableName, oldTable, finalTempTable, token);
        await ImportChunks(splitFiles, tempTables, tableName, finalTempTable, token);
        await RenameTable(tableName, oldTable, finalTempTable, token);

        foreach (var part in splitFiles.Where(f => f != fileName))
        {
            try { File.Delete(part); } catch { /* log or ignore */ }
        }
    }

    private async Task RenameTable(string tableName, string oldTable, string finalTempTable, CancellationToken token)
    {
        try
        {
            using var conn = new MySqlConnection(importOptions.Value.ImportConnectionString);
            await conn.OpenAsync(token);
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 420;

            // Re-enable keys and rename tables
            cmd.CommandText = @$"
RENAME TABLE {tableName} TO {oldTable}, {finalTempTable} TO {tableName};
DROP TABLE {oldTable};
ALTER TABLE {tableName} ENABLE KEYS;";
            await cmd.ExecuteNonQueryAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed finalizing temp table swap: {error}", ex.Message);
        }
    }

    private async Task ImportChunks(List<string> splitFiles,
                                    List<string> tempTables,
                                    string tableName,
                                    string finalTempTable,
                                    CancellationToken token)
    {
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = token,
        };

        await Parallel.ForEachAsync(splitFiles, async (file, token) =>
        {
            try
            {
                var index = splitFiles.IndexOf(file);
                var tempTable = tempTables[index];
                using var conn = new MySqlConnection(importOptions.Value.ImportConnectionString);
                await conn.OpenAsync(token);

                using var cmd = conn.CreateCommand();
                cmd.CommandTimeout = 420;
                cmd.CommandText = $@"
DROP TABLE IF EXISTS {tempTable};
CREATE TABLE {tempTable} LIKE {tableName};
SET SESSION unique_checks = 0;
SET SESSION foreign_key_checks = 0;
ALTER TABLE {finalTempTable} DISABLE KEYS;
LOAD DATA INFILE '{file}'
INTO TABLE {tempTable}
COLUMNS TERMINATED BY ',' 
OPTIONALLY ENCLOSED BY '""' 
ESCAPED BY '""' 
LINES TERMINATED BY '\r\n';
INSERT INTO {finalTempTable} SELECT * FROM {tempTable};
DROP TABLE {tempTable};";

                await cmd.ExecuteNonQueryAsync(token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Import cancelled for chunk {chunk}", file);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed importing chunk {chunk}: {error}", file, ex.Message);
                throw;
            }
        });
    }

    private async Task CreateTempTable(string tableName, string oldTable, string finalTempTable, CancellationToken token)
    {
        try
        {
            using var connection = new MySqlConnection(importOptions.Value.ImportConnectionString);
            await connection.OpenAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 420;

            // Drop and create temp table
            command.CommandText = @$"
DROP TABLE IF EXISTS {oldTable};
DROP TABLE IF EXISTS {finalTempTable};
CREATE TABLE {finalTempTable} LIKE {tableName};
ALTER TABLE {finalTempTable} DISABLE KEYS;";
            await command.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed creating temp table: {error}", ex.Message);
            return;
        }
    }

    private List<string> SplitCsvFile(string originalFile, int maxChunkSizeBytes = 5 * 1024 * 1024)
    {
        var splitFiles = new List<string>();

        FileInfo fileInfo = new FileInfo(originalFile);
        if (fileInfo.Length <= maxChunkSizeBytes)
        {
            splitFiles.Add(originalFile);
            return splitFiles;
        }

        int fileIndex = 0;
        long currentSize = 0;
        string tempFile = "";
        StreamWriter? writer = null;

        using (var reader = new StreamReader(originalFile))
        {

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // Start new chunk file
                if (writer == null || currentSize >= maxChunkSizeBytes)
                {
                    writer?.Dispose(); // Close previous writer

                    tempFile = importOptions.Value.MySqlImportDir + "/" +
                     $"{Path.GetFileNameWithoutExtension(originalFile)}_part{fileIndex++}.csv";
                    writer = new StreamWriter(tempFile);
                    splitFiles.Add(tempFile);
                    currentSize = 0;
                }

                writer.WriteLine(line);
                currentSize += System.Text.Encoding.UTF8.GetByteCount(line + Environment.NewLine);
            }
        }

        writer?.Dispose();
        File.Delete(originalFile);
        return splitFiles;
    }

}