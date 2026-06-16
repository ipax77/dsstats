using dsstats.shared;
using dsstats.shared.Maui;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class MauiConfig
{
    public int MauiConfigId { get; set; }
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    [MaxLength(10)]
    public string Version { get; set; } = "3.0.3";
    public int CPUCores { get; set; } = 2;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; }
    public string ReplayStartName { get; set; } = "Direct Strike";
    public string Culture { get; set; } = "iv";
    [Precision(0)]
    public DateTime UploadAskTime { get; set; }
    public string[] IgnoreReplays { get; set; } = [];
    public MauiSessionWindowMode SessionWindowMode { get; set; } = MauiSessionWindowMode.Time;
    public int SessionWindowHours { get; set; } = 6;
    public int SessionWindowReplayCount { get; set; } = 10;
    public GameMode SessionWindowGameMode { get; set; } = GameMode.None;
    public bool SessionWindowInitialized { get; set; }
    public ICollection<Sc2Profile> Sc2Profiles { get; set; } = [];
    public ICollection<MauiReplayFolder> ManualReplayFolders { get; set; } = [];
}

public sealed class Sc2Profile
{
    public int Sc2ProfileId { get; set; }
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Folder { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = null!;
    public bool Active { get; set; }
    public int MauiConfigId { get; set; }
    public MauiConfig? MauiConfig { get; set; }
}

public sealed class MauiReplayFolder
{
    public int MauiReplayFolderId { get; set; }
    [MaxLength(200)]
    public string Folder { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    [MaxLength(30)]
    public string? DetectedName { get; set; }
    public int? DetectedToonIdRegion { get; set; }
    public int? DetectedToonIdRealm { get; set; }
    public int? DetectedToonIdId { get; set; }
    [Precision(0)]
    public DateTime? DetectedAtUtc { get; set; }
    public int DetectedReplayCount { get; set; }
    public int MauiConfigId { get; set; }
    public MauiConfig? MauiConfig { get; set; }
}

public static class MauiConfigExtensions
{
    public static List<ToonIdDto> GetToonIdDtos(this MauiConfig config)
    {
        return config.Sc2Profiles
            .Where(p => p.ToonId.Id > 0)
            .Select(p => new ToonIdDto
            {
                Region = p.ToonId.Region,
                Realm = p.ToonId.Realm,
                Id = p.ToonId.Id
            })
            .ToList();
    }

    public static MauiConfigDto ToDto(this MauiConfig entity) => new()
    {
        Version = entity.Version,
        CPUCores = entity.CPUCores,
        AutoDecode = entity.AutoDecode,
        CheckForUpdates = entity.CheckForUpdates,
        UploadCredential = entity.UploadCredential,
        ReplayStartName = entity.ReplayStartName,
        Culture = entity.Culture,
        UploadAskTime = entity.UploadAskTime,
        IgnoreReplays = entity.IgnoreReplays,
        SessionWindowMode = entity.SessionWindowMode,
        SessionWindowHours = entity.SessionWindowHours,
        SessionWindowReplayCount = entity.SessionWindowReplayCount,
        SessionWindowGameMode = entity.SessionWindowGameMode,
        SessionWindowInitialized = entity.SessionWindowInitialized,
        Sc2Profiles = entity.Sc2Profiles
            .Select(p => p.ToDto()).ToList(),
        ManualReplayFolders = entity.ManualReplayFolders
            .Select(p => p.ToDto()).ToList()
    };

    public static Sc2ProfileDto ToDto(this Sc2Profile entity) => new()
    {
        Name = entity.Name,
        Folder = entity.Folder,
        Active = entity.Active,
        ToonId = new ToonIdDto
        {
            Region = entity.ToonId.Region,
            Realm = entity.ToonId.Realm,
            Id = entity.ToonId.Id
        }
    };

