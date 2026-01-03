using dsstats.db;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace dsstats.maui.Services;

public partial class DsstatsService
{
    public event EventHandler? CultureChanged;

    private void OnCultureChanged()
    {
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<MauiConfig> GetConfig()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var config = await context.MauiConfig
            .Include(i => i.Sc2Profiles)
            .AsNoTracking()
            .OrderBy(o => o.MauiConfigId)
            .FirstOrDefaultAsync();

        if (config is null)
        {
            config = new();
            config.Sc2Profiles = GetInitialNamesAndFolders();
            context.MauiConfig.Add(config);
            await context.SaveChangesAsync();
        }
        return config;
    }

    public async Task SaveConfig(MauiConfig config)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var dbConfig = await context.MauiConfig
            .Include(c => c.Sc2Profiles)
            .FirstOrDefaultAsync();

        if (dbConfig is null)
        {
            context.MauiConfig.Add(config);
            await context.SaveChangesAsync();
            return;
        }

        bool cultureChanged = dbConfig.Culture != config.Culture;

        context.Entry(dbConfig).CurrentValues.SetValues(config);

        // Remove deleted profiles
        foreach (var existing in dbConfig.Sc2Profiles.ToList())
        {
            bool stillPresent = config.Sc2Profiles.Any(p =>
                p.ToonId.Region == existing.ToonId.Region &&
                p.ToonId.Realm == existing.ToonId.Realm &&
                p.ToonId.Id == existing.ToonId.Id);

            if (!stillPresent)
            {
                context.Sc2Profiles.Remove(existing);
            }
        }

        // Add or update profiles
        foreach (var profile in config.Sc2Profiles)
        {
            var existing = dbConfig.Sc2Profiles.FirstOrDefault(p =>
                p.ToonId.Region == profile.ToonId.Region &&
                p.ToonId.Realm == profile.ToonId.Realm &&
                p.ToonId.Id == profile.ToonId.Id);

            if (existing == null)
            {
                dbConfig.Sc2Profiles.Add(profile);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(profile);

                // Owned type must be updated explicitly
                context.Entry(existing).Reference(p => p.ToonId)
                    .CurrentValue = profile.ToonId;
            }
        }

        await context.SaveChangesAsync();

        if (cultureChanged)
        {
            OnCultureChanged();
        }
    }

    public static List<CultureInfo> GetSupportedCultures()
    {
        return
        [
            new CultureInfo("en"),
            new CultureInfo("de"),
            new CultureInfo("fr"),
            new CultureInfo("es"),
            new CultureInfo("ru"),
            new CultureInfo("uk"),
        ];
    }

    public static List<Sc2Profile> DiscoverProfiles()
    {
        return GetInitialNamesAndFolders();
    }

    public static List<Sc2Profile> GetInitialNamesAndFolders()
    {
        var sc2Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starcraft II");
        HashSet<Sc2Profile> profiles = [];

        if (Directory.Exists(sc2Dir))
        {
            foreach (var file in Directory.GetFiles(sc2Dir, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                var target = GetShortcutTarget(file);

                if (target == null)
                {
                    continue;
                }

                Sc2Profile profile = new() { Active = true };

                var battlenetString = Path.GetFileName(target);
                var playerId = GetPlayerIdFromFolder(battlenetString);
                if (playerId == null)
                {
                    continue;
                }
                profile.ToonId = playerId;

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

    private static ToonId? GetPlayerIdFromFolder(string folder)
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
                return new()
                {
                    Id = toonId,
                    Region = regionId,
                    Realm = realmId,
                };
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
