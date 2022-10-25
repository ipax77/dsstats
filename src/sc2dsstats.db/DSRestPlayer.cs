namespace sc2dsstats.db
{
    public class DSRestPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Json { get; set; }
        public DateTime LastRep { get; set; }
        public DateTime LastUpload { get; set; }
        public int Data { get; set; } = 0;
        public int Total { get; set; } = 0;
        public string Version { get; set; }
    }
}
