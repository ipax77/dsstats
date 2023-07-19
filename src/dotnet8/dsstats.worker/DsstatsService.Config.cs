using System.Text.Json;
using System.Text.RegularExpressions;

namespace dsstats.worker;

public partial class DsstatsService
{
    private AppConfigOptions AppConfigOptions = new();
    private object lockobject = new();

    private void AddExcludeReplays(List<string> excludeReplayFilenames)
    {
        if (excludeReplayFilenames.Count == 0)
        {
            return;
        }

        var hashset = excludeReplayFilenames.ToHashSet();
        hashset.UnionWith(AppConfigOptions.ExcludeReplays);
        AppConfigOptions.ExcludeFolders = hashset.OrderBy(o => o).ToList();
        SaveConfig(AppConfigOptions);
    }

    private void UpdateConfig()
    {
        if (!File.Exists(configFile))
        {
            AppConfigOptions = new();
            InitOptions();
        }
        else
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configFile));
                if (config == null)
                {
                    AppConfigOptions = new();
                    InitOptions();
                }
                else
                {
                    AppConfigOptions = config.AppConfigOptions;
                }
            }
            catch
            {
                AppConfigOptions = new();
                InitOptions();
            }
        }
    }

    private void SaveConfig(AppConfigOptions config)
    {
        lock (lockobject)
        {
            AppConfigOptions = config with { };
            var json = JsonSerializer.Serialize(new AppConfig() { AppConfigOptions = AppConfigOptions },
                new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(configFile, json);
        }
    }

    private void InitOptions()
    {
        SetNamesAndFolders();
        SaveConfig(AppConfigOptions);
    }

    private void SetNamesAndFolders()
    {
        HashSet<string> playerNames = new();
        HashSet<string> folderNames = new();
        HashSet<string> battlenetStrings = new();

        foreach (var sc2Dir in sc2Dirs)
        {
            if (!Directory.Exists(sc2Dir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(sc2Dir, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                var target = GetShortcutTarget(file);

                if (target == null)
                {
                    continue;
                }

                var battlenetString = Path.GetFileName(target);
                battlenetStrings.Add(battlenetString);

                Match m = LnkRegex().Match(Path.GetFileName(file));
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
        }

        AppConfigOptions.PlayerNames.Clear();
        AppConfigOptions.ReplayFolders.Clear();
        AppConfigOptions.BattlenetStrings.Clear();

        AppConfigOptions.PlayerNames.AddRange(playerNames);
        AppConfigOptions.ReplayFolders.AddRange(folderNames);
        AppConfigOptions.BattlenetStrings.AddRange(battlenetStrings);
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
            using System.IO.BinaryReader fileReader = new(fileStream);
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
        catch
        {
            return null;
        }
    }

    [GeneratedRegex("(.*)_\\d+\\@\\d+\\.lnk$", RegexOptions.IgnoreCase, "en")]
    private static partial Regex LnkRegex();
}

public class AppConfig
{
    public AppConfigOptions AppConfigOptions { get; set; } = new();
}

public record AppConfigOptions
{
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public List<string> PlayerNames { get; set; } = new();
    public List<string> ReplayFolders { get; set; } = new();
    public List<string> BattlenetStrings { get; set; } = new();
    public int CPUCores { get; set; } = 1;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; }
    public DateTime UploadAskTime { get; set; }
    public string ReplayStartName { get; set; } = "Direct Strike";
    public List<string> ExcludeFolders { get; set; } = new();
    public List<string> ExcludeReplays { get; set; } = new();
}