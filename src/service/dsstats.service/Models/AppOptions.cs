using System.Text.Json.Serialization;

namespace dsstats.service.Models;

internal sealed class AppOptions
{
    public int ConfigVersion { get; init; } = 3;
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    [JsonIgnore]
    public List<Sc2Profile> ActiveProfiles { get; set; } = [];
    [JsonIgnore]
    public List<Sc2Profile> Sc2Profiles { get; set; } = [];
    public List<Sc2Profile> IgnoreProfiles { get; set; } = [];
    public List<string> CustomFolders { get; set; } = [];
    public int CPUCores { get; set; } = 2;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; } = true;
    public DateTime UploadAskTime { get; set; }
    public List<string> IgnoreReplays { get; set; } = [];
    public string ReplayStartName { get; set; } = "Direct Strike";
}

internal sealed class Sc2Profile
{
    public string Name { get; set; } = string.Empty;
    public PlayerId PlayerId { get; set; } = new();
    public string Folder { get; set; } = string.Empty;
}

internal sealed record PlayerId
{
    public PlayerId()
    {

    }

    public PlayerId(int toonId, int realmId, int regionId)
    {
        ToonId = toonId;
        RealmId = realmId;
        RegionId = regionId;
    }

    public int ToonId { get; set; }
    public int RealmId { get; set; }
    public int RegionId { get; set; }
}