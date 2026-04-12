using dsstats.shared;

namespace dsstats.indexedDb.Services;

public enum SessionWindowModeDto
{
    Time = 0,
    Count = 1,
}

public sealed class TrackedProfileDto
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public bool Active { get; set; } = true;
    public bool AutoDetected { get; set; }
}

public sealed class ProfileCandidateDto
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public int Count { get; set; }
}

public sealed class SessionWindowSettingsDto
{
    public SessionWindowModeDto Mode { get; set; } = SessionWindowModeDto.Time;
    public int Hours { get; set; } = 6;
    public int ReplayCount { get; set; } = 10;
    public GameMode GameMode { get; set; } = GameMode.None;
}
