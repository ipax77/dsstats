using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Stockprice
{
    public int StockpriceId { get; set; }

    public string Symbol { get; set; } = null!;

    public DateTime Date { get; set; }

    public double Open { get; set; }

    public double High { get; set; }

    public double Low { get; set; }

    public double Close { get; set; }

    public double AdjClose { get; set; }

    public double Volume { get; set; }

    public double? NextPredictedClose { get; set; }
}
