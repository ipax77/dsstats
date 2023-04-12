using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sc2dsstats.maui.Services;

internal class UserSettingsService
{
    public static readonly string ConfigFile = Path.Combine(FileSystem.Current.AppDataDirectory, "config.json");
    public static UserSettings UserSettings = new UserSettings();
    private SemaphoreSlim semaphoreSlim = new(1, 1);
    private static readonly bool debug = false;
    private readonly IServiceProvider serviceProvider;

    public UserSettingsService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        ReloadConfig();
        UserSettings.BattleNetInfos = GetBattleNetIds();
    }

    public void ReloadConfig()
    {
        if (!File.Exists(ConfigFile) || debug)
        {
            SetInitialNamesAndFolders();
            SaveConfig();
        }
        else
        {
            var userSettings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(ConfigFile));
            if (userSettings == null)
            {
                SetInitialNamesAndFolders();
                SaveConfig();
            }
            else
            {
                UserSettings = userSettings;

                if (UserSettings.AllowCleanUploads)
                {
                    UserSettings.AutoScanForNewReplays = true;
                    UserSettings.AllowUploads = true;
                }
            }

            if (UserSettings.DoV1_0_8_Init)
            {
                try
                {
                    DoV1_0_8InitJob();
                } catch (Exception ex)
                {
                    Console.WriteLine($"failed doinng v1.0.8 init job: {ex.Message}");
                }
                finally
                {
                    UserSettings.DoV1_0_8_Init = false;
                    SaveConfig();
                }
            }
        }
    }

    private static void SaveConfig()
    {
        var jsonConfig = JsonSerializer.Serialize(UserSettings);
        File.WriteAllText(ConfigFile, jsonConfig);
    }

    public async Task Save()
    {
        await semaphoreSlim.WaitAsync();

        try
        {
            SaveConfig();
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    internal static void SetInitialNamesAndFolders()
    {
        var sc2Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starcraft II");

        if (Directory.Exists(sc2Dir))
        {
            HashSet<string> playerNames = new();
            HashSet<string> folderNames = new();

            foreach (var file in Directory.GetFiles(sc2Dir, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                var target = GetShortcutTarget(file);

                if (target == null)
                {
                    continue;
                }

                Match m = Regex.Match(Path.GetFileName(file), @"(.*)_\d+\@\d+\.lnk$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    playerNames.Add(m.Groups[1].Value);
                }

                var replayDir = Path.Combine(target, "Replays", "Multiplayer");

                if (Directory.Exists(replayDir))
                {
                    folderNames.Add(replayDir);
                }
            }

            UserSettings.PlayerNames.Clear();
            UserSettings.ReplayPaths.Clear();

            UserSettings.PlayerNames.AddRange(playerNames);
            UserSettings.ReplayPaths.AddRange(folderNames);
        }

    }

    private static List<BattleNetInfo> GetBattleNetIds()
    {
        var docDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var sc2Dir = Path.Combine(docDir, "Starcraft II");
        var sc2AccDir = Path.Combine(sc2Dir, "Accounts");

        if (!Directory.Exists(sc2AccDir))
        {
            return new();
        }

        var sc2AccDirs = Directory.GetDirectories(sc2AccDir);

        List<BattleNetInfo> battleNetInfos = new();

        foreach (var accDir in sc2AccDirs)
        {
            if (int.TryParse(accDir.Split(Path.DirectorySeparatorChar).Last(), out int battleNetId))
            {
                BattleNetInfo battleNetInfo = new() { BattleNetId = battleNetId };

                foreach (var toonDir in Directory.GetDirectories(accDir))
                {
                    if (Directory.Exists(Path.Combine(toonDir, "Replays", "Multiplayer")))
                    {
                        var toonFolder = toonDir.Split(Path.DirectorySeparatorChar).Last();
                        int toonId = 0;
                        int regionId = 0;
                        int realmId = 1;
                        var match = Regex.Match(toonFolder, @"\d+$");
                        if (match.Success)
                        {
                            if (int.TryParse(match.Value, out int ttoonId))
                            {
                                toonId = ttoonId;
                            }
                        }
                        var regionMatch = Regex.Match(toonFolder, @"^\d+");
                        if (regionMatch.Success)
                        {
                            if (int.TryParse(regionMatch.Value, out int tregionId))
                            {
                                regionId = tregionId;
                            }
                        }

                        var realmMatch = Regex.Match(toonFolder, @"-(\d+)-");
                        if (realmMatch.Success)
                        {
                            if (int.TryParse(realmMatch.Value, out int trealmId))
                            {
                                realmId = trealmId;
                            }
                        }
                        battleNetInfo.ToonIds.Add(new() { RegionId = regionId, ToonId = toonId, RealmId = realmId });
                    }
                }
                battleNetInfos.Add(battleNetInfo);
            }
        }
        return battleNetInfos;
    }

    public List<RequestNames> GetDefaultPlayers()
    {
        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        List<RequestNames> requestNames = new();

        foreach (var name in UserSettingsService.UserSettings.PlayerNames)
        {
            var toonIds = ratingRepository.GetNameToonIds(name);
            if (toonIds.Any())
            {
                foreach (var toonId in toonIds)
                {
                    requestNames.Add(new()
                    {
                        Name = name,
                        ToonId = toonId
                    });
                }
            }
        }

        return requestNames;
    }

    private static string? GetShortcutTarget(string file)
    {
        try
        {
            if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
            {
                return null;
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
            {
                fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                uint flags = fileReader.ReadUInt32();        // Read flags
                if ((flags & 1) == 1)
                {                      // Bit 1 set means we have to
                                       // skip the shell item ID list
                    fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                    uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                    fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                }

                long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                             // structure begins
                uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                           // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                    // base pathname (target)
                long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                    // the base pathname. I don't need the 2 terminating nulls.
                char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                var link = new string(linkTarget);

                int begin = link.IndexOf("\0\0");
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2) + 2;
                    end = link.IndexOf('\0', end) + 1;

                    string firstPart = link[..begin];
                    string secondPart = link[end..];

                    return firstPart + secondPart;
                }
                else
                {
                    return link;
                }
            }
        }
        catch
        {
            return null;
        }
    }

    private void DoV1_0_8InitJob()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // remove skip replays to redecode NoSetupEvent errors
        var skipReplays = context.SkipReplays
            .ToList();

        if (skipReplays.Any())
        {
            context.SkipReplays.RemoveRange(skipReplays);
            context.SaveChanges();
        }

        // adjust replays including Computer players
        var computerReplays = context.Replays
            .Where(x => x.ReplayPlayers.Any(a => a.Player.ToonId == 0))
            .ToList();

        if (computerReplays.Any())
        {
            computerReplays.ForEach(f => f.GameMode = GameMode.Tutorial);
            context.SaveChanges();
        }

        // recalculate ratings
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        ratingsService.ProduceRatings(true).Wait();
    }
}



public record UserSettings
{
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public Guid DbGuid { get; set; } = Guid.Empty;
    public List<BattleNetInfo> BattleNetInfos { get; set; } = new();
    public int CpuCoresUsedForDecoding { get; set; } = 2;
    public bool AllowUploads { get; set; }
    public bool AllowCleanUploads { get; set; }
    public bool AutoScanForNewReplays { get; set; } = true;
    public string ReplayStartName { get; set; } = "Direct Strike";
    public List<string> PlayerNames { get; set; } = new();
    public List<string> ReplayPaths { get; set; } = new();
    public DateTime UploadAskTime { get; set; }
    public bool CheckForUpdates { get; set; } = true;
    public bool DoV1_0_8_Init { get; set; } = true;
    public bool DoV1_1_2_Init { get; set; } = true;
}

public record BattleNetInfo
{
    public int BattleNetId { get; set; }
    public List<ToonIdInfo> ToonIds { get; set; } = new();
}

public record ToonIdInfo
{
    public int RegionId { get; set; }
    public int ToonId { get; set; }
    public int RealmId { get; set; } = 1;
}