namespace pax.dsstats.shared;

public record ChartIconsConfig
{
    public int XWidth { get; set; }
    public int YWidth { get; set; }
    public int YOffset { get; set; }
    public string ImageSrc { get; set; } = null!;
    public string? Cmdr { get; set; }
}
