using dsstats.db;
using dsstats.shared.Maui;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace dsstats.maui.Services;

public partial class DsstatsService
{
    public event EventHandler? CultureChanged;
    public event EventHandler? IgnoreReplaysChanged;
    private readonly SemaphoreSlim configSemaphore = new(1, 1);
    private bool sc2ProfilesRefreshedFromDisk;

    private void OnCultureChanged()
    {
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnIgnoreReplaysChanged()
    {
        IgnoreReplaysChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<MauiConfig> GetConfig()
    {
        await configSemaphore.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            var config = await GetOrCreateConfig(context);
            return config;
        }
        finally
        {
            configSemaphore.Release();
        }
    }

    public async Task<MauiConfigDto> GetConfigDto()
    {
        await configSemaphore.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            var config = await GetOrCreateConfig(context);
            return config.ToDto();
        }
        finally
        {
            configSemaphore.Release();
        }
    }

    public async Task SaveConfig(MauiConfigDto dto)
    {
        bool ignoreReplaysChanged = false;
        bool configSaved = false;
        await configSemaphore.WaitAsync();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            var dbConfig = await context.MauiConfig
                .Include(c => c.Sc2Profiles)
                .Include(c => c.ManualReplayFolders)
                .FirstOrDefaultAsync();

            if (dbConfig == null)
            {
                dbConfig = new MauiConfig();
                MauiConfigPersistence.RefreshDiscoveredProfileDtos(dto, GetInitialNamesAndFolders());
                var previousIgnoreReplays = dbConfig.IgnoreReplays.ToArray();
                var newFolderIdAssignments = MauiConfigPersistence.ApplyConfig(dbConfig, dto, context);
                ignoreReplaysChanged = !previousIgnoreReplays.SequenceEqual(
                    dbConfig.IgnoreReplays,
                    StringComparer.OrdinalIgnoreCase);

                context.MauiConfig.Add(dbConfig);
                await context.SaveChangesAsync();
                configSaved = true;
                MauiConfigPersistence.SyncGeneratedManualReplayFolderIds(newFolderIdAssignments);
                sc2ProfilesRefreshedFromDisk = true;
                return;
            }

            bool cultureChanged = dbConfig.Culture != dto.Culture;
            var oldIgnoreReplays = dbConfig.IgnoreReplays.ToArray();

            var folderIdAssignments = MauiConfigPersistence.ApplyConfig(dbConfig, dto, context);
            ignoreReplaysChanged = !oldIgnoreReplays.SequenceEqual(
                dbConfig.IgnoreReplays,
                StringComparer.OrdinalIgnoreCase);
            await context.SaveChangesAsync();
            configSaved = true;
            MauiConfigPersistence.SyncGeneratedManualReplayFolderIds(folderIdAssignments);

            if (cultureChanged)
            {
                OnCultureChanged();
            }
        }
        finally
        {
            configSemaphore.Release();

            if (configSaved && ignoreReplaysChanged)
            {
                OnIgnoreReplaysChanged();
            }
        }
    }

    public void RefreshDiscoveredProfiles(MauiConfigDto dto)
    {
        MauiConfigPersistence.RefreshDiscoveredProfileDtos(dto, GetInitialNamesAndFolders());
    }

    private async Task<MauiConfig> GetOrCreateConfig(DsstatsContext context)
    {
        var config = await context.MauiConfig
            .Include(i => i.Sc2Profiles)
            .Include(i => i.ManualReplayFolders)
            .OrderBy(o => o.MauiConfigId)
            .FirstOrDefaultAsync();

        if (config is null)
        {
            config = new();
            config.Sc2Profiles = GetInitialNamesAndFolders();
            context.MauiConfig.Add(config);
            await context.SaveChangesAsync();
            sc2ProfilesRefreshedFromDisk = true;
            return config;
        }

        if (!sc2ProfilesRefreshedFromDisk)
        {
            var changed = MauiConfigPersistence.RefreshDiscoveredProfiles(
                config,
                GetInitialNamesAndFolders(),
                context);

            if (changed)
            {
                await context.SaveChangesAsync();
            }

            sc2ProfilesRefreshedFromDisk = true;
        }

        return config;
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
        Dictionary<(int Region, int Realm, int Id), Sc2Profile> profiles = [];

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
                profiles.TryAdd(
                    (profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id),
                    profile);
            }
        }
        return profiles.Values.ToList();
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
