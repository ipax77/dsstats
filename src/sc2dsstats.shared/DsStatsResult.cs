using System;

namespace sc2dsstats.shared
{
    public class DbStatsResult
    {
        public int DbStatsResultId { get; set; }
        public int Id { get; set; }
        public byte Race { get; set; }
        public byte OppRace { get; set; }
        public int Duration { get; set; }
        public bool Win { get; set; }
        public bool Player { get; set; }
        public int Army { get; set; }
        public int Kills { get; set; }
        public bool MVP { get; set; }
        public DateTime GameTime { get; set; }
    }
}
