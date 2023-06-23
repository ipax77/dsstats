
namespace pax.dsstats.shared;

public record DurationRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public bool WithBrawl { get; set; }
    public bool WithRating { get; set; }
}

public record DurationResponse
{
    public List<ChartData> ChartDatas { get; set; } = new();
}

public record ChartData
{
    public Commander Commander { get; set; }
    public List<double> Data { get; set; } = new();
    public List<double> NiceData { get; set; } = new();
    public List<int> Counts { get; set; } = new();
}

public record DRangeResult
{
    public int Race { get; set; }
    public int DRange { get; set;  }
    public int Count { get; set; }
    public double WinsOrRating { get; set; }
}

public record DurationResult
{
    public Commander Commander { get; set; }
    public ChartData ChartData { get; set; } = new();
    public List<DRangeResult> Results { get; set; } = new();
}

