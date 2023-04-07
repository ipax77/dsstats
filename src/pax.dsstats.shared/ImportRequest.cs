namespace pax.dsstats.shared;

public record ImportRequest
{
    public List<string> Replayblobs { get; set; } = new();
}

public record ImportResult
{
    public int UnitsCount { get; init; }
    public int UpgradesCount { get; init; }
    public int PlayersCount { get; init; }
    public int UploadersCount { get; init; }
    public int ReplaysCount { get; init; }
    public int LastSpawnsCount { get; init; }
    public int BlobCount { get; init; }
    public int LatestImports { get; init; }
    public int LatestDuplicates { get; init; }
    public int LatestErrors { get; init; }
    public int LatestDuration { get; init; }
}

public record RatingsReport
{
    public DateTime Produced { get; init; }
    public int ElapsedMs { get; init; }
    public bool Recalc { get; init; }
    public int RecalcCount { get; init; }
}