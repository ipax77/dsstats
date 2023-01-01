namespace pax.dsstats.shared;

public record CrossTableRequest
{
    public string Mode { get; set; } = "Standard";
    public string TimePeriod { get; set; } = "This Year";
    public bool TeMaps { get; set; }
}

public record CrossTableReplaysRequest
{
    public string Mode { get; set; } = "Standard";
    public string TimePeriod { get; set; } = "This Year";
    public bool TeMaps { get; set; }
    public TeamCmdrs TeamCmdrs { get; set; } = null!;
    public TeamCmdrs? TeamCmdrsVs { get; set; }
}