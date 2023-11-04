using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class TimelineQueryData
{
    public int Race { get; set; }

    public int Ryear { get; set; }

    public int Rmonth { get; set; }

    public int Count { get; set; }

    public int Wins { get; set; }

    public double AvgGain { get; set; }

    public double AvgRating { get; set; }

    public double AvgOppRating { get; set; }

    public double AvgTeamRating { get; set; }

    public double AvgOppTeamRating { get; set; }
}
