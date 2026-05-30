using dsstats.service.Models;
using System.Text.Json;

namespace dsstats.service.Services;

internal static class DsstatsConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static async Task<DsstatsConfigLoadResult> Load(
        string appFolder,
        IReadOnlyList<string> legacyFolders,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(appFolder);

        var configFiles = GetConfigFiles(appFolder, legacyFolders);
        var configs = new List<DsstatsConfigCandidate>();
        foreach (var configFile in configFiles)
        {
            var config = await TryReadConfig(configFile, ct);
            if (config is not null)
            {
                configs.Add(config);
            }
        }

        var selected = configs
            .OrderByDescending(config => config.Options.ConfigVersion)
            .ThenByDescending(config => config.LastWriteTimeUtc)
            .ThenByDescending(config => GetConfigFileRank(config.Path))
            .FirstOrDefault();

        var options = selected?.Options ?? new AppOptions();
        var stableConfigFile = Path.Combine(appFolder, DsstatsServicePaths.ConfigFileName);
        await File.WriteAllTextAsync(
            stableConfigFile,
            JsonSerializer.Serialize(options, JsonOptions),
            ct);

        return new(
            options,
            stableConfigFile,
            selected?.Path,
            selected is null,
            configs.Count);
    }

    internal static AppOptions? ReadConfig(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var configElement = root.TryGetProperty(nameof(AppOptions), out var wrappedOptions)
            ? wrappedOptions
            : root;

        return configElement.Deserialize<AppOptions>();
    }

    private static List<string> GetConfigFiles(string appFolder, IReadOnlyList<string> legacyFolders)
    {
        var folders = new[] { appFolder }
            .Concat(legacyFolders)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return folders
            .Where(Directory.Exists)
            .SelectMany(GetConfigFilesInFolder)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetConfigFilesInFolder(string folder)
    {
        foreach (var fileName in new[] { "workerconfig3.json", "workerconfig2.json", DsstatsServicePaths.ConfigFileName })
        {
            var path = Path.Combine(folder, fileName);
            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static async Task<DsstatsConfigCandidate?> TryReadConfig(string configFile, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(configFile, ct);
            var options = ReadConfig(json);
            if (options is null)
            {
                return null;
            }

            return new(options, configFile, File.GetLastWriteTimeUtc(configFile));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static int GetConfigFileRank(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName switch
        {
            "workerconfig3.json" => 3,
            "workerconfig2.json" => 2,
            DsstatsServicePaths.ConfigFileName => 1,
            _ => 0
        };
    }

    private sealed record DsstatsConfigCandidate(
        AppOptions Options,
        string Path,
        DateTime LastWriteTimeUtc);
}

internal sealed record DsstatsConfigLoadResult(
    AppOptions Options,
    string StableConfigPath,
    string? SourceConfigPath,
    bool CreatedDefault,
    int ParsedConfigCount);
