namespace dsstats.discord.cli;

internal sealed class PatchNoteEntry
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public int Commander { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public string? SourceMessageId { get; init; }
    public int SourceSequence { get; init; }
    public required string Content { get; init; }
}

internal sealed record DiscordMessageBatch(string? LastMessageId, List<PatchNoteEntry> Entries);
