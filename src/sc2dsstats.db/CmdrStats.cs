namespace sc2dsstats.db
{
    [Serializable]
    public class CmdrStats
    {
        public int year { get; set; }
        public int month { get; set; }
        public byte RACE { get; set; }
        public byte OPPRACE { get; set; }
        public int count { get; set; }
        public int wins { get; set; }
        public int mvp { get; set; }
        public decimal army { get; set; }
        public decimal kills { get; set; }
        public decimal duration { get; set; }
        public int replays { get; set; }
    }
}
