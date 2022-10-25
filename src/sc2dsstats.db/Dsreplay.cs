using sc2dsstats.shared;
using System.Text.Json.Serialization;
using static sc2dsstats.shared.DSData;

namespace sc2dsstats.db
{
    public partial class Dsreplay
    {
        public int Id { get; set; }
        public string Replay { get; set; }
        public DateTime Gametime { get; set; }
        public sbyte Winner { get; set; }
        public int Duration { get; set; }
        public int Minkillsum { get; set; }
        public int Maxkillsum { get; set; }
        public int Minarmy { get; set; }
        public int Minincome { get; set; }
        public int Maxleaver { get; set; }
        public byte Playercount { get; set; }
        public byte Reported { get; set; }
        public bool Isbrawl { get; set; }
        public byte Gamemode { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }
        public string Replaypath { get; set; }
        public int Objective { get; set; }
        public int Bunker { get; set; }
        public int Cannon { get; set; }
        public DateTime Upload { get; set; }
        public bool DefaultFilter { get; set; }
        public decimal? Mid1 { get; set; }
        public decimal? Mid2 { get; set; }
        [JsonPropertyName("DSPlayer")]
        public virtual ICollection<Dsplayer> Dsplayers { get; set; }
        [JsonPropertyName("Middle")]
        public virtual ICollection<Middle> Middles { get; set; }

        public Dsreplay()
        {
            Dsplayers = new HashSet<Dsplayer>();
            Middles = new HashSet<Middle>();
        }

        public Dsreplay(DsReplayDto dto)
        {
            Dsplayers = dto.DSPlayer.Select(s => new Dsplayer()
            {
                Breakpoints = s.Breakpoints.Select(b => new Breakpoint()
                {
                    Breakpoint1 = b.Breakpoint,
                    Gas = b.Gas,
                    Income = b.Income,
                    Army = b.Army,
                    Kills = b.Kills,
                    Upgrades = b.Upgrades,
                    Tier = b.Tier,
                    Mid = b.Mid,
                    DsUnitsString = b.dsUnitsString,
                    DbUnitsString = b.dbUnitsString,
                    DbUpgradesString = b.dbUpgradesString
                }).ToList(),
                Pos = (byte)s.POS,
                Realpos = (byte)s.REALPOS,
                Name = s.NAME,
                Race = (byte)DSData.GetCommander(s.RACE),
                Opprace = (byte)DSData.GetCommander(s.OPPRACE),
                Win = s.WIN,
                Team = (byte)s.TEAM,
                Killsum = s.KILLSUM,
                Income = s.INCOME,
                Pduration = s.PDURATION,
                Army = s.ARMY,
                Gas = (byte)s.GAS,
                isPlayer = s.isPlayer
            }).ToList();
            Middles = dto.Middle.Select(m => new Middle()
            {
                Gameloop = m.Gameloop,
                Team = (byte)m.Team
            }).ToList();
            Replay = dto.REPLAY;
            Gametime = dto.GAMETIME;
            Winner = (sbyte)dto.WINNER;
            Duration = dto.DURATION;
            Minkillsum = dto.MINKILLSUM;
            Maxkillsum = dto.MAXKILLSUM;
            Minarmy = dto.MINARMY;
            Minincome = dto.MININCOME;
            Maxleaver = dto.MAXLEAVER;
            Playercount = (byte)dto.PLAYERCOUNT;
            Reported = (byte)dto.REPORTED;
            Isbrawl = dto.ISBRAWL;
            Gamemode = (byte)DSData.GetGameMode(dto.GAMEMODE);
            Version = dto.VERSION;
            Hash = dto.HASH;
            Replaypath = dto.REPLAYPATH;
            Objective = dto.OBJECTIVE;
            Upload = dto.Upload;
            Bunker = dto.Bunker;
            Cannon = dto.Cannon;
            Mid1 = dto.Mid1;
            Mid2 = dto.Mid2;
        }

        public DsReplayDto GetDto()
        {
            return new DsReplayDto()
            {
                DSPlayer = Dsplayers.Select(s => new DSPlayerDto()
                {
                    Breakpoints = s.Breakpoints.Select(b => new BreakpointDto()
                    {
                        Breakpoint = b.Breakpoint1,
                        Gas = b.Gas,
                        Income = b.Income,
                        Army = b.Army,
                        Kills = b.Kills,
                        Upgrades = b.Upgrades,
                        Tier = b.Tier,
                        Mid = b.Mid,
                        dsUnitsString = b.DsUnitsString,
                        dbUnitsString = b.DbUnitsString,
                        dbUpgradesString = b.DbUpgradesString
                    }).ToList(),
                    POS = s.Pos,
                    REALPOS = s.Realpos,
                    NAME = s.Name,
                    RACE = ((Commander)s.Race).ToString(),
                    OPPRACE = ((Commander)s.Opprace).ToString(),
                    WIN = s.Win,
                    TEAM = s.Team,
                    KILLSUM = s.Killsum,
                    INCOME = s.Income,
                    PDURATION = s.Pduration,
                    ARMY = s.Army,
                    GAS = s.Gas,
                    isPlayer = s.isPlayer
                }).ToList(),
                Middle = Middles.Select(m => new MiddleDto()
                {
                    Gameloop = m.Gameloop,
                    Team = m.Team
                }).ToList(),
                REPLAY = Replay,
                GAMETIME = Gametime,
                WINNER = Winner,
                DURATION = Duration,
                MINKILLSUM = Minkillsum,
                MAXKILLSUM = Maxkillsum,
                MINARMY = Minarmy,
                MININCOME = Minincome,
                MAXLEAVER = Maxleaver,
                PLAYERCOUNT = Playercount,
                REPORTED = Reported,
                ISBRAWL = Isbrawl,
                GAMEMODE = ((Gamemode)Gamemode).ToString(),
                VERSION = Version,
                HASH = Hash,
                REPLAYPATH = Replaypath,
                OBJECTIVE = Objective,
                Upload = Upload,
                Bunker = Bunker,
                Cannon = Cannon,
                Mid1 = Mid1,
                Mid2 = Mid2
            };
        }
    }
}
