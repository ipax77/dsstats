using System.Text.Json.Serialization;

namespace sc2dsstats.db
{
    public partial class Dsunit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bp { get; set; }
        public int Count { get; set; }
        public int? BreakpointId { get; set; }
        public int? DsplayerId { get; set; }
        [JsonIgnore]
        public virtual Breakpoint Breakpoint { get; set; }
        [JsonIgnore]
        public virtual Dsplayer Dsplayer { get; set; }
    }
}
