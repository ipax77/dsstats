namespace sc2dsstats.shared
{
    public class DsReplayResponse
    {
        public int Id { get; set; }
        public string Hash { get; set; } = "";
        public List<string> Races { get; set; } = new();
        public List<string> Players { get; set; } = new();
        public DateTime Gametime { get; set; }
        public int Duration { get; set; }
        public int PlayerCount { get; set; }
        public string GameMode { get; set; } = "";
        public int MaxLeaver { get; set; }
        public int MaxKillsum { get; set; }
        public int Winner { get; set; }
        public bool DefaultFilter { get; set; }
        public decimal Mid1 { get; set; }
        public decimal Mid2 { get; set; }
    }
}