    public static MauiReplayFolderDto ToDto(this MauiReplayFolder entity) => new()
    {
        MauiReplayFolderId = entity.MauiReplayFolderId,
        Folder = entity.Folder,
        Active = entity.Active,
        DetectedName = entity.DetectedName,
        DetectedToonId = entity.DetectedToonIdId > 0
            ? new()
            {
                Region = entity.DetectedToonIdRegion.GetValueOrDefault(),
                Realm = entity.DetectedToonIdRealm.GetValueOrDefault(),
                Id = entity.DetectedToonIdId.GetValueOrDefault(),
            }
            : null,
        DetectedAtUtc = entity.DetectedAtUtc,
        DetectedReplayCount = entity.DetectedReplayCount,
    };
}

public static class MauiConfigPersistence
{
    public static string NormalizeFolderPath(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        return Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(folder.Trim()));
    }

    public static string NormalizeReplayPath(string replayPath)
    {
        if (string.IsNullOrWhiteSpace(replayPath))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(replayPath.Trim());
        }
        catch (Exception)
        {
            return replayPath.Trim();
        }
    }

    public static List<MauiReplayFolderAssignment> ApplyConfig(
        MauiConfig entity,
        MauiConfigDto dto,
        DsstatsContext context)
    {
        ApplyScalars(entity, dto);
        ApplySc2ProfileActivation(entity, dto);
        return ApplyManualReplayFolders(entity, dto, context);
    }

    public static bool RefreshDiscoveredProfiles(
        MauiConfig entity,
        IEnumerable<Sc2Profile> discoveredProfiles,
        DsstatsContext context)
    {
        var changed = MigrateInvalidProfilesToManualReplayFolders(entity, context);
        var discoveredByToonId = BuildValidToonIdLookup(discoveredProfiles);
        var existingProfiles = entity.Sc2Profiles.ToList();
        var existingByToonId = BuildValidToonIdLookup(existingProfiles);
        HashSet<Sc2Profile> matchedProfiles = [];

        foreach (var discoveredEntry in discoveredByToonId)
        {
            var key = discoveredEntry.Key;
            var discoveredProfile = discoveredEntry.Value;
            if (existingByToonId.TryGetValue(key, out var existingProfile))
            {
                matchedProfiles.Add(existingProfile);
                changed |= SetIfChanged(existingProfile.Name, discoveredProfile.Name, value => existingProfile.Name = value);
                changed |= SetIfChanged(existingProfile.Folder, discoveredProfile.Folder, value => existingProfile.Folder = value);
                continue;
            }

            var newProfile = new Sc2Profile
            {
                Name = discoveredProfile.Name,
                Folder = discoveredProfile.Folder,
                Active = true,
                ToonId = new ToonId
                {
                    Region = discoveredProfile.ToonId.Region,
                    Realm = discoveredProfile.ToonId.Realm,
                    Id = discoveredProfile.ToonId.Id,
                }
            };
            entity.Sc2Profiles.Add(newProfile);
            matchedProfiles.Add(newProfile);
            changed = true;
        }

        foreach (var existingProfile in existingProfiles)
        {
            if (!IsValidToonId(existingProfile.ToonId))
            {
                continue;
            }

            if (!matchedProfiles.Contains(existingProfile))
            {
                context.Sc2Profiles.Remove(existingProfile);
                changed = true;
            }
        }

        return changed;
    }

    public static void RefreshDiscoveredProfileDtos(
        MauiConfigDto dto,
        IEnumerable<Sc2Profile> discoveredProfiles)
    {
        var existingByToonId = dto.Sc2Profiles
            .Where(profile => IsValidToonId(profile.ToonId))
            .ToDictionary(profile => new ToonIdKey(profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id));

        List<Sc2ProfileDto> profiles = [];
        foreach (var discoveredProfile in discoveredProfiles)
        {
            if (!IsValidToonId(discoveredProfile.ToonId))
            {
                continue;
            }

            var key = new ToonIdKey(
                discoveredProfile.ToonId.Region,
                discoveredProfile.ToonId.Realm,
                discoveredProfile.ToonId.Id);

            profiles.Add(new()
            {
                Name = discoveredProfile.Name,
                Folder = discoveredProfile.Folder,
                Active = existingByToonId.TryGetValue(key, out var existingProfile)
                    ? existingProfile.Active
                    : discoveredProfile.Active,
                ToonId = new()
                {
                    Region = key.Region,
                    Realm = key.Realm,
                    Id = key.Id,
                }
            });
        }

        dto.Sc2Profiles = profiles;
    }

