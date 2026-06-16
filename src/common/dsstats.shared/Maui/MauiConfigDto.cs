using dsstats.shared;

namespace dsstats.shared.Maui;

public enum MauiSessionWindowMode
{
    Time = 0,
    Count = 1,
}

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
    public MauiSessionWindowMode SessionWindowMode { get; set; } = MauiSessionWindowMode.Time;
    public int SessionWindowHours { get; set; } = 6;
    public int SessionWindowReplayCount { get; set; } = 10;
    public GameMode SessionWindowGameMode { get; set; } = GameMode.None;
    public bool SessionWindowInitialized { get; set; }
    public List<Sc2ProfileDto> Sc2Profiles { get; set; } = new();
    public List<MauiReplayFolderDto> ManualReplayFolders { get; set; } = new();
}

public sealed class Sc2ProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public bool Active { get; set; }
}

public sealed class MauiReplayFolderDto
{
    public int MauiReplayFolderId { get; set; }
    public string Folder { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? DetectedName { get; set; }
    public ToonIdDto? DetectedToonId { get; set; }
    public DateTime? DetectedAtUtc { get; set; }
    public int DetectedReplayCount { get; set; }
}
