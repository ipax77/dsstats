using System.ComponentModel.DataAnnotations;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public sealed class ReplayObservers
{
    public int ReplayObserversId { get; set; }
    public int[]? PlayerIds { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
}

public sealed class InHouseGameSessionSimplified
{
    public int InHouseGameSessionId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    public int CreatedByInHouseUserId { get; set; }
    public InHouseUser? CreatedBy { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(0)]
    public DateTime? ClosedAt { get; set; }
    public int[] ReplayIds { get; set; } = [];
    public InHouseGameSessionStateSnapshot? StateSnapshot { get; set; }
}

public sealed class InHouseGameSessionStateSnapshot
{
    public int InHouseGameSessionId { get; set; }

    public InHouseGameSessionSimplified? Session { get; set; }

    public string Json { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(0)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
