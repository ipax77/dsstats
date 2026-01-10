namespace dsstats.shared.Maui;

public sealed class MauiConfigDto
{
    public string Version { get; set; } = string.Empty;
    public int CPUCores { get; set; }
    public bool AutoDecode { get; set; }
    public bool CheckForUpdates { get; set; }
    public bool UploadCredential { get; set; }
    public string ReplayStartName { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public DateTime UploadAskTime { get; set; }
    public string[] IgnoreReplays { get; set; } = [];
    public List<Sc2ProfileDto> Sc2Profiles { get; set; } = new();
}

public sealed class Sc2ProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public bool Active { get; set; }
}
