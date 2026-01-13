using dsstats.db;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui.Services;

public class DatabaseService(DsstatsService dsstatsService, DsstatsContext context)
{
    private readonly string _dbPath = Path.Combine(FileSystem.Current.AppDataDirectory, "dsstats4.db");

    public async Task<string?> BackupDatabase()
    {
        await dsstatsService.DbSemaphore.WaitAsync();
        try
        {
            // var backupDir = Path.Combine(FileSystem.Current.AppDataDirectory, "Backups");
            var backupDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var destinationPath = Path.Combine(backupDir, $"dsstats_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            File.Copy(_dbPath, destinationPath, true);
            return destinationPath;
        }
        catch
        {
            return null;
        }
        finally
        {
            dsstatsService.DbSemaphore.Release();
        }
    }

    public async Task<bool> RestoreDatabase(string sourcePath)
    {
        await dsstatsService.DbSemaphore.WaitAsync();
        var backupPath = _dbPath + ".bak";
        var tempPath = _dbPath + ".tmp";

        try
        {
            // Backup current
            if (File.Exists(_dbPath))
                File.Copy(_dbPath, backupPath, true);

            // Copy candidate into temp
            File.Copy(sourcePath, tempPath, true);

            // Migrate temp using fresh context
            var optionsBuilder = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite($"Data Source={tempPath}");

            using (var testContext = new DsstatsContext(optionsBuilder.Options))
            {
                testContext.Database.OpenConnection();
                try
                {
                    testContext.Database.Migrate();
                }
                finally
                {
                    testContext.Database.CloseConnection();
                }
            }

            // Clear pooled connections
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            // Replace live DB
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);

            File.Copy(tempPath, _dbPath, true);

            return true;
        }
        catch
        {
            // Restore backup if needed
            if (File.Exists(backupPath))
                File.Copy(backupPath, _dbPath, true);

            return false;
        }
        finally
        {
            // Clear pools before delete
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignore */ }
            }

            dsstatsService.DbSemaphore.Release();
        }
    }

}