    public static void SyncGeneratedManualReplayFolderIds(IEnumerable<MauiReplayFolderAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            assignment.Dto.MauiReplayFolderId = assignment.Entity.MauiReplayFolderId;
        }
    }

    private static void ApplyScalars(MauiConfig entity, MauiConfigDto dto)
    {
        _ = SetIfChanged(entity.Version, dto.Version, value => entity.Version = value);
        _ = SetIfChanged(entity.CPUCores, dto.CPUCores, value => entity.CPUCores = value);
        _ = SetIfChanged(entity.AutoDecode, dto.AutoDecode, value => entity.AutoDecode = value);
        _ = SetIfChanged(entity.CheckForUpdates, dto.CheckForUpdates, value => entity.CheckForUpdates = value);
        _ = SetIfChanged(entity.UploadCredential, dto.UploadCredential, value => entity.UploadCredential = value);
        _ = SetIfChanged(entity.ReplayStartName, dto.ReplayStartName, value => entity.ReplayStartName = value);
        _ = SetIfChanged(entity.Culture, dto.Culture, value => entity.Culture = value);
        _ = SetIfChanged(entity.UploadAskTime, dto.UploadAskTime, value => entity.UploadAskTime = value);

        var ignoreReplays = dto.IgnoreReplays
            .Select(NormalizeReplayPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!entity.IgnoreReplays.SequenceEqual(ignoreReplays, StringComparer.OrdinalIgnoreCase))
        {
            entity.IgnoreReplays = ignoreReplays;
        }
        dto.IgnoreReplays = ignoreReplays;

        var sessionWindowMode = dto.SessionWindowMode is MauiSessionWindowMode.Time or MauiSessionWindowMode.Count
            ? dto.SessionWindowMode
            : MauiSessionWindowMode.Time;
        _ = SetIfChanged(entity.SessionWindowMode, sessionWindowMode, value => entity.SessionWindowMode = value);

        var sessionWindowHours = dto.SessionWindowHours switch
        {
            3 or 6 or 12 or 24 => dto.SessionWindowHours,
            _ => 6,
        };
        _ = SetIfChanged(entity.SessionWindowHours, sessionWindowHours, value => entity.SessionWindowHours = value);

        var sessionWindowReplayCount = dto.SessionWindowReplayCount switch
        {
            10 or 20 or 30 or 50 => dto.SessionWindowReplayCount,
            _ => 10,
        };
        _ = SetIfChanged(entity.SessionWindowReplayCount, sessionWindowReplayCount, value => entity.SessionWindowReplayCount = value);

        var sessionWindowGameMode = Enum.IsDefined(dto.SessionWindowGameMode)
            ? dto.SessionWindowGameMode
            : GameMode.None;
        _ = SetIfChanged(entity.SessionWindowGameMode, sessionWindowGameMode, value => entity.SessionWindowGameMode = value);
        _ = SetIfChanged(entity.SessionWindowInitialized, dto.SessionWindowInitialized, value => entity.SessionWindowInitialized = value);
    }

    private static void ApplySc2ProfileActivation(MauiConfig entity, MauiConfigDto dto)
    {
        var activeByToonId = dto.Sc2Profiles
            .Where(profile => IsValidToonId(profile.ToonId))
            .ToDictionary(
                profile => new ToonIdKey(profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id),
                profile => profile.Active);

        foreach (var profile in entity.Sc2Profiles)
        {
            if (!IsValidToonId(profile.ToonId))
            {
                continue;
            }

            var key = new ToonIdKey(profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id);
            if (activeByToonId.TryGetValue(key, out var active))
            {
                _ = SetIfChanged(profile.Active, active, value => profile.Active = value);
            }
        }
    }

    private static List<MauiReplayFolderAssignment> ApplyManualReplayFolders(
        MauiConfig entity,
        MauiConfigDto dto,
        DsstatsContext context)
    {
        var assignments = new List<MauiReplayFolderAssignment>();
        var existingFolders = entity.ManualReplayFolders.ToList();
        var existingById = existingFolders
            .Where(folder => folder.MauiReplayFolderId > 0)
            .ToDictionary(folder => folder.MauiReplayFolderId);
        var existingByPath = existingFolders
            .GroupBy(folder => NormalizeFolderPath(folder.Folder), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        HashSet<MauiReplayFolder> matchedFolders = [];
        HashSet<string> matchedPaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (var dtoFolder in dto.ManualReplayFolders)
        {
            var normalizedFolder = NormalizeFolderPath(dtoFolder.Folder);
            if (string.IsNullOrWhiteSpace(normalizedFolder) || !matchedPaths.Add(normalizedFolder))
            {
                continue;
            }

            if (dtoFolder.MauiReplayFolderId > 0 &&
                existingById.TryGetValue(dtoFolder.MauiReplayFolderId, out var folder) &&
                matchedFolders.Add(folder))
            {
                ApplyManualReplayFolder(folder, dtoFolder);
                assignments.Add(new(dtoFolder, folder));
                continue;
            }

            if (existingByPath.TryGetValue(normalizedFolder, out folder) &&
                matchedFolders.Add(folder))
            {
                ApplyManualReplayFolder(folder, dtoFolder);
                assignments.Add(new(dtoFolder, folder));
                continue;
            }

            folder = new MauiReplayFolder();
            ApplyManualReplayFolder(folder, dtoFolder);
            entity.ManualReplayFolders.Add(folder);
            matchedFolders.Add(folder);
            assignments.Add(new(dtoFolder, folder));
        }

        foreach (var existingFolder in existingFolders)
        {
            if (!matchedFolders.Contains(existingFolder))
            {
                context.Set<MauiReplayFolder>().Remove(existingFolder);
            }
        }

        return assignments;
    }

    private static bool MigrateInvalidProfilesToManualReplayFolders(
        MauiConfig entity,
        DsstatsContext context)
    {
        var changed = false;
        HashSet<string> manualFolders = entity.ManualReplayFolders
            .Select(folder => NormalizeFolderPath(folder.Folder))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in entity.Sc2Profiles.ToList())
        {
            if (IsValidToonId(profile.ToonId))
            {
                continue;
            }

            var normalizedFolder = NormalizeFolderPath(profile.Folder);
            if (!string.IsNullOrWhiteSpace(normalizedFolder) && manualFolders.Add(normalizedFolder))
            {
                entity.ManualReplayFolders.Add(new()
                {
                    Folder = normalizedFolder,
                    Active = profile.Active,
                });
            }

            context.Sc2Profiles.Remove(profile);
            changed = true;
        }

        return changed;
    }

    private static Dictionary<ToonIdKey, Sc2Profile> BuildValidToonIdLookup(IEnumerable<Sc2Profile> profiles)
    {
        Dictionary<ToonIdKey, Sc2Profile> profilesByToonId = [];
        foreach (var profile in profiles)
        {
            if (!IsValidToonId(profile.ToonId))
            {
                continue;
            }

            profilesByToonId.TryAdd(new(profile.ToonId.Region, profile.ToonId.Realm, profile.ToonId.Id), profile);
        }

        return profilesByToonId;
    }

    private static void ApplyManualReplayFolder(MauiReplayFolder entity, MauiReplayFolderDto dto)
    {
        var normalizedFolder = NormalizeFolderPath(dto.Folder);
        var folderChanged = !string.Equals(entity.Folder, normalizedFolder, StringComparison.OrdinalIgnoreCase);
        _ = SetIfChanged(entity.Folder, normalizedFolder, value => entity.Folder = value);
        dto.Folder = normalizedFolder;
        _ = SetIfChanged(entity.Active, dto.Active, value => entity.Active = value);

        if (folderChanged)
        {
            ClearDetectedProfile(entity);
            dto.DetectedName = null;
            dto.DetectedToonId = null;
            dto.DetectedAtUtc = null;
            dto.DetectedReplayCount = 0;
        }
    }

    public static bool SetDetectedProfile(
        MauiReplayFolder entity,
        string name,
        ToonIdDto toonId,
        DateTime detectedAtUtc,
        int replayCount)
    {
        var changed = false;
        changed |= SetIfChanged(entity.DetectedName, name, value => entity.DetectedName = value);
        changed |= SetIfChanged(entity.DetectedToonIdRegion, toonId.Region, value => entity.DetectedToonIdRegion = value);
        changed |= SetIfChanged(entity.DetectedToonIdRealm, toonId.Realm, value => entity.DetectedToonIdRealm = value);
        changed |= SetIfChanged(entity.DetectedToonIdId, toonId.Id, value => entity.DetectedToonIdId = value);
        changed |= SetIfChanged(entity.DetectedAtUtc, detectedAtUtc, value => entity.DetectedAtUtc = value);
        changed |= SetIfChanged(entity.DetectedReplayCount, replayCount, value => entity.DetectedReplayCount = value);
        return changed;
    }

    private static void ClearDetectedProfile(MauiReplayFolder entity)
    {
        entity.DetectedName = null;
        entity.DetectedToonIdRegion = null;
        entity.DetectedToonIdRealm = null;
        entity.DetectedToonIdId = null;
        entity.DetectedAtUtc = null;
        entity.DetectedReplayCount = 0;
    }

    private static bool IsValidToonId(ToonId toonId)
        => toonId.Id > 0;

    private static bool IsValidToonId(ToonIdDto toonId)
        => toonId.Id > 0;

    private static bool SetIfChanged<T>(T currentValue, T newValue, Action<T> setValue)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setValue(newValue);
        return true;
    }

    private readonly record struct ToonIdKey(int Region, int Realm, int Id);
}

