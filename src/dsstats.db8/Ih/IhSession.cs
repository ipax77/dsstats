using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8;

public class IhSession
{
    public IhSession()
    {
        IhSessionPlayers = new HashSet<IhSessionPlayer>();
    }
    public int IhSessionId { get; set; }
    public RatingType RatingType { get; set; }
    public Guid GroupId { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; }
    public int Players { get; set; }
    public int Games { get; set; }
    public bool Closed { get; set; }
    public GroupState? GroupState { get; set; }
    public GroupStateV2? GroupStateV2 { get; set; }
    public virtual ICollection<IhSessionPlayer> IhSessionPlayers { get; set; }
}

public class IhSessionPlayer
{
    public int IhSessionPlayerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Obs { get; set; }
    public int RatingStart { get; set; }
    public int RatingEnd { get; set; }
    public int Performance { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int IhSessionId { get; set; }
    public IhSession? IhSession { get; set; }
}

