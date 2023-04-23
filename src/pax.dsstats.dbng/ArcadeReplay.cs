using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.dbng;

public class ArcadeReplay
{
    public ArcadeReplay()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
    }

    [Key]
    public int ArcadeReplayId { get; set; }
    public int RegionId { get; set; }
    public long BnetBucketId { get; set; }
    public long BnetRecordId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public bool TournamentEdition { get; set; }
    public int WinnerTeam { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
    [StringLength(64)]
    public string ReplayHash { get; set; } = string.Empty;
    public ArcadeReplayRating? ArcadeReplayRating { get; set; }
    public ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}

public class ArcadeReplayPlayer
{
    [Key]
    public int ArcadeReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int ArcadePlayerId { get; set; }
    public ArcadeReplayPlayerRating? ArcadeReplayPlayerRating { get; set; }
    public ArcadePlayer ArcadePlayer { get; set; } = null!;
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay ArcadeReplay { get; set; } = null!;
}

public class ArcadePlayer
{
    public ArcadePlayer()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
        ArcadePlayerRatings = new HashSet<ArcadePlayerRating>();
    }

    [Key]
    public int ArcadePlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public ICollection<ArcadePlayerRating> ArcadePlayerRatings { get; set; }
    public ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}

public class ArcadePlayerRating
{
    public int ArcadePlayerRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public double Rating { get; set; }
    public int Pos { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public int MainCount { get; set; }
    public Commander Main { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public ArcadePlayerRatingChange? ArcadePlayerRatingChange { get; set; }
    public int ArcadePlayerId { get; set; }
    public virtual ArcadePlayer ArcadePlayer { get; set; } = null!;
}

public class ArcadePlayerRatingChange
{
    public int ArcadePlayerRatingChangeId { get; set; }
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
    public int ArcadePlayerRatingId { get; set; }
    public ArcadePlayerRating ArcadePlayerRating { get; set; } = null!;
}

public class ArcadeReplayRating
{
    public ArcadeReplayRating()
    {
        ArcadeReplayPlayerRatings = new HashSet<ArcadeReplayPlayerRating>();
    }
    public int ArcadeReplayRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    public float ExpectationToWin { get; set; } // WinnerTeam
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay ArcadeReplay { get; set; } = null!;
    public virtual ICollection<ArcadeReplayPlayerRating> ArcadeReplayPlayerRatings { get; set; }
}

public class ArcadeReplayPlayerRating
{
    public int ArcadeReplayPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ArcadeReplayPlayerId { get; set; }
    public ArcadeReplayPlayer ReplayPlayer { get; set; } = null!;
    public int ArcadeReplayRatingId { get; set; }
    public ArcadeReplayRating ArcadeReplayRating { get; set; } = null!;
}