public readonly record struct MauiReplayFolderAssignment(
    MauiReplayFolderDto Dto,
    MauiReplayFolder Entity);

public static class MauiDuplicateReplayPathDetector
{
    public static string[] GetDuplicateReplayPaths(
        IReadOnlyCollection<ReplayImportDto> imports,
        IReadOnlySet<string> existingReplayHashes,
        IEnumerable<string> ignoreReplays)
    {
        if (imports.Count == 0)
        {
            return [];
        }

        HashSet<string> ignoredPaths = ignoreReplays
            .Select(MauiConfigPersistence.NormalizeReplayPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> importedReplayHashes = existingReplayHashes
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> duplicatePaths = new(StringComparer.OrdinalIgnoreCase);
        List<ReplayImportCandidate> candidates = new(imports.Count);

        var index = 0;
        foreach (var import in imports)
        {
            var path = MauiConfigPersistence.NormalizeReplayPath(import.Replay.FileName);
            if (string.IsNullOrWhiteSpace(path) || ignoredPaths.Contains(path))
            {
                index++;
                continue;
            }

            var replayHash = import.Replay.ComputeHash();
            candidates.Add(new(replayHash, path, import.Replay.Duration, index++));
        }

        foreach (var candidate in candidates)
        {
            if (importedReplayHashes.Contains(candidate.ReplayHash))
            {
                duplicatePaths.Add(candidate.Path);
            }
        }

        foreach (var group in candidates
            .Where(candidate => !importedReplayHashes.Contains(candidate.ReplayHash))
            .GroupBy(candidate => candidate.ReplayHash, StringComparer.OrdinalIgnoreCase))
        {
            var keeper = group
                .OrderByDescending(candidate => candidate.Duration)
                .ThenBy(candidate => candidate.Index)
                .First();

            foreach (var duplicate in group.Where(candidate => candidate.Index != keeper.Index))
            {
                duplicatePaths.Add(duplicate.Path);
            }
        }

        return duplicatePaths
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record ReplayImportCandidate(
        string ReplayHash,
        string Path,
        int Duration,
        int Index);
}
