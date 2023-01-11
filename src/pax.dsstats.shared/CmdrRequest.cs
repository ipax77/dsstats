namespace pax.dsstats.shared;

public record CmdrRequest
{
    public Commander Cmdr { get; set; }
    public TimePeriod TimeSpan { get; set; } = TimePeriod.Past90Days;
    public bool Uploaders { get; set; }
}
