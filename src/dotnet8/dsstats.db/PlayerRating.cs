using System;
using System.Collections.Generic;
using dsstats.shared;

namespace dsstats.db;

public partial class PlayerRating
{
    public int PlayerRatingId { get; set; }

    public RatingType RatingType { get; set; }

    public double Rating { get; set; }

    public int Games { get; set; }

    public int Wins { get; set; }

    public int Mvp { get; set; }

    public int TeamGames { get; set; }

    public int MainCount { get; set; }

    public int Main { get; set; }

    public double Consistency { get; set; }

    public double Confidence { get; set; }

    public bool IsUploader { get; set; }
    public int ArcadeDefeatsSinceLastUpload { get; set; }

    public int PlayerId { get; set; }
    public virtual Player? Player { get; set; }

    public int Pos { get; set; }

    public virtual PlayerRatingChange? PlayerRatingChange { get; set; }
}
