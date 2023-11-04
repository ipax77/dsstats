using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class DrangeResult
{
    public int Race { get; set; }

    public int Drange { get; set; }

    public int Count { get; set; }

    public double WinsOrRating { get; set; }
}
