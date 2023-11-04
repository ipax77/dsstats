using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class PredictionClose
{
    public int PredictionCloseId { get; set; }

    public DateTime Date { get; set; }

    public double Close { get; set; }

    public int PredictionId { get; set; }

    public virtual Prediction Prediction { get; set; } = null!;
}
