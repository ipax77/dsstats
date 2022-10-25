namespace sc2dsstats.db
{
    public class DsPlayerName
    {
        public int Id { get; set; }
        public Guid AppId { get; set; }
        public Guid DbId { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public DateTime LatestReplay { get; set; }
        public DateTime LatestUpload { get; set; }
        public int TotlaReplays { get; set; }
        public string AppVersion { get; set; }
        public bool NamesMapped { get; set; } = false;
        public ICollection<Dsplayer> Dsplayers { get; set; }

        public DsPlayerName()
        {
            Dsplayers = new HashSet<Dsplayer>();
        }
    }
}
