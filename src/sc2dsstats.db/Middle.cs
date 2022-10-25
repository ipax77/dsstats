using System.Text.Json.Serialization;

namespace sc2dsstats.db
{
    public partial class Middle
    {
        public int Id { get; set; }
        public int Gameloop { get; set; }
        public byte Team { get; set; }
        public int? ReplayId { get; set; }
        [JsonIgnore]
        public virtual Dsreplay Replay { get; set; }
    }
}
