namespace dsstats.shared;

public class PlayerV2Dto
{
    public string Name { get; init; } = string.Empty;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public int RealmId { get; init; }
}

public class ReplayV2Dto
{
    public string FileName { get; set; } = string.Empty;
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
    public int Minkillsum { get; set; }
    public int Maxkillsum { get; set; }
    public int Minarmy { get; set; }
    public int Minincome { get; set; }
    public int Maxleaver { get; set; }
    public byte Playercount { get; init; }
    public string ReplayHash { get; set; } = string.Empty;
    public string? CompatHash { get; set; }
    public bool DefaultFilter { get; set; }
    public int Views { get; init; }
    public int Downloads { get; init; }
    public string Middle { get; init; } = string.Empty;
    public string CommandersTeam1 { get; init; } = string.Empty;
    public string CommandersTeam2 { get; init; } = string.Empty;
    public bool TournamentEdition { get; init; }
    public List<ReplayPlayerV2Dto> ReplayPlayers { get; init; } = [];

    public ReplayV2Dto() { }

    public ReplayV2Dto(ReplayDto replay)
    {
        GameTime = replay.Gametime;
        GameMode = replay.GameMode;
        Duration = replay.Duration;
        WinnerTeam = replay.WinnerTeam;
        Bunker = replay.Bunker;
        Cannon = replay.Cannon;
        Playercount = (byte)replay.Players.Count;
        Middle = GetMiddleString(replay.MiddleChanges);
        CommandersTeam1 = GetCommandersString(replay, 1);
        CommandersTeam2 = GetCommandersString(replay, 2);
        TournamentEdition = replay.Title.EndsWith("TE");
        ReplayPlayers = replay.Players.Select(s => GetReplayPlayer(s, replay)).ToList();
        Minkillsum = ReplayPlayers.Min(m => m.Kills);
        Maxkillsum = ReplayPlayers.Max(m => m.Kills);
        Minarmy = ReplayPlayers.Min(m => m.Army);
        Minincome = ReplayPlayers.Min(m => m.Income);
        Maxleaver = Math.Max(0, ReplayPlayers.Max(m => Duration - m.Duration));
        CompatHash = replay.CompatHash;
    }

    private static string GetMiddleString(List<int> middleChanges)
    {
        if (middleChanges.Count == 0)
        {
            return string.Empty;
        }
        return middleChanges[0] + "|" + string.Join("|", middleChanges[1..].Select(c => GetGameloop(c)));
    }

    private ReplayPlayerV2Dto GetReplayPlayer(ReplayPlayerDto s, ReplayDto replay)
    {
        Commander race = s.Race;
        if (Data.IsCommanderGameMode(replay.GameMode) && (int)s.Race <= 3)
        {
            race = Commander.None;
        }

        ReplayPlayerV2Dto rp = new()
        {
            Name = s.Name,
            Clan = s.Clan,
            GamePos = s.GamePos,
            Team = s.TeamId,
            PlayerResult = s.Result,
            Duration = s.Duration,
            Race = race,
            OppRace = GetOppCommander(s.GamePos, replay),
            APM = s.Apm,
            TierUpgrades = string.Join("|", s.TierUpgrades.Select(s => GetGameloop(s))),
            Refineries = string.Join("|", s.Refineries.Select(s => GetGameloop(s))),
            Upgrades = s.Upgrades.Select(u => new PlayerUpgradeV2Dto
            {
                Gameloop = u.Gameloop,
                Upgrade = new UpgradeV2Dto { Name = u.Name }
            }).ToList(),
            Player = new PlayerV2Dto
            {
                Name = s.Player.Name,
                ToonId = s.Player.ToonId.Id,
                RegionId = s.Player.ToonId.Region,
                RealmId = s.Player.ToonId.Realm
            },
            Spawns = s.Spawns.Select(spawn => new SpawnV2Dto
            {
                Breakpoint = spawn.Breakpoint,
                Income = spawn.Income,
                GasCount = spawn.GasCount,
                ArmyValue = spawn.ArmyValue,
                KilledValue = spawn.KilledValue,
                UpgradeSpent = spawn.UpgradeSpent,
                Units = spawn.Units.Select(unit => new SpawnUnitV2Dto
                {
                    Count = (byte)unit.Count,
                    Poss = string.Join(",", unit.Positions),
                    Unit = new UnitV2Dto { Name = unit.Name }
                }).ToList()
            }).ToList(),
        };
        var lastSpawn = s.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
        if (lastSpawn != null)
        {
            rp.Income = lastSpawn.Income;
            rp.Army = lastSpawn.ArmyValue;
            rp.Kills = lastSpawn.KilledValue;
            rp.UpgradesSpent = lastSpawn.UpgradeSpent;
        }
        return rp;
    }

    private static Commander GetOppCommander(int gamePos, ReplayDto replay)
    {
        var race = gamePos switch
        {
            1 => replay.Players.FirstOrDefault(x => x.GamePos == 4)?.Race,
            2 => replay.Players.FirstOrDefault(x => x.GamePos == 5)?.Race,
            3 => replay.Players.FirstOrDefault(x => x.GamePos == 6)?.Race,
            4 => replay.Players.FirstOrDefault(x => x.GamePos == 1)?.Race,
            5 => replay.Players.FirstOrDefault(x => x.GamePos == 2)?.Race,
            6 => replay.Players.FirstOrDefault(x => x.GamePos == 3)?.Race,
            _ => Commander.None
        };
        if (race is null || Data.IsCommanderGameMode(replay.GameMode) && (int)race <= 3)
        {
            race = Commander.None;
        }
        return race ?? Commander.None;
    }

