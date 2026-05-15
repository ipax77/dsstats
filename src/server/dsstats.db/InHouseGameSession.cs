using System.ComponentModel.DataAnnotations;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public sealed class ReplayObservers
{
    public int ReplayObserversId { get; set; }
    public int[] PlayerIds { get; set; } = [];
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
}


public sealed class InHouseGameSession
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
    public ICollection<InHouseGameSessionReplay> Replays { get; set; } = [];
    public ICollection<InHouseGameSessionRosterPlayer> RosterPlayers { get; set; } = [];
    public ICollection<InHouseGameSessionPlayerSummary> PlayerSummaries { get; set; } = [];
}

public sealed class InHouseGameSessionReplay
{
    public int InHouseGameSessionReplayId { get; set; }
    public int InHouseGameSessionId { get; set; }
    public InHouseGameSession? Session { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public int UploadedByInHouseUserId { get; set; }
    public InHouseUser? UploadedBy { get; set; }
    [Precision(0)]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public ICollection<InHouseGameSessionReplayPlayer> Players { get; set; } = [];
}

public sealed class InHouseGameSessionReplayPlayer
{
    public int InHouseGameSessionReplayPlayerId { get; set; }
    public int InHouseGameSessionReplayId { get; set; }
    public InHouseGameSessionReplay? SessionReplay { get; set; }
    public int? ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
    public int? PlayerId { get; set; }
    public Player? Player { get; set; }
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = new();
    public bool Observer { get; set; }
    public int TeamId { get; set; }
    public int GamePos { get; set; }
    public PlayerResult Result { get; set; }
}

public sealed class InHouseGameSessionPlayerSummary
{
    public int InHouseGameSessionPlayerSummaryId { get; set; }
    public int InHouseGameSessionId { get; set; }
    public InHouseGameSession? Session { get; set; }
    public int? PlayerId { get; set; }
    public Player? Player { get; set; }
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Observes { get; set; }
    [Precision(7, 2)]
    public double? RatingStart { get; set; }
    [Precision(7, 2)]
    public double? RatingEnd { get; set; }
    [Precision(7, 2)]
    public double? RatingDelta { get; set; }
    [Precision(7, 2)]
    public double? AverageGain { get; set; }
    public bool PlayedLatestGame { get; set; }
    public bool ObservedLatestGame { get; set; }
    public bool RatingsPending { get; set; }
}

public sealed class InHouseGameSessionRosterPlayer
{
    public int InHouseGameSessionRosterPlayerId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int InHouseGameSessionId { get; set; }
    public InHouseGameSession? Session { get; set; }
    public int? PlayerId { get; set; }
    public Player? Player { get; set; }
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = new();
    [Precision(7, 2)]
    public double InitialRating { get; set; } = 1000;
    public int JoinedReplayCount { get; set; }
    public bool IsSitter { get; set; }
    public bool IsManual { get; set; }
    [MaxLength(24)]
    public string AddSource { get; set; } = "replay";
    public int? AddedByInHouseUserId { get; set; }
    public InHouseUser? AddedBy { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(0)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
