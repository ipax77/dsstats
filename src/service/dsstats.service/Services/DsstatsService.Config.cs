using dsstats.service.Models;
using dsstats.shared;
using Microsoft.Win32;
using System.Management;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService
{
    private readonly SemaphoreSlim configSemaphore = new(1, 1);
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

    internal async Task<AppOptions> GetConfig()
    {
        await configSemaphore.WaitAsync();
        try
        {
            AppOptions? appOptions = null;
            if (File.Exists(configFile))
            {
                appOptions = JsonSerializer.Deserialize<AppOptions>(await File.ReadAllTextAsync(configFile));
            }
            var folders = GetMyDocumentsPathAllUsers();
            var sc2profiles = GetInitialNamesAndFolders(folders);

            if (sc2profiles.Count == 0)
            {
                logger.LogWarning("No sc2 profiles found.");
            }

            if (appOptions is null)
            {
                appOptions = new()
                {
                    Sc2Profiles = sc2profiles
                };
                await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(appOptions, jsonSerializerOptions));
            }
            else
            {
                appOptions.Sc2Profiles = sc2profiles;
            }

            return appOptions;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting config: {error}", ex.Message);
            return new();
        }
        finally
        {
            configSemaphore.Release();
        }
    }

    internal static List<ToonIdDto> GetToonIds(AppOptions config)
    {
        return FilterProfiles(config)
            .Select(s => new ToonIdDto
            {
                Id = s.PlayerId.ToonId,
                Realm = s.PlayerId.RealmId,
                Region = s.PlayerId.RegionId
            })
            .ToList();
    }

    internal static List<string> GetReplayFolders(AppOptions config)
    {
        return FilterProfiles(config)
            .Select(s => s.Folder)
            .Concat(config.CustomFolders)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<Sc2Profile> FilterProfiles(AppOptions config)
    {
        var ignoreIds = config.IgnoreProfiles
            .Select(p => p.PlayerId)
            .ToHashSet();

        return config.Sc2Profiles
            .Where(p => !ignoreIds.Contains(p.PlayerId));
    }

    internal static List<Sc2Profile> GetInitialNamesAndFolders(List<string> folders)
    {
        HashSet<Sc2Profile> profiles = [];
        foreach (var folder in folders)
        {
            var sc2Dir = Path.Combine(folder, "Starcraft II");

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
            if (!Path.GetExtension(file).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
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

                int begin = link.IndexOf("\0\0", StringComparison.OrdinalIgnoreCase);
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2, StringComparison.OrdinalIgnoreCase) + 2;
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

    private static List<string> GetMyDocumentsPathAllUsers()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        const string parcialSubkey = @"\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";
        const string keyName = "Personal";

        //get sids
        List<string> sids = GetMachineSids();
        List<string> myDocumentsPaths = new();

        if (sids != null)
        {
            foreach (var sid in sids)
            {
                //get paths                  
                var subkey = sid + parcialSubkey;

                using var key = Registry.Users.OpenSubKey(subkey);
                if (key != null)
                {
                    var o = key.GetValue(keyName);
                    if (o != null)
                    {
                        var myDocumentPath = o.ToString();
                        if (myDocumentPath != null)
                        {
                            myDocumentsPaths.Add(myDocumentPath);
                        }
                    }
                }
            }
        }

        return myDocumentsPaths;
    }

    private static List<string> GetMachineSids()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new();
        }

        ManagementObjectSearcher searcher = new("SELECT * FROM Win32_UserProfile");
        var regs = searcher.Get();
        List<string> sids = new();

        foreach (ManagementObject os in regs.Cast<ManagementObject>())
        {
            if (os["SID"] != null)
            {
                var sid = os["SID"].ToString();
                if (sid != null)
                {
                    sids.Add(sid);
                }
            }
        }
        searcher.Dispose();
        return sids;
    }

    [GeneratedRegex(@"(.*)_\d+\@\d+\.lnk$", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRx();
    [GeneratedRegex(@"^(\d+)-S2-(\d+)-(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileRx();
}
