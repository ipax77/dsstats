
namespace pax.dsstats.shared;

public record UpdateResult
{
    public int Total { get; set; }
    public int Update { get; set; }
    public int New { get; set; }
}