    private string GetCommandersString(ReplayDto replay, int team)
    {
        var commanders = replay.Players.Where(x => x.TeamId == team)
            .Select(s => s.Race).ToList();
        if (commanders.Count == 0)
        {
            return string.Empty;
        }
        return '|' + string.Join("|", commanders.Select(c => (int)c)) + '|';
    }

    private static int GetGameloop(int sec)
    {
        return (int)(sec * 22.4);
    }
}

public class ReplayPlayerV2Dto
{
    public string Name { get; init; } = string.Empty;
    public string? Clan { get; init; }
    public int GamePos { get; init; }
    public int Team { get; init; }
    public PlayerResult PlayerResult { get; set; }
    public double? MmrChange { get; set; }
    public int Duration { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int APM { get; init; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public bool IsUploader { get; set; }
    public string TierUpgrades { get; init; } = string.Empty;
    public string Refineries { get; init; } = string.Empty;
    public int Downloads { get; init; }
    public int Views { get; init; }
    public PlayerV2Dto Player { get; init; } = new();
    public List<SpawnV2Dto> Spawns { get; init; } = [];
    public List<PlayerUpgradeV2Dto> Upgrades { get; init; } = [];
}

public class SpawnV2Dto
{
    public int Gameloop { get; init; }
    public Breakpoint Breakpoint { get; init; }
    public int Income { get; init; }
    public int GasCount { get; init; }
    public int ArmyValue { get; init; }
    public int KilledValue { get; init; }
    public int UpgradeSpent { get; init; }
    public List<SpawnUnitV2Dto> Units { get; init; } = [];
}

public class SpawnUnitV2Dto
{
    public byte Count { get; set; }
    public string Poss { get; set; } = string.Empty;
    public UnitV2Dto Unit { get; init; } = new();
}

public class PlayerUpgradeV2Dto
{
    public int Gameloop { get; set; }
    public virtual UpgradeV2Dto Upgrade { get; set; } = new();
}

public class UnitV2Dto
{
    public string Name { get; init; } = string.Empty;
}

public class UpgradeV2Dto
{
    public string Name { get; init; } = string.Empty;
}

public static class ReplayV2DtoMapper
{
    public static ReplayDto ToV3Dto(this ReplayV2Dto dto)
    {
        return new()
        {
            Title = dto.TournamentEdition ? "Direct Strike TE" : "Direct Strike",
            Version = "v2",
            GameMode = dto.GameMode,
            RegionId = dto.ReplayPlayers.FirstOrDefault()?.Player.RegionId ?? 0,
            Gametime = dto.GameTime,
            BaseBuild = 0,
            Duration = dto.Duration,
            Cannon = dto.Cannon,
            Bunker = dto.Bunker,
            WinnerTeam = dto.WinnerTeam,
            MiddleChanges = GetMiddle(dto.Middle),
            Players = dto.ReplayPlayers.Select(p => p.ToV3Dto(dto)).ToList(),
            CompatHash = dto.CompatHash ?? string.Empty,
        };
    }

    private static List<int> GetMiddle(string middle)
    {
        if (string.IsNullOrEmpty(middle))
        {
            return [];
        }
        List<int> changes = [];
        var middleString = middle.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (middleString.Length < 2)
        {
            return [];
        }
        changes.Add(int.Parse(middleString[0]));
        changes.AddRange(middleString[1..].Select(s => (int)(int.Parse(s) / 22.4)));
        return changes;
    }

    public static ReplayPlayerDto ToV3Dto(this ReplayPlayerV2Dto dto, ReplayV2Dto replay)
    {
        return new()
        {
            Name = dto.Name.Length > 20 ? dto.Name[..20] : dto.Name,
            Clan = dto.Clan?.Length > 10 ? dto.Clan[..10] : dto.Clan,
            Race = dto.Race,
            SelectedRace = Commander.None,
            GamePos = dto.GamePos,
            TeamId = dto.Team,
            Result = dto.PlayerResult,
            Duration = dto.Duration,
            Apm = dto.APM,
            IsMvp = dto.Kills == replay.Maxkillsum,
            IsUploader = dto.IsUploader,
            Spawns = dto.Spawns.Select(s => s.ToV3Dto()).ToList(),
            TierUpgrades = dto.TierUpgrades.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => (int)(int.Parse(s) / 22.4)).ToList(),
            Refineries = dto.Refineries.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => (int)(int.Parse(s) / 22.4)).ToList(),
            Upgrades = dto.Upgrades.Select(s => new UpgradeDto()
            {
                Gameloop = s.Gameloop,
                Name = s.Upgrade.Name,
            }).ToList(),
            Player = new()
            {
                Name = dto.Player.Name.Length > 20 ? dto.Player.Name[..20] : dto.Player.Name,
                ToonId = new()
                {
                    Realm = dto.Player.RealmId,
                    Region = dto.Player.RegionId,
                    Id = dto.Player.ToonId,
                }
            }
        };
    }

    public static SpawnDto ToV3Dto(this SpawnV2Dto dto)
    {
        return new()
        {
            Breakpoint = dto.Breakpoint,
            GasCount = dto.GasCount,
            Income = dto.Income,
            ArmyValue = dto.ArmyValue,
            KilledValue = dto.KilledValue,
            UpgradeSpent = dto.UpgradeSpent,
            Units = dto.Units.Select(s => new UnitDto()
            {
                Count = s.Count,
                Positions = s.Poss.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).ToList(),
                Name = s.Unit.Name
            }).ToList()
        };
    }
}