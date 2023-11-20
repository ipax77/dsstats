using dsstats.shared;
using System.Text.Json.Serialization;

namespace dsstats.maui8;

public record AppConfig
{
    public AppOptions AppOptions { get; set; } = new();
}

public record AppOptions
{
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
}

public record Sc2Profile
{
    public string Name { get; set; } = string.Empty;
    public PlayerId PlayerId { get; set; } = new();
    public string Folder { get; set; } = string.Empty;
}