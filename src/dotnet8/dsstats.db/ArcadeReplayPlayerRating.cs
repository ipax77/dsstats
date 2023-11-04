using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class ArcadeReplayPlayerRating
{
    public int ArcadeReplayPlayerRatingId { get; set; }

    public int GamePos { get; set; }

    public float Rating { get; set; }

    public float RatingChange { get; set; }

    public int Games { get; set; }

    public float Consistency { get; set; }

    public float Confidence { get; set; }

    public int ArcadeReplayPlayerId { get; set; }
    public virtual ArcadeReplayPlayer? ReplayPlayer { get; set; }
    public int ArcadeReplayRatingId { get; set; }
    public virtual ArcadeReplayRating? ArcadeReplayRating { get; set; }
}
