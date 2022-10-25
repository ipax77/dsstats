using System;
using System.Collections.Generic;

namespace sc2dsstats.shared
{
    public class BreakpointDto
    {
        public string Breakpoint { get; set; } = "MIN5";
        public int Gas { get; set; }
        public int Income { get; set; }
        public int Army { get; set; }
        public int Kills { get; set; }
        public int Upgrades { get; set; }
        public int Tier { get; set; }
        public int Mid { get; set; }
        public string dsUnitsString { get; set; } = "";
        public string dbUnitsString { get; set; } = "";
        public string dbUpgradesString { get; set; } = "";
    }

    public class DSPlayerDto
    {
        public List<BreakpointDto> Breakpoints { get; set; } = new();
        public int POS { get; set; }
        public int REALPOS { get; set; }
        public string NAME { get; set; } = "";
        public string RACE { get; set; } = "Abathur";
        public string OPPRACE { get; set; } = "Abathur";
        public bool WIN { get; set; }
        public int TEAM { get; set; }
        public int KILLSUM { get; set; }
        public int INCOME { get; set; }
        public int PDURATION { get; set; }
        public int ARMY { get; set; }
        public int GAS { get; set; }
        public bool isPlayer { get; set; }
    }

    public class MiddleDto
    {
        public int Gameloop { get; set; }
        public int Team { get; set; }
    }

    public class DsReplayDto
    {
        public List<DSPlayerDto> DSPlayer { get; set; } = new();
        public List<MiddleDto> Middle { get; set; } = new();
        public string REPLAY { get; set; } = "";
        public DateTime GAMETIME { get; set; }
        public int WINNER { get; set; }
        public int DURATION { get; set; }
        public int MINKILLSUM { get; set; }
        public int MAXKILLSUM { get; set; }
        public int MINARMY { get; set; }
        public int MININCOME { get; set; }
        public int MAXLEAVER { get; set; }
        public int PLAYERCOUNT { get; set; }
        public int REPORTED { get; set; }
        public bool ISBRAWL { get; set; }
        public string GAMEMODE { get; set; } = "";
        public string VERSION { get; set; } = "";
        public string HASH { get; set; } = "";
        public string REPLAYPATH { get; set; } = "";
        public int OBJECTIVE { get; set; }
        public DateTime Upload { get; set; }
        public int Bunker { get; set; }
        public int Cannon { get; set; }
        public decimal? Mid1 { get; set; }
        public decimal? Mid2 { get; set; }
    }
}
