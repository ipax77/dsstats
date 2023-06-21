
namespace pax.dsstats.shared;

public record DurationRequest
{
    public TimePeriod TimePeriod { get; set; }
    public Commander Commander { get; set; }

}

public record DurationResponse
{
    public Commander Commander { get; set; }
    public ChartData ChartData { get; set; } = new();
    public List<DRangeResult> Results { get; set; } = new();
}

public record ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<double> Data { get; set; } = new();
}

public record DRangeResult
{
    public int DRange { get; set;  }
    public int Count { get; set; }
    public int Wins { get; set; }
}