namespace dsstats.shared;

public record UnitRequest
{
    public string Search { get; set; } = string.Empty;
    public string? UnitName {  get; set; }
    public Commander Commander { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = [];
}

public record UnitDetailRequest
{
    public string Name { get; set; } = string.Empty;
    public Commander Commander { get; set; }
}
