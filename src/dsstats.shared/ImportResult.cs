namespace dsstats.shared;

public record ImportResult
{
    public int Imported { get; init; }
    public int Duplicates { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, string> DetailErrors = [];
}
