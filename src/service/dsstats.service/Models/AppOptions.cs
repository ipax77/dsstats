using dsstats.shared.Maui;

namespace dsstats.service.Models;

public class AppOptions
{
    public int ConfigVersion { get; init; } = 2;
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public List<Sc2ProfileDto> Sc2Profiles { get; set; } = [];
    public List<Sc2ProfileDto> IgnoreProfiles { get; set; } = [];
    public List<string> CustomFolders { get; set; } = [];
    public int CPUCores { get; set; } = 2;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; } = true;
    public DateTime UploadAskTime { get; set; }
    public List<string> IgnoreReplays { get; set; } = [];
    public string ReplayStartName { get; set; } = "Direct Strike";
}
