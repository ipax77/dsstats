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
        try
        {
            // Create a backup of the current database before overwriting.
            var backupPath = _dbPath + ".bak";
            File.Copy(_dbPath, backupPath, true);
            context.Database.EnsureDeleted();
            File.Copy(sourcePath, _dbPath, true);
            context.Database.Migrate();

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            dsstatsService.DbSemaphore.Release();
        }
    }
}
