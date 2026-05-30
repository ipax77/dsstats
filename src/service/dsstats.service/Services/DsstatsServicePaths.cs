using Microsoft.Extensions.Logging;

namespace dsstats.service.Services;

internal static class DsstatsServicePaths
{
    internal const string AppFolderName = "dsstats.worker";
    internal const string ConfigFileName = "workerconfig.json";
    internal const string DatabaseFileName = "dsstats3.db";
    private const string LegacyDatabaseFileName = "dsstats.db";

    internal static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        AppFolderName);

    internal static string ConfigFile => Path.Combine(AppFolder, ConfigFileName);

    internal static string DatabasePath => Path.Combine(AppFolder, DatabaseFileName);

    internal static DsstatsServicePathInfo Initialize(ILogger? logger = null)
    {
        Directory.CreateDirectory(AppFolder);
        var legacyFolders = GetLegacyAppFolders(AppFolder, logger);
        MigrateDatabaseIfMissing(AppFolder, legacyFolders, logger);

        return new(AppFolder, ConfigFile, DatabasePath, legacyFolders);
    }

    internal static IReadOnlyList<string> GetLegacyAppFolders(string appFolder, ILogger? logger = null)
    {
        List<string> folders = [];
        AddIfDifferent(folders, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName), appFolder);

        if (OperatingSystem.IsWindows())
        {
            var userRoot = Path.Combine(
                Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\",
                "Users");

            try
            {
                foreach (var userFolder in Directory.EnumerateDirectories(userRoot))
                {
                    AddIfDifferent(
                        folders,
                        Path.Combine(userFolder, "AppData", "Local", AppFolderName),
                        appFolder);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("Failed discovering legacy dsstats worker folders: {Error}", ex.Message);
            }
        }

        return folders
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddIfDifferent(List<string> folders, string path, string appFolder)
    {
        if (string.IsNullOrWhiteSpace(path)
            || string.Equals(path, appFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        folders.Add(path);
    }

    private static void MigrateDatabaseIfMissing(
        string appFolder,
        IReadOnlyList<string> legacyFolders,
        ILogger? logger)
    {
        var destination = Path.Combine(appFolder, DatabaseFileName);
        if (File.Exists(destination))
        {
            return;
        }

        var source = FindLegacyDatabase(legacyFolders);
        if (source is null)
        {
            return;
        }

        try
        {
            File.Copy(source, destination, overwrite: false);
            logger?.LogWarning("Migrated dsstats worker database from {Source} to {Destination}.", source, destination);
        }
        catch (Exception ex)
        {
            logger?.LogWarning("Failed migrating dsstats worker database from {Source} to {Destination}: {Error}", source, destination, ex.Message);
        }
    }

    private static string? FindLegacyDatabase(IReadOnlyList<string> legacyFolders)
    {
        foreach (var fileName in new[] { DatabaseFileName, LegacyDatabaseFileName })
        {
            var candidate = legacyFolders
                .Select(folder => Path.Combine(folder, fileName))
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (candidate is not null)
            {
                return candidate.FullName;
            }
        }

        return null;
    }
}

internal sealed record DsstatsServicePathInfo(
    string AppFolder,
    string ConfigFile,
    string DatabasePath,
    IReadOnlyList<string> LegacyFolders);
