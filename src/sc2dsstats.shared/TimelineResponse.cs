using System.Collections.Generic;

namespace sc2dsstats.shared
{
    public class TimelineResponse : DsResponse
    {
        public string? Versus { get; set; }
        public List<double> SmaData { get; set; } = new();
    }

    public class TimelineResponseItem : DsResponseItem
    {
    }

}
