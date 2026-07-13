using dsstats.shared;

namespace dsstats.db;

public enum PatchNoteSource
{
    LegacySql = 1,
    Manual = 2,
    Discord = 3
}

public sealed class PatchNote
{
    public long PatchNoteId { get; set; }
    public required string SourceKey { get; set; }
    public PatchNoteSource Source { get; set; }
    public string? SourceMessageId { get; set; }
    public int SourceSequence { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public Commander Commander { get; set; }
    public required string Content { get; set; }
}

public sealed class PatchNoteSyncState
{
    public const string DiscordStateId = "discord";

    public required string PatchNoteSyncStateId { get; set; }
    public string? Cursor { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
