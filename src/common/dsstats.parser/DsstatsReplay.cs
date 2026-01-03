using dsstats.shared;

namespace dsstats.parser;

internal class DsstatsReplay
{
    public string Title { get; set; } = string.Empty;
    public Version Version { get; set; } = new();
    public DateTime Gametime { get; set; }
    public int BaseBuild { get; set; }
    public int Duration { get; set; }
    public int Cannon { get; set; }
    public int Bunker { get; set; }
    public int WinnerTeam { get; set; }
    public Dictionary<Breakpoint, MiddleIncome> MiddleIncome { get; set; } = [];
    public List<DsPlayer> Players { get; set; } = [];
    public List<DsMiddle> MiddleChanges { get; set; } = [];
    public HashSet<string> Modes { get; set; } = [];
}



internal class DsPlayer
{
    public int PlayerId { get; set; }
    public int MetadataPlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Clan { get; set; }
    public ToonId ToonId { get; set; } = null!;
    public Commander Race { get; set; }
    public Commander SelectedRace { get; set; }
    public int RaceInGameSelected { get; set; }
    public int Control { get; set; }
    public int TeamId { get; set; }
    public int GamePos { get; set; }
    public int Observe { get; set; }
    public int Duration { get; set; }
    public PlayerResult Result { get; set; }
    public int WorkingSetSlotId { get; set; }
    public int Apm { get; set; }
    public int Messages { get; set; }
    public int Pings { get; set; }
    public PlayerLayout Layout { get; set; } = new();
    public List<DsUnit> Units { get; set; } = [];
    public List<Refinery> Refineries { get; set; } = [];
    public List<PlayerStats> Stats { get; set; } = [];
    public List<int> TierUpgrades { get; set; } = [];
    public Dictionary<string, int> Upgrades { get; set; } = [];
    public PlayerSpawnStats SpawnStats { get; set; } = new();
}

internal class ToonId
{
    public int Region { get; set; }
    public int Realm { get; set; }
    public int Id { get; set; }
}

internal sealed record Pos(int X, int Y)
{
    public static Pos Zero => new(0, 0);
};

internal class DsUnit
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Gameloop { get; set; }
    public Pos Position { get; set; } = Pos.Zero;
    public Pos DiedPosition { get; set; } = Pos.Zero;
}

internal class Refinery
{
    public int Gameloop { get; set; }
    public int UnitTagIndex { get; set; }
    public int UnitTagRecyle { get; set; }
    public bool Taken { get; set; }
}

internal class PlayerStats
{
    public int Gameloop { get; set; }
    public int MineralsCollectionRate { get; set; }
    public int MineralsUsedCurrentTechnology { get; set; }
    public int MineralsUsedActiveForces { get; set; }
    public int MineralsKilledArmy { get; set; }
}

internal class DsMiddle
{
    public int Gameloop { get; set; }
    public int ControlTeam { get; set; }
}

internal class DsUpgrade
{
    public int Gameloop { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal class MiddleIncome
{
    public int Team1 { get; set; }
    public int Team2 { get; set; }
}

public class PlayerSpawnStats
{
    public int Income { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradesSpent { get; set; }
}