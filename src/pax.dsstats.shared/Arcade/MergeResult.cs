namespace pax.dsstats.shared.Arcade;

public record MergeResult
{
    public int DsCount { get; set; }
    public int ArCount { get; set; }
    public List<int> DsOnly { get; set; } = new();
    public List<int> ArOnly { get; set; } = new();
    public List<KeyValuePair<int, int>> DsAndAr { get; set; } = new();
}

public record MergeResultReplays
{
    public List<ReplayListDto> DsOnly { get; set; } = new();
    public List<ArcadeReplayListDto> ArOnly { get; set; } = new();
    public List<KeyValuePair<ReplayListDto, ArcadeReplayListDto>> DsAndAr { get; set; } = new();
}