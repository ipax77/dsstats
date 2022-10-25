using System.Text.Json.Serialization;

namespace sc2dsstats.db
{
    public partial class Breakpoint
    {
        public Breakpoint()
        {
            Dsunits = new HashSet<Dsunit>();
        }

        public int Id { get; set; }
        [JsonPropertyName("Breakpoint")]
        public string Breakpoint1 { get; set; }
        public int Gas { get; set; }
        public int Income { get; set; }
        public int Army { get; set; }
        public int Kills { get; set; }
        public int Upgrades { get; set; }
        public int Tier { get; set; }
        public int? PlayerId { get; set; }
        public int Mid { get; set; }
        public string DsUnitsString { get; set; }
        public string DbUnitsString { get; set; }
        public string DbUpgradesString { get; set; }
        [JsonIgnore]
        public virtual Dsplayer Player { get; set; }
        public virtual ICollection<Dsunit> Dsunits { get; set; }
    }
}
