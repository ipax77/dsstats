namespace sc2dsstats.maui.Configuration;

public class AppConfig
{
    public AppConfigOptions AppConfigOptions { get; set; } = new();
}

public record AppConfigOptions
{
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public List<string> PlayerNames { get; set; } = new();
    public List<string> ReplayFolders { get; set; } = new();
    public List<string> BattlenetStrings { get; set; } = new();
    public int CPUCores { get; set; } = 2;
    public bool AutoDecode { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UploadCredential { get; set; }
    public DateTime UploadAskTime { get; set; }
    public string ReplayStartName { get; set; } = "Direct Strike";
}
