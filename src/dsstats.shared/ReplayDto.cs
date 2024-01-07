using System.ComponentModel.DataAnnotations;

namespace dsstats.shared;

public record PlayerDto
{
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public int RealmId { get; init; }
}

public record ReplayDto
{
    public string FileName { get; set; } = null!;
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int PlayerPos { get; set; }
    public bool ResultCorrected { get; set; }
    public GameMode GameMode { get; init; }
    public int Objective { get; init; }
    public int Bunker { get; init; }
    public int Cannon { get; init; }
    public int Minkillsum { get; init; }
    public int Maxkillsum { get; init; }
    public int Minarmy { get; init; }
    public int Minincome { get; init; }
    public int Maxleaver { get; init; }
    public byte Playercount { get; init; }
    public string ReplayHash { get; set; } = "";
    public bool DefaultFilter { get; set; }
    public int Views { get; init; }
    public int Downloads { get; init; }
    public string Middle { get; init; } = null!;
    public string CommandersTeam1 { get; init; } = null!;
    public string CommandersTeam2 { get; init; } = null!;
    public bool TournamentEdition { get; init; }
    public ReplayEventDto? ReplayEvent { get; set; }
    public ICollection<ReplayPlayerDto> ReplayPlayers { get; init; } = new HashSet<ReplayPlayerDto>();
}

public record ReplayPlayerDto
{
    public string Name { get; init; } = null!;
    public string? Clan { get; init; }
    public int GamePos { get; init; }
    public int Team { get; init; }
    public PlayerResult PlayerResult { get; set; }
    public float? MmrChange { get; set; } = null;
    public int Duration { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int APM { get; init; }
    public int Income { get; init; }
    public int Army { get; init; }
    public int Kills { get; init; }
    public int UpgradesSpent { get; init; }
    public bool IsUploader { get; set; }
    public string TierUpgrades { get; init; } = null!;
    public string Refineries { get; init; } = null!;
    public int Downloads { get; init; }
    public int Views { get; init; }
    public PlayerDto Player { get; init; } = null!;
    public ICollection<SpawnDto> Spawns { get; init; } = new HashSet<SpawnDto>();
    public ICollection<PlayerUpgradeDto> Upgrades { get; init; } = new HashSet<PlayerUpgradeDto>();
}

public record SpawnDto
{
    public int Gameloop { get; init; }
    public Breakpoint Breakpoint { get; init; }
    public int Income { get; init; }
    public int GasCount { get; init; }
    public int ArmyValue { get; init; }
    public int KilledValue { get; init; }
    public int UpgradeSpent { get; init; }
    public ICollection<SpawnUnitDto> Units { get; init; } = new HashSet<SpawnUnitDto>();
}

public record SpawnUnitDto
{
    public byte Count { get; init; }
    public string Poss { get; init; } = null!;
    public UnitDto Unit { get; init; } = null!;
}

public record PlayerUpgradeDto
{
    public int Gameloop { get; set; }
    public virtual UpgradeDto Upgrade { get; set; } = null!;
}

public record UnitDto
{
    public string Name { get; init; } = null!;
}

public record UpgradeDto
{
    public string Name { get; init; } = null!;
}

public record EventDto
{
    [MaxLength(200)]
    public string Name { get; set; } = "unknown";
}

public record ReplayEventDto
{
    [MaxLength(200)]
    public string Round { get; set; } = "GroupA";
    public string WinnerTeam { get; set; } = "Team1";
    public string RunnerTeam { get; set; } = "Team2";
    public Commander Ban1 { get; set; }
    public Commander Ban2 { get; set; }
    public Commander Ban3 { get; set; }
    public Commander Ban4 { get; set; }
    public Commander Ban5 { get; set; }
    public EventDto Event { get; set; } = new();
}

public record EventListDto
{
    public string Name { get; init; } = "";
    public string? WinnerTeam { get; init; }
    public GameMode GameMode { get; init; }
}