using dsstats.service.Models;
using dsstats.service.Services;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public sealed class DsstatsServiceConfigTests
{
    [TestMethod]
    public void ReadConfig_LoadsDirectAppOptions()
    {
        var appGuid = Guid.NewGuid();
        var config = DsstatsConfigLoader.ReadConfig($$"""
            {
              "ConfigVersion": 3,
              "AppGuid": "{{appGuid}}",
              "CPUCores": 4,
              "AutoDecode": false
            }
            """);

        Assert.IsNotNull(config);
        Assert.AreEqual(3, config.ConfigVersion);
        Assert.AreEqual(appGuid, config.AppGuid);
        Assert.AreEqual(4, config.CPUCores);
        Assert.IsFalse(config.AutoDecode);
    }

    [TestMethod]
    public void ReadConfig_LoadsWrappedAppOptions()
    {
        var appGuid = Guid.NewGuid();
        var config = DsstatsConfigLoader.ReadConfig($$"""
            {
              "AppOptions": {
                "ConfigVersion": 2,
                "AppGuid": "{{appGuid}}",
                "UploadCredential": false
              }
            }
            """);

        Assert.IsNotNull(config);
        Assert.AreEqual(2, config.ConfigVersion);
        Assert.AreEqual(appGuid, config.AppGuid);
        Assert.IsFalse(config.UploadCredential);
    }

    [TestMethod]
    public async Task Load_PrefersHighestConfigVersionAndMigratesToStableConfig()
    {
        var root = CreateTempDirectory();
        var appFolder = Path.Combine(root, "programdata");
        var legacyFolder = Path.Combine(root, "legacy");
        Directory.CreateDirectory(appFolder);
        Directory.CreateDirectory(legacyFolder);

        try
        {
            var oldGuid = Guid.NewGuid();
            var newGuid = Guid.NewGuid();
            await File.WriteAllTextAsync(
                Path.Combine(legacyFolder, "workerconfig.json"),
                $$"""
                {
                  "AppOptions": {
                    "ConfigVersion": 2,
                    "AppGuid": "{{oldGuid}}",
                    "CPUCores": 1
                  }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(legacyFolder, "workerconfig3.json"),
                $$"""
                {
                  "ConfigVersion": 3,
                  "AppGuid": "{{newGuid}}",
                  "CPUCores": 3
                }
                """);

            var result = await DsstatsConfigLoader.Load(appFolder, [legacyFolder]);

            Assert.AreEqual(3, result.Options.ConfigVersion);
            Assert.AreEqual(newGuid, result.Options.AppGuid);
            Assert.AreEqual(3, result.Options.CPUCores);
            Assert.AreEqual(Path.Combine(appFolder, "workerconfig.json"), result.StableConfigPath);
            Assert.IsTrue(File.Exists(result.StableConfigPath));

            var stableConfig = JsonSerializer.Deserialize<AppOptions>(
                await File.ReadAllTextAsync(result.StableConfigPath));
            Assert.IsNotNull(stableConfig);
            Assert.AreEqual(newGuid, stableConfig.AppGuid);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task Load_CreatesDefaultStableConfigWhenNoConfigExists()
    {
        var root = CreateTempDirectory();
        var appFolder = Path.Combine(root, "programdata");

        try
        {
            var result = await DsstatsConfigLoader.Load(appFolder, []);

            Assert.IsTrue(result.CreatedDefault);
            Assert.AreEqual(0, result.ParsedConfigCount);
            Assert.IsTrue(File.Exists(result.StableConfigPath));
            Assert.AreEqual(3, result.Options.ConfigVersion);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task DiscoverReplayPaths_FiltersIgnoredAndExistingReplays()
    {
        var root = CreateTempDirectory();
        var replayFolder = Path.Combine(root, "Replays");
        Directory.CreateDirectory(replayFolder);

        try
        {
            var newestIgnored = CreateReplay(replayFolder, "Direct Strike (5).SC2Replay", DateTime.UtcNow.AddMinutes(5));
            var existing = CreateReplay(replayFolder, "Direct Strike (4).SC2Replay", DateTime.UtcNow.AddMinutes(4));
            var expectedNewest = CreateReplay(replayFolder, "Direct Strike (3).SC2Replay", DateTime.UtcNow.AddMinutes(3));
            var expectedOldest = CreateReplay(replayFolder, "Direct Strike (2).SC2Replay", DateTime.UtcNow.AddMinutes(2));
            _ = CreateReplay(replayFolder, "Other Map.SC2Replay", DateTime.UtcNow.AddMinutes(10));
            _ = CreateReplay(replayFolder, "Direct Strike (1).SC2Replay", DateTime.UtcNow.AddMinutes(1));

            AppOptions config = new()
            {
                IgnoreReplays = [newestIgnored],
                Sc2Profiles =
                [
                    new()
                    {
                        Folder = replayFolder,
                        PlayerId = new(1, 1, 1)
                    }
                ]
            };

            var result = await DsstatsService.DiscoverReplayPaths(
                config,
                2,
                (paths, _) => Task.FromResult(paths
                    .Where(path => string.Equals(path, existing, StringComparison.OrdinalIgnoreCase))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)),
                CancellationToken.None);

            CollectionAssert.AreEqual(
                new[] { expectedNewest, expectedOldest },
                result.ReplayPaths.ToArray());
            Assert.AreEqual(1, result.ReplayFolderCount);
            Assert.AreEqual(5, result.ScannedReplayCount);
            Assert.AreEqual(1, result.IgnoredReplayCount);
            Assert.AreEqual(1, result.ExistingReplayCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateReplay(string folder, string fileName, DateTime creationTimeUtc)
    {
        var path = Path.Combine(folder, fileName);
        File.WriteAllText(path, string.Empty);
        File.SetCreationTimeUtc(path, creationTimeUtc);
        File.SetLastWriteTimeUtc(path, creationTimeUtc);
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsstats-service-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
