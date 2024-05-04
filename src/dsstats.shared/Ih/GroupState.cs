
namespace dsstats.shared;

public record GroupState
{
    public RatingType RatingType { get; set; } = RatingType.StdTE;
    public RatingCalcType RatingCalcType { get; set; } = RatingCalcType.Dsstats;
    public Guid GroupId { get; set; }
    public int Visitors { get; set; }
    public HashSet<string> ReplayHashes { get; set; } = [];
    public List<PlayerState> PlayerStates { get; set; } = [];
    public IhMatch IhMatch { get; set; } = new();
}

public record PlayerState
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public List<PlayerId> PlayedWith { get; set; } = [];
    public List<PlayerId> PlayedAgainst { get; set; } = [];
    public int Games { get; set; }
    public int Observer { get; set; }
    public bool InQueue {  get; set; }
    public bool Joined { get; set; }
    public int RatingStart { get; set; }
    public int CurrentRating { get; set; }
}