namespace pax.dsstats.shared;

public record CrossTableRequest
{
    public string Mode { get; set; } = "Standard";
    public string TimePeriod { get; set; } = "This Year";
}