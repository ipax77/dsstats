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

public static class MauiConfigExtensions
{
    public static List<ToonIdDto> GetToonIdDtos(this MauiConfig config)
    {
        return config.Sc2Profiles
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
        .Select(p => p.ToDto()).ToList()
    };

    public static Sc2ProfileDto ToDto(this Sc2Profile entity) => new()
    {
        Sc2ProfileId = entity.Sc2ProfileId,
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
}

public static class MauiConfigPersistence
{
    public static List<Sc2ProfileIdentityAssignment> ApplyConfig(
        MauiConfig entity,
        MauiConfigDto dto,
        DsstatsContext context)
    {
        ApplyScalars(entity, dto);
        return ApplyProfiles(entity, dto, context);
    }

    public static void SyncGeneratedProfileIds(IEnumerable<Sc2ProfileIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            assignment.Dto.Sc2ProfileId = assignment.Entity.Sc2ProfileId;
        }
    }

    private static void ApplyScalars(MauiConfig entity, MauiConfigDto dto)
    {
        SetIfChanged(entity.Version, dto.Version, value => entity.Version = value);
        SetIfChanged(entity.CPUCores, dto.CPUCores, value => entity.CPUCores = value);
        SetIfChanged(entity.AutoDecode, dto.AutoDecode, value => entity.AutoDecode = value);
        SetIfChanged(entity.CheckForUpdates, dto.CheckForUpdates, value => entity.CheckForUpdates = value);
        SetIfChanged(entity.UploadCredential, dto.UploadCredential, value => entity.UploadCredential = value);
        SetIfChanged(entity.ReplayStartName, dto.ReplayStartName, value => entity.ReplayStartName = value);
        SetIfChanged(entity.Culture, dto.Culture, value => entity.Culture = value);
        SetIfChanged(entity.UploadAskTime, dto.UploadAskTime, value => entity.UploadAskTime = value);

        if (!entity.IgnoreReplays.SequenceEqual(dto.IgnoreReplays))
        {
            entity.IgnoreReplays = dto.IgnoreReplays;
        }

        var sessionWindowMode = dto.SessionWindowMode is MauiSessionWindowMode.Time or MauiSessionWindowMode.Count
            ? dto.SessionWindowMode
            : MauiSessionWindowMode.Time;
        SetIfChanged(entity.SessionWindowMode, sessionWindowMode, value => entity.SessionWindowMode = value);

        var sessionWindowHours = dto.SessionWindowHours switch
        {
            3 or 6 or 12 or 24 => dto.SessionWindowHours,
            _ => 6,
        };
        SetIfChanged(entity.SessionWindowHours, sessionWindowHours, value => entity.SessionWindowHours = value);

        var sessionWindowReplayCount = dto.SessionWindowReplayCount switch
        {
            10 or 20 or 30 or 50 => dto.SessionWindowReplayCount,
            _ => 10,
        };
        SetIfChanged(entity.SessionWindowReplayCount, sessionWindowReplayCount, value => entity.SessionWindowReplayCount = value);

        var sessionWindowGameMode = Enum.IsDefined(dto.SessionWindowGameMode)
            ? dto.SessionWindowGameMode
            : GameMode.None;
        SetIfChanged(entity.SessionWindowGameMode, sessionWindowGameMode, value => entity.SessionWindowGameMode = value);
        SetIfChanged(entity.SessionWindowInitialized, dto.SessionWindowInitialized, value => entity.SessionWindowInitialized = value);
    }

    private static List<Sc2ProfileIdentityAssignment> ApplyProfiles(
        MauiConfig entity,
        MauiConfigDto dto,
        DsstatsContext context)
    {
        var assignments = new List<Sc2ProfileIdentityAssignment>();
        var existingProfiles = entity.Sc2Profiles.ToList();
        var existingById = existingProfiles
            .Where(profile => profile.Sc2ProfileId > 0)
            .ToDictionary(profile => profile.Sc2ProfileId);
        var existingByToonId = BuildValidToonIdLookup(existingProfiles);
        var matchedProfiles = new HashSet<Sc2Profile>();

        foreach (var dtoProfile in dto.Sc2Profiles)
        {
            var profile = FindExistingProfile(dtoProfile, existingById, existingByToonId);

            if (profile is not null && matchedProfiles.Add(profile))
            {
                ApplyProfile(profile, dtoProfile);
                assignments.Add(new(dtoProfile, profile));
                continue;
            }

            profile = new Sc2Profile
            {
                ToonId = new ToonId()
            };
            ApplyProfile(profile, dtoProfile);
            entity.Sc2Profiles.Add(profile);
            matchedProfiles.Add(profile);
            assignments.Add(new(dtoProfile, profile));
        }

        foreach (var existing in existingProfiles)
        {
            if (!matchedProfiles.Contains(existing))
            {
                context.Sc2Profiles.Remove(existing);
            }
        }

        return assignments;
    }

    private static Dictionary<ToonIdKey, Sc2Profile> BuildValidToonIdLookup(IEnumerable<Sc2Profile> profiles)
    {
        Dictionary<ToonIdKey, Sc2Profile> profilesByToonId = [];
        foreach (var profile in profiles)
        {
            if (!TryGetValidToonIdKey(profile.ToonId, out var key))
            {
                continue;
            }

            profilesByToonId.TryAdd(key, profile);
        }

        return profilesByToonId;
    }

    private static Sc2Profile? FindExistingProfile(
        Sc2ProfileDto dtoProfile,
        IReadOnlyDictionary<int, Sc2Profile> existingById,
        IReadOnlyDictionary<ToonIdKey, Sc2Profile> existingByToonId)
    {
        if (dtoProfile.Sc2ProfileId > 0 &&
            existingById.TryGetValue(dtoProfile.Sc2ProfileId, out var existingByIdProfile))
        {
            return existingByIdProfile;
        }

        return TryGetValidToonIdKey(dtoProfile.ToonId, out var key) &&
            existingByToonId.TryGetValue(key, out var existingByToonIdProfile)
            ? existingByToonIdProfile
            : null;
    }

    private static void ApplyProfile(Sc2Profile entity, Sc2ProfileDto dto)
    {
        SetIfChanged(entity.Name, dto.Name, value => entity.Name = value);
        SetIfChanged(entity.Folder, dto.Folder, value => entity.Folder = value);
        SetIfChanged(entity.Active, dto.Active, value => entity.Active = value);

        entity.ToonId ??= new ToonId();
        SetIfChanged(entity.ToonId.Region, dto.ToonId.Region, value => entity.ToonId.Region = value);
        SetIfChanged(entity.ToonId.Realm, dto.ToonId.Realm, value => entity.ToonId.Realm = value);
        SetIfChanged(entity.ToonId.Id, dto.ToonId.Id, value => entity.ToonId.Id = value);
    }

    private static bool TryGetValidToonIdKey(ToonId toonId, out ToonIdKey key)
    {
        key = new(toonId.Region, toonId.Realm, toonId.Id);
        return toonId.Id > 0;
    }

    private static bool TryGetValidToonIdKey(ToonIdDto toonId, out ToonIdKey key)
    {
        key = new(toonId.Region, toonId.Realm, toonId.Id);
        return toonId.Id > 0;
    }

    private static void SetIfChanged<T>(T currentValue, T newValue, Action<T> setValue)
    {
        if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            setValue(newValue);
        }
    }

    private readonly record struct ToonIdKey(int Region, int Realm, int Id);
}

public readonly record struct Sc2ProfileIdentityAssignment(
    Sc2ProfileDto Dto,
    Sc2Profile Entity);
