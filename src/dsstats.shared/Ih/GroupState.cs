
namespace dsstats.shared;

public record GroupState
{
    public Guid GroupId { get; set; }
    public int Visitors { get; set; }
    public HashSet<string> ReplayHashes { get; set; } = [];
    public List<PlayerState> PlayerStates { get; set; } = [];
}

public record PlayerState
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public List<PlayerId> PlayedWith { get; set; } = [];
    public List<PlayerId> PlayedAgainst { get; set; } = [];
    public int Games { get; set; }
    public int Observer { get; set; }
    public bool PlayedLastGame { get; set; }
    public bool ObsLastGame { get; set; }
}