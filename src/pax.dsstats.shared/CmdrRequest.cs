namespace pax.dsstats.shared;

public record CmdrRequest
{
    public Commander Cmdr { get; set; }
    public string TimeSpan { get; set; } = "This Year";
    public bool Uploaders { get; set; }
}
