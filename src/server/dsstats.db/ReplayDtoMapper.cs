using dsstats.shared;
using dsstats.shared.Arcade;

namespace dsstats.db;

public static class ReplayDtoMapper
{
    public static Replay ToEntity(this ReplayDto dto)
    {
        var replay = new Replay
        {
            Title = dto.Title,
            FileName = string.IsNullOrEmpty(dto.FileName) ? null : dto.FileName,
            Version = dto.Version,
            GameMode = dto.GameMode,
            PlayerCount = dto.Players.Count,
            RegionId = dto.RegionId,
            TE = dto.Title.EndsWith("TE"),
            Gametime = dto.Gametime,
            BaseBuild = dto.BaseBuild,
            Duration = dto.Duration,
            Cannon = dto.Cannon,
            Bunker = dto.Bunker,
            WinnerTeam = dto.WinnerTeam,
            MiddleChanges = dto.MiddleChanges.ToArray(),
            Players = dto.Players.Select(p => p.ToEntity(dto)).ToList(),
            Imported = DateTime.UtcNow,

            ReplayHash = dto.ComputeHash(),
            CompatHash = dto.ComputeCandidateHash(),
        };

        // Set Replay reference on each player
        foreach (var player in replay.Players)
        {
            player.Replay = replay;
        }

        return replay;
    }

    public static ReplayPlayer ToEntity(this ReplayPlayerDto dto, ReplayDto replay)
    {
        Commander race = dto.Race;
        if (Data.IsCommanderGameMode(replay.GameMode) && (int)dto.Race <= 3)
        {
            race = Commander.None;
        }

        return new ReplayPlayer
        {
            Name = dto.Name,
            Clan = dto.Clan,
            Race = race,
            SelectedRace = dto.SelectedRace,
            OppRace = GetOppRace(dto, replay),
            GamePos = dto.GamePos,
            TeamId = dto.TeamId,
            Result = dto.Result,
            Duration = dto.Duration,
            Apm = dto.Apm,
            Messages = dto.Messages,
            Pings = dto.Pings,
            IsMvp = dto.IsMvp,
            IsUploader = dto.IsUploader,
            Spawns = dto.Spawns.Select(s => s.ToEntity()).ToList(),
            TierUpgrades = dto.TierUpgrades.ToArray(),
            Refineries = dto.Refineries.ToArray(),
            Upgrades = dto.Upgrades.Select(s => new PlayerUpgrade()
            {
                Gameloop = s.Gameloop,
                Upgrade = new() { Name = s.Name }
            }).ToList(),
            Player = dto.Player?.ToEntity()
        };
    }

    private static Commander GetOppRace(ReplayPlayerDto player, ReplayDto replay)
    {
        var oppPos = player.GamePos switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            4 => 1,
            5 => 2,
            6 => 3,
            _ => 0
        };
        var oppPlayer = replay.Players.FirstOrDefault(f => f.GamePos == oppPos);

