using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.maui.Services;

public static class BackupService
{
    public static string Backup()
    {
        var appDir = FileSystem.Current.AppDataDirectory;
        var dbFile = Path.Combine(appDir, "dsstats2.db");
        var configFile = Path.Combine(appDir, "config.json");

        var tempDir = Path.Combine(Path.GetTempPath(), "dsstatsbak");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var backDbFile = Path.Combine(tempDir, "dsstats2.db");
        var backConfigFile = Path.Combine(tempDir, "config.json");
        File.Copy(dbFile, backDbFile, true);
        File.Copy(configFile, backConfigFile, true);

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var zipFile = Path.Combine(desktopPath, $"dsstatsBackup{DateTime.Today.ToString(@"yyyyMMdd")}.zip");
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
            return zipFile;
        }
        else
        {
            return "";
        }
    }

    public static bool Restore(string backupFile)
    {
        if (!File.Exists(backupFile))
        {
            return false;
        }
        var appDir = FileSystem.Current.AppDataDirectory;
        var dbFile = Path.Combine(appDir, "dsstats2.db");
        var configFile = Path.Combine(appDir, "config.json");

        if (File.Exists(dbFile))
        {
            File.Move(dbFile, $"{dbFile}.bak", true);
        }
        if (File.Exists(configFile))
        {
            File.Move(configFile, $"{configFile}.bak", true);
        }

        ZipFile.ExtractToDirectory(backupFile, appDir);
        return true;
    }
}
