using System;
using System.Collections.Generic;
using dsstats.shared;

namespace dsstats.db;

public partial class ArcadeReplayRating
{
    public ArcadeReplayRating()
    {
        ArcadeReplayPlayerRatings = new HashSet<ArcadeReplayPlayerRating>();
    }
    public int ArcadeReplayRatingId { get; set; }

    public RatingType RatingType { get; set; }

    public LeaverType LeaverType { get; set; }

    public float ExpectationToWin { get; set; }

    public int ArcadeReplayId { get; set; }
    public virtual ArcadeReplay? ArcadeReplay { get; set; }
    public virtual ICollection<ArcadeReplayPlayerRating> ArcadeReplayPlayerRatings { get; set; }
}
