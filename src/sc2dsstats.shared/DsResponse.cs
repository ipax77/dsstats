using System.Text.Json.Serialization;

namespace sc2dsstats.shared
{
    public class DsResponse
    {
        public int Count { get; set; }
        public string? Interest { get; set; }
        public int AvgDuration { get; set; }
        public double AvgWinrate { get; set; }
        public List<DsResponseItem> Items { get; set; } = new();
        public DsCountResponse? CountResponse { get; set; }
    }

    public class DsResponseItem
    {
        public string Label { get; set; } = "Abathur";
        public int Count { get; set; }
        public int Wins { get; set; }
        [JsonIgnore]
        public long duration { get; set; }
        public int Replays { get; set; }
        [JsonIgnore]
        public double Winrate => Count > 0 ? Math.Round((double)Wins * 100 / (double)Count, 2) : 0;
    }
}
