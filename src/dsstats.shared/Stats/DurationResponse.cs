namespace dsstats.shared;

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
