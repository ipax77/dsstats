using System.Text.Json.Serialization;

namespace sc2dsstats.db
{
    public partial class Dsplayer
    {
        public Dsplayer()
        {
            Breakpoints = new HashSet<Breakpoint>();
            Dsunits = new HashSet<Dsunit>();
        }

        public int Id { get; set; }
        public byte Pos { get; set; }
        public byte Realpos { get; set; }
        public string Name { get; set; }
        public byte Race { get; set; }
        public byte Opprace { get; set; }
        public bool Win { get; set; }
        public byte Team { get; set; }
        public int Killsum { get; set; }
        public int Income { get; set; }
        public int Pduration { get; set; }
        public int Army { get; set; }
        public byte Gas { get; set; }
        public int? DsreplayId { get; set; }
        public bool isPlayer { get; set; }
        public DsPlayerName PlayerName { get; set; }
        [JsonIgnore]
        public virtual Dsreplay Dsreplay { get; set; }
        public virtual ICollection<Breakpoint> Breakpoints { get; set; }
        public virtual ICollection<Dsunit> Dsunits { get; set; }
    }
}
