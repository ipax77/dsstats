using dsstats.db8;
using dsstats.localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace dsstats.maui8.Services;

public class BackupService(ILogger<BackupService> logger, IStringLocalizer<DsstatsLoc> Loc, IServiceScopeFactory scopeFactory)
{
    public async Task<BackupResult> Backup()
    {
        try
        {
            var appDir = FileSystem.Current.AppDataDirectory;
            var dbFile = Path.Combine(appDir, MauiProgram.DbName);
            var configFile = Path.Combine(appDir, "config.json");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var zipFile = Path.Combine(desktopPath, $"dsstatsBackup{DateTime.Today.ToString(@"yyyyMMdd")}.zip");

            if (!File.Exists(dbFile))
            {
                throw new FileNotFoundException($"file not found: {dbFile}");
            }

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException($"file not found: {configFile}");
            }

            if (!Directory.Exists(desktopPath))
            {
                throw new DirectoryNotFoundException($"destination not found: {desktopPath}");
            }

            if (Application.Current != null && Application.Current.MainPage != null)
            {
                bool answer = await Application.Current.MainPage.DisplayAlert(Loc["Backup"],
                    Loc["Should a backup be created at this location now?"] + $" {zipFile}", Loc["Yes"], Loc["No"]);
                if (!answer)
                {
                    return BackupResult.Canceled;
                }
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "dsstatsbak");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            var backDbFile = Path.Combine(tempDir, MauiProgram.DbName);
            var backConfigFile = Path.Combine(tempDir, "config.json");
            File.Copy(dbFile, backDbFile, true);
            File.Copy(configFile, backConfigFile, true);


            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }
            ZipFile.CreateFromDirectory(tempDir, zipFile);

            if (File.Exists(zipFile))
            {
                File.Delete(backDbFile);
                File.Delete(backConfigFile);
                Directory.Delete(tempDir);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating backup: {error}", ex.Message);
            if (Application.Current != null && Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayPromptAsync(Loc["Backup failed."], ex.Message);
            }
            return BackupResult.Error;
        }
        return BackupResult.Success;
    }

    public async Task<RestoreResult> Restore(string backupFile)
    {
        var appDir = FileSystem.Current.AppDataDirectory;
        var dbFile = Path.Combine(appDir, MauiProgram.DbName);
        var configFile = Path.Combine(appDir, "config.json");

        try
        {
            if (!File.Exists(backupFile))
            {
                throw new FileNotFoundException(backupFile);
            }

            if (Application.Current != null && Application.Current.MainPage != null)
            {
                bool answer = await Application.Current.MainPage.DisplayAlert(Loc["Restore Database?"],
                    Loc["The current data will be overwritten."], Loc["Yes"], Loc["No"]);
                if (!answer)
                {
                    return RestoreResult.Canceled;
                }
            }

            if (File.Exists(dbFile))
            {
                File.Copy(dbFile, $"{dbFile}.bak", true);
            }
            if (File.Exists(configFile))
            {
                File.Copy(configFile, $"{configFile}.bak", true);
            }

            using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();

            context.Database.EnsureDeleted();
            ZipFile.ExtractToDirectory(backupFile, appDir, true);

            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogError("Backup restore failed: {error}", ex.Message);
            try
            {
                if (File.Exists($"{dbFile}.bak"))
                {
                    File.Copy(dbFile, $"{dbFile}.bak");
                }
                if (File.Exists($"{configFile}.bak"))
                {
                    File.Copy(configFile, configFile);
                }
            } catch
            {

            }
            if (Application.Current != null && Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(Loc["Backup restore failed."], ex.Message, "OK");
            }
            return RestoreResult.Error;
        }
        if (Application.Current != null && Application.Current.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(Loc["Backup restore successful."], Loc["Please check if the settings are as expected."], "OK");
        }
        return RestoreResult.Success;
    }
}

public enum BackupResult
{
    Success = 0,
    Canceled = 1,
    Error = 2,
}

public enum RestoreResult
{
    Success = 0,
    Canceled = 1,
    Error = 2
}