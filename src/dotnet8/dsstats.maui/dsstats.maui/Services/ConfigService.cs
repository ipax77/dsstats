using dsstats.shared;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace dsstats.maui.Services;

public partial class ConfigService
{
    public static readonly string ConfigFile = Path.Combine(FileSystem.Current.AppDataDirectory, "config.json");
    public AppOptions AppOptions { get; private set; }
    private object lockobject = new();

    public ConfigService()
    {
        if (!File.Exists(ConfigFile))
        {
            AppOptions = new();
            InitOptions();
        }
        else
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile));
                if (config == null)
                {
                    AppOptions = new();
                    InitOptions();
                }
                else
                {
                    AppOptions = config.AppOptions;
                }
            }
            catch
            {
                AppOptions = new();
                InitOptions();
            }
        }
        AppOptions.Sc2Profiles = GetInitialNamesAndFolders();
        AppOptions.ActiveProfiles = AppOptions.Sc2Profiles
            .Except(AppOptions.IgnoreProfiles)
            .Distinct()
            .ToList();
    }

    public List<RequestNames> GetRequestNames()
    {
        return AppOptions.ActiveProfiles.Select(s => new RequestNames()
        {
            Name = s.Name,
            ToonId = s.PlayerId.ToonId,
            RealmId = s.PlayerId.RealmId,
            RegionId = s.PlayerId.RegionId
        }).ToList();
    }

    public List<string> GetReplayFolders()
    {
        HashSet<string> folders = AppOptions.ActiveProfiles.Select(s => s.Folder).ToHashSet();
        folders.UnionWith(AppOptions.CustomFolders);
        return folders.ToList();
    }

    public void UpdateConfig(AppOptions config)
    {
        lock (lockobject)
        {
            AppOptions = config with { };
            var json = JsonSerializer.Serialize(new AppConfig() { AppOptions = AppOptions });
            File.WriteAllText(ConfigFile, json);

            AppOptions.ActiveProfiles = AppOptions.Sc2Profiles
                .Except(AppOptions.IgnoreProfiles)
                .Distinct()
                .ToList();
            }
    }

    public void InitOptions()
    {
        UpdateConfig(AppOptions);
    }

    private List<Sc2Profile> GetInitialNamesAndFolders()
    {
        var sc2Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starcraft II");
        HashSet<Sc2Profile> profiles = new();

        if (Directory.Exists(sc2Dir))
        {
            foreach (var file in Directory.GetFiles(sc2Dir, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                var target = GetShortcutTarget(file);

                if (target == null)
                {
                    continue;
                }

                Sc2Profile profile = new();

                var battlenetString = Path.GetFileName(target);
                var playerId = GetPlayerIdFromFolder(battlenetString);
                if (playerId == null)
                {
                    continue;
                }
                profile.PlayerId = playerId;

                Match m = LinkRx().Match(Path.GetFileName(file));
                if (m.Success)
                {
                    profile.Name = m.Groups[1].Value;
                }

                var replayDir = Path.Combine(target, "Replays", "Multiplayer");

                if (Directory.Exists(replayDir))
                {
                    profile.Folder = replayDir;
                }
                else
                {
                    continue;
                }
                profiles.Add(profile);
            }
        }
        return profiles.ToList();
    }

    private static PlayerId? GetPlayerIdFromFolder(string folder)
    {
        Match m = ProfileRx().Match(folder);
        if (m.Success)
        {
            var region = m.Groups[1].Value;
            var realm = m.Groups[2].Value;
            var toon = m.Groups[3].Value;

            if (int.TryParse(region, out int regionId)
                && int.TryParse(realm, out int realmId)
                && int.TryParse(toon, out int toonId))
            {
                return new(toonId, realmId, regionId);
            }
        }
        return null;
    }

    private static string? GetShortcutTarget(string file)
    {
        try
        {
            if (Path.GetExtension(file).ToLower() != ".lnk")
            {
                return null;
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (BinaryReader fileReader = new BinaryReader(fileStream))
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

    [GeneratedRegex(@"(.*)_\d+\@\d+\.lnk$", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRx();
    [GeneratedRegex(@"^(\d+)-S2-(\d+)-(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileRx();
}
