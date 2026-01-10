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
        Sc2Profiles = entity.Sc2Profiles
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
}