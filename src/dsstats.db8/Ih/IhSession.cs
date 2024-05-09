using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8;

public class IhSession
{
    public int IhSessionId { get; set; }
    public RatingType RatingType { get; set; }
    public Guid GroupId { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; }
    public int Players { get; set; }
    public int Games { get; set; }
    public bool Closed { get; set; }
    public GroupState? GroupState { get; set; }
}




