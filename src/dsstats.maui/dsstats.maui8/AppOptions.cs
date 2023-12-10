using dsstats.db8;
using dsstats.shared;
using System.Text.Json.Serialization;

namespace dsstats.maui8;

public record AppConfig
{
    public AppOptions AppOptions { get; set; } = new();
}

public record AppOptions
{
    public int ConfigVersion { get; init; } = 2;
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public List<Sc2Profile> ActiveProfiles { get; set; } = new();
    [JsonIgnore]
    public List<Sc2Profile> Sc2Profiles { get; set; } = new();
    public List<Sc2Profile> IgnoreProfiles { get; set; } = new();
    public List<string> CustomFolders { get; set; } = new();
    public int CPUCores { get; set; } = 2;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; }
    public DateTime UploadAskTime { get; set; }
    public List<string> IgnoreReplays { get; set; } = new();
    public string ReplayStartName { get; set; } = "Direct Strike";
    public string Culture { get; set; } = "iv";
}

public record Sc2Profile
{
    public string Name { get; set; } = string.Empty;
    public PlayerId PlayerId { get; set; } = new();
    public string Folder { get; set; } = string.Empty;
}

public record UserSettingsV6
{
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public Guid DbGuid { get; set; } = Guid.Empty;
    public List<BattleNetInfoV6> BattleNetInfos { get; set; } = new();
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

public record BattleNetInfoV6
{
    public int BattleNetId { get; set; }
    public List<ToonIdInfoV6> ToonIds { get; set; } = new();
}

public record ToonIdInfoV6
{
    public int RegionId { get; set; }
    public int ToonId { get; set; }
    public int RealmId { get; set; } = 1;
}