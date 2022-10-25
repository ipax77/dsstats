using System.ComponentModel.DataAnnotations;

namespace sc2dsstats.db
{
    public class DsTimeResult
    {
        public int Id { get; set; }
        public bool Player { get; set; }
        [MaxLength(100)]
        public string Timespan { get; set; }
        [MaxLength(100)]
        public string Cmdr { get; set; }
        [MaxLength(100)]
        public string Opp { get; set; }
        public int Count { get; set; }
        public int Wins { get; set; }
        public int MVP { get; set; }
        public decimal Duration { get; set; }
        public decimal Kills { get; set; }
        public decimal Army { get; set; }
        public virtual ICollection<DsParticipant> Teammates { get; set; }
        public virtual ICollection<DsParticipant> Opponents { get; set; }
    }

    public class DsParticipant
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string Cmdr { get; set; }
        public int Count { get; set; }
        public int Wins { get; set; }
        public int? DsTimeResultId { get; set; }
        public int? DsTimeResultId1 { get; set; }

        public virtual DsTimeResult DsTimeResult { get; set; }
        public virtual DsTimeResult DsTimeResultId1Navigation { get; set; }
    }

    public class DsTimeResultValue
    {
        public int Id { get; set; }
        public bool Player { get; set; }
        [MaxLength(100)]
        public DateTime Gametime { get; set; }
        [MaxLength(100)]
        public string Cmdr { get; set; }
        [MaxLength(100)]
        public string Opp { get; set; }
        public bool Win { get; set; }
        public bool MVP { get; set; }
        public int Duration { get; set; }
        public int Kills { get; set; }
        public int Army { get; set; }
        public virtual ICollection<DsParticipantsValue> Teammates { get; set; }
        public virtual ICollection<DsParticipantsValue> Opponents { get; set; }
    }

    public class DsParticipantsValue
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string Cmdr { get; set; }
        public bool Win { get; set; }
        public int? DsTimeResultValuesId { get; set; }
        public int? DsTimeResultValuesId1 { get; set; }

        public virtual DsTimeResultValue DsTimeResultValues { get; set; }
        public virtual DsTimeResultValue DsTimeResultValuesId1Navigation { get; set; }
    }
}
