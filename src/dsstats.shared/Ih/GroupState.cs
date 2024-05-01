
namespace dsstats.shared;

public record GroupState
{
    public Guid GroupId { get; set; }
    public int Visitors { get; set; }
    public HashSet<string> ReplayHashes { get; set; } = [];
}
