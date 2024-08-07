﻿using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db8;

public class MaterializedArcadeReplay
{
    [Key]
    public int MaterializedArcadeReplayId { get; set; }
    public int ArcadeReplayId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
}

public class ArcadeReplay
{
    public ArcadeReplay()
    {
        ArcadeReplayDsPlayers = new HashSet<ArcadeReplayDsPlayer>();
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
    public ICollection<ArcadeReplayDsPlayer> ArcadeReplayDsPlayers { get; set; }
}

public class ArcadeReplayDsPlayer
{
    [Key]
    public int ArcadeReplayDsPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public ArcadeReplayDsPlayerRating? ArcadeReplayPlayerRating { get; set; }
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay? ArcadeReplay { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
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
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    //public int? PlayerId { get; set; }
    //public Player? Player { get; set; }
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
        ArcadeReplayDsPlayerRatings = new HashSet<ArcadeReplayDsPlayerRating>();
    }
    public int ArcadeReplayRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    public float ExpectationToWin { get; set; } // WinnerTeam
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay ArcadeReplay { get; set; } = null!;
    public int AvgRating { get; set; }
    public virtual ICollection<ArcadeReplayDsPlayerRating> ArcadeReplayDsPlayerRatings { get; set; }
}

public class ArcadeReplayDsPlayerRating
{
    public int ArcadeReplayDsPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ArcadeReplayDsPlayerId { get; set; }
    public ArcadeReplayDsPlayer ReplayDsPlayer { get; set; } = null!;
    public int ArcadeReplayRatingId { get; set; }
    public ArcadeReplayRating ArcadeReplayRating { get; set; } = null!;
}