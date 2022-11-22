
namespace pax.dsstats.shared;

public record RatingsRequest
{
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int? ToonId { get; set; }
}

public record RatingsResult
{

}