        Commander race = oppPlayer?.Race ?? Commander.None;
        if (Data.IsCommanderGameMode(replay.GameMode) && (int)race <= 3)
        {
            race = Commander.None;
        }
        return race;
    }

    public static Player ToEntity(this PlayerDto dto)
    {
        return new Player
        {
            Name = dto.Name,
            ToonId = new ToonId
            {
                Region = dto.ToonId.Region,
                Realm = dto.ToonId.Realm,
                Id = dto.ToonId.Id
            }
        };
    }

    public static Spawn ToEntity(this SpawnDto dto)
    {
        return new()
        {
            Breakpoint = dto.Breakpoint,
            GasCount = dto.GasCount,
            Income = dto.Income,
            ArmyValue = dto.ArmyValue,
            KilledValue = dto.KilledValue,
            UpgradeSpent = dto.UpgradeSpent,
            Units = dto.Units.Select(s => new SpawnUnit()
            {
                Count = s.Count,
                Positions = s.Positions.ToArray(),
                Unit = new() { Name = s.Name }
            }).ToList()
        };
    }

    public static ReplayDto ToDto(this Replay replay)
    {
        return new ReplayDto
        {
            Title = replay.Title,
            FileName = replay.FileName ?? string.Empty,
            Version = replay.Version,
            GameMode = replay.GameMode,
            Players = replay.Players.Select(p => p.ToDto()).ToList(),
            Gametime = replay.Gametime,
            RegionId = replay.RegionId,
            BaseBuild = replay.BaseBuild,
            Duration = replay.Duration,
            Cannon = replay.Cannon,
            Bunker = replay.Bunker,
            WinnerTeam = replay.WinnerTeam,
            MiddleChanges = replay.MiddleChanges.ToList(),
            CompatHash = replay.CompatHash,
        };
    }

    public static ReplayPlayerDto ToDto(this ReplayPlayer replayPlayer)
    {
        return new ReplayPlayerDto
        {
            Name = replayPlayer.Name,
            Clan = replayPlayer.Clan,
            Race = replayPlayer.Race,
            SelectedRace = replayPlayer.SelectedRace,
            GamePos = replayPlayer.GamePos,
            TeamId = replayPlayer.TeamId,
            Result = replayPlayer.Result,
            Duration = replayPlayer.Duration,
            Apm = replayPlayer.Apm,
            Messages = replayPlayer.Messages,
            Pings = replayPlayer.Pings,
            IsMvp = replayPlayer.IsMvp,
            IsUploader = replayPlayer.IsUploader,
            Spawns = replayPlayer.Spawns.Select(s => s.ToDto()).ToList(),
            TierUpgrades = replayPlayer.TierUpgrades.ToList(),
            Refineries = replayPlayer.Refineries.ToList(),
            Upgrades = replayPlayer.Upgrades.Select(u => new UpgradeDto()
            {
                Gameloop = u.Gameloop,
                Name = u.Upgrade!.Name,
            }).ToList(),
            Player = replayPlayer.Player!.ToDto()
        };
    }

    public static PlayerDto ToDto(this Player player)
    {
        return new PlayerDto
        {
            PlayerId = player.PlayerId,
            Name = player.Name,
            ToonId = new ToonIdDto
            {
                Region = player.ToonId.Region,
                Realm = player.ToonId.Realm,
                Id = player.ToonId.Id
            }
        };
    }

    public static SpawnDto ToDto(this Spawn spawn)
    {
        return new SpawnDto
        {
            Breakpoint = spawn.Breakpoint,
            GasCount = spawn.GasCount,
            Income = spawn.Income,
            ArmyValue = spawn.ArmyValue,
            KilledValue = spawn.KilledValue,
            UpgradeSpent = spawn.UpgradeSpent,
            Units = spawn.Units.Select(u => new UnitDto()
            {
                Count = u.Count,
                Positions = u.Positions.ToList(),
                Name = u.Unit!.Name
            }).ToList()
        };
    }
}

public static class ArcadeReplayDtoMapper
{
    public static ArcadeReplay ToEntity(this ArcadeReplayDto replay, Dictionary<ToonIdRec, PlayerInfo> toonIdDict)
    {
        return new ArcadeReplay()
        {
            RegionId = replay.RegionId,
            BnetBucketId = replay.BnetBucketId,
            BnetRecordId = replay.BnetRecordId,
            GameMode = replay.GameMode,
            CreatedAt = replay.CreatedAt,
            Duration = replay.Duration,
            PlayerCount = replay.PlayerCount,
            WinnerTeam = replay.WinnerTeam,
            Imported = DateTime.UtcNow,
            Players = replay.Players.Select(p => p.ToEntity(toonIdDict)).ToList(),
        };
    }

    public static ArcadeReplayPlayer ToEntity(this ArcadeReplayPlayerDto player, Dictionary<ToonIdRec, PlayerInfo> toonIdDict)
    {
        var toonIdKey = new ToonIdRec(player.Player.ToonId.Region, player.Player.ToonId.Realm, player.Player.ToonId.Id);
        return new()
        {
            SlotNumber = player.SlotNumber,
            Team = player.Team,
            PlayerId = toonIdDict[toonIdKey].PlayerId,
            Player = null
        };
    }

    public static ArcadeReplayKey GetKey(this ArcadeReplayDto replay)
    {
        return new(replay.RegionId, replay.BnetBucketId, replay.BnetRecordId);
    }

    public static ReplayDto ToDto(this ArcadeReplay replay)
    {
        return new()
        {
            RegionId = replay.RegionId,
            GameMode = replay.GameMode,
            Duration = replay.Duration,
            Players = replay.Players.Select(p => p.ToDto(replay.Duration)).ToList(),
            WinnerTeam = replay.WinnerTeam,
        };
    }

    public static ReplayPlayerDto ToDto(this ArcadeReplayPlayer player, int duration)
    {
        return new()
        {
            GamePos = player.SlotNumber,
            TeamId = player.Team,
            Duration = duration,
            Name = player.Player?.Name ?? string.Empty,
            Player = player.Player!.ToDto()
        };
    }
}