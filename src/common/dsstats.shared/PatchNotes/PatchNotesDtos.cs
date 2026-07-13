namespace dsstats.shared.PatchNotes;

public enum PatchNotesSort
{
    NewestFirst = 0,
    OldestFirst = 1
}

public sealed record PatchNotesRequest
{
    public Commander Commander { get; init; }
    public string? Unit { get; init; }
    public DateTime? PatchDate { get; init; }
    public DateTime? ToDate { get; init; }
    public PatchNotesSort Sort { get; init; } = PatchNotesSort.NewestFirst;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed record PatchNoteDto
{
    public long Id { get; init; }
    public DateTime PublishedAtUtc { get; init; }
    public Commander Commander { get; init; }
    public required string Content { get; init; }
}

public sealed record PatchNotesPage
{
    public IReadOnlyList<PatchNoteDto> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public bool HasMore { get; init; }
}
