using System.Numerics;
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;
public record DsReplay
{
    public string FileName { get; set; } = "";
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public string GameMode { get; set; } = "";
    public List<string> Mutations { get; set; } = new List<string>();
    public Position Center { get; set; } = Position.Zero;
    public int Bunker { get; set; }
    public int Cannon { get; set; }
    public bool TournamentEdition { get; set; }
    public float[] LineTeam1 { get; set; } = new float[4] { 0, 0, 0, 0 };
    public float[] LineTeam2 { get; set; } = new float[4] { 0, 0, 0, 0 };
    public List<DsPlayer> Players { get; set; } = new List<DsPlayer>();
    public List<DsMiddle> Middles { get; set; } = new List<DsMiddle>();
    public ReplayLayout Layout { get; set; } = new ReplayLayout();
}

public record ReplayLayout
{
    public Position Nexus { get; set; } = Position.Zero;
    public Position Planetary { get; set; } = Position.Zero;
    public Position Cannon { get; set; } = Position.Zero;
    public Position Bunker { get; set; } = Position.Zero;
}

public record DsMiddle
{
    public int Gameloop { get; set; }
    public int Team { get; set; }
}

public record DsPlayer
{
    public string Name { get; set; } = "";
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public string? Clan { get; set; }
    public int Pos { get; set; }
    public int GamePos { get; set; }
    public int WorkingsetSlot { get; set; }
    public int Control { get; set; }
    public int Team { get; set; }
    public int Duration { get; set; }
    public string Race { get; set; } = "unknown";
    public string SelectedRace { get; set; } = "unknown";
    public bool RaceInGameSelected { get; set; } = false;
    public bool HasSpawns { get; set; } = false;
    public double APM { get; set; }
    public int Army { get; set; }
    public int Income { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public List<int> TierUpgrades { get; set; } = new List<int>();
    public List<DsUnit> Units { get; set; } = new List<DsUnit>();
    public List<DsRefinery> Refineries { get; set; } = new List<DsRefinery>();
    public List<DsStats> Stats { get; set; } = new List<DsStats>();
    public List<DsSpawns> Spawns { get; set; } = new List<DsSpawns>();
    public List<DsUpgrade> Upgrades { get; set; } = new List<DsUpgrade>();
    public int[] SpawnArea { get; set; } = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
    public SpawnArea SpawnArea2 { get; set; } = new SpawnArea();
    public List<PlayerSpawnStats> SpawnStats { get; set; } = new List<PlayerSpawnStats>();
}

public record PlayerSpawnStats
{
    public int Gameloop { get; set; }
    public List<DsUnit> Units { get; set; } = new List<DsUnit>();
    public List<DsUnit> Surwivers { get; set; } = new List<DsUnit>();
    public List<DsUpgrade> Upgrades { get; set; } = new List<DsUpgrade>();
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradesSpent { get; set; }
}
public record SpawnArea
{
    public Position South { get; set; } = Position.Zero;
    public Position West { get; set; } = Position.Zero;
    public Position North { get; set; } = Position.Zero;
    public Position East { get; set; } = Position.Zero;

}

public record DsUpgrade
{
    public int Gameloop { get; set; }
    public string Upgrade { get; set; } = "";
    public int Count { get; set; }
}

public record DsSpawns
{
    public int Gameloop { get; set; }
}

public record DsStats
{
    public int ID { get; set; }
    public int Gameloop { get; set; }
    public int FoodUsed { get; set; } = 0;
    public int MineralsCollectionRate { get; set; } = 0;
    public int MineralsCurrent { get; set; } = 0;
    public int MineralsFriendlyFireArmy { get; set; } = 0;
    public int MineralsFriendlyFireTechnology { get; set; } = 0;
    public int MineralsKilledArmy { get; set; } = 0;
    public int MineralsKilledTechnology { get; set; } = 0;
    public int MineralsLostArmy { get; set; } = 0;
    public int MineralsUsedActiveForces { get; set; } = 0;
    public int MineralsUsedCurrentArmy { get; set; } = 0;
    public int MineralsUsedCurrentTechnology { get; set; } = 0;
    public int Army { get; set; } = 0;
}

public record DsRefinery
{
    public int Gameloop { get; set; }
}

public enum UnitType
{
    None = 0,
    Build = 1,
    Spawn = 2,
    Tier = 3
}

public record DsUnit
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public int Gameloop { get; set; }
    public Position Position { get; set; } = Position.Zero;
    public int Spawn { get; set; }
    public bool BuildArea { get; set; }
    public int KillerPlayer { get; set; }
    public string? KillerUnit { get; set; }
    public int DiedGameloop { get; set; }
    public Position DiedPosition { get; set; } = Position.Zero;
    public DsUnitData? DsUnitData { get; set; }
    public UnitType UnitType { get; set; }
}

public record DsUnitData
{
    public DsUnitData() { }
    public DsUnitData(DsUnitDataCsvDummy d)
    {
        Name = d.Name.Replace(" ", "");
        Race = d.Race;
        if (int.TryParse(d.Tier, out int tier))
        {
            Tier = tier;
        }
        if (int.TryParse(d.Cost, out int cost))
        {
            Cost = cost;
        }
        if (int.TryParse(d.Life, out int life))
        {
            Life = life;
        }
        if (int.TryParse(d.Shields, out int shields))
        {
            Shields = shields;
        }
        if (int.TryParse(d.TotalVitality, out int totalVitality))
        {
            TotalVitality = totalVitality;
        }
    }

    public string Name { get; set; } = "";
    public string Race { get; set; } = "";
    public int Tier { get; set; }
    public int Cost { get; set; }
    public int Life { get; set; }
    public int Shields { get; set; }
    public int TotalVitality { get; set; }
}

public record Position
{
    public Position()
    {

    }

    public Position(int x, int y) : base()
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public static readonly Position Zero = new() { X = 0, Y = 0 };
    public static readonly Position Center1 = new() { X = 128, Y = 120 };
    public static readonly Position Center2 = new() { X = 120, Y = 120 };
    public static readonly Position Center3 = new() { X = 128, Y = 122 };

    [JsonIgnore]
    public Vector2 Vector2 => new Vector2(X, Y);
}

public record DsUnitDataCsvDummy
{
    public string Name { get; set; } = "";
    public string Race { get; set; } = "";
    public string? Tier { get; set; }
    public string? Cost { get; set; }
    public string? Life { get; set; }
    public string? Shields { get; set; }
    public string? TotalVitality { get; set; }
}