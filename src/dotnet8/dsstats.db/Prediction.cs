using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Prediction
{
    public int PredictionId { get; set; }

    public string Symbol { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public virtual ICollection<PredictionClose> PredictionCloses { get; set; } = new List<PredictionClose>();
}
