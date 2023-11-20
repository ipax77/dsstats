using pax.BlazorChartJs;

namespace dsstats.razorlib.Models;

public class IconsChartJsConfig : ChartJsConfig
{
    public new IconsChartJsOptions? Options { get; set; }
}

public record IconsChartJsOptions : ChartJsOptions
{
    public new IconsPlugins? Plugins { get; set; }
}

public record IconsPlugins : Plugins
{
    public ICollection<ChartIconsConfig>? BarIcons { get; set; }
}

public record ChartIconsConfig
{
    public int XWidth { get; set; }
    public int YWidth { get; set; }
    public int YOffset { get; set; }
    public string ImageSrc { get; set; } = null!;
    public string? Cmdr { get; set; }
}