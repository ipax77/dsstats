namespace dsstats.shared;

public record TableOrder
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}