namespace dsstats.shared;

public class PwaConfig
{
    public string ConfigVersion { get; init; } = "77.0.0";
    public Guid AppGuid { get; set; } = Guid.NewGuid();
    public int CPUCores { get; set; } = 2;
    public bool UploadCredential { get; set; } = true;
    public List<string> IgnoreReplays { get; set; } = [];
    public string ReplayStartName { get; set; } = "Direct Strike";
    public string Culture { get; set; } = "iv";
}