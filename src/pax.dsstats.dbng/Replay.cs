using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pax.dsstats.dbng;

public class Uploader
{
    public Uploader()
    {
        Players = new HashSet<Player>();
        BattleNetInfos = new HashSet<BattleNetInfo>();
        Replays = new HashSet<Replay>();
    }
    public int UploaderId { get; set; }
    public Guid AppGuid { get; set; }
    public string AppVersion { get; set; } = "";
    public string Identifier { get; set; } = "Anonymous";
    [Precision(0)]
    public DateTime LatestUpload { get; set; }
    [Precision(0)]
    public DateTime LatestReplay { get; set; }
    public virtual ICollection<Player> Players { get; set; }
    public ICollection<BattleNetInfo>? BattleNetInfos { get; set; }
    public ICollection<Replay> Replays { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public Commander MainCommander { get; set; }
    public int MainCount { get; set; }
    public int TeamGames { get; set; }
    [Precision(0)]
    public DateTime UploadLastDisabled { get; set; }
    public int UploadDisabledCount { get; set; }
    public bool UploadIsDisabled { get; set; }
    public bool IsDeleted { get; set; }
}

public class BattleNetInfo
{
    public int BattleNetInfoId { get; set; }
    public int BattleNetId { get; set; }
    public int UploaderId { get; set; }
    public Uploader Uploader { get; set; } = null!;
}

public class NoUploadResult
{
    public int NoUploadResultId { get; set; }
    public int TotalReplays { get; set; }
    [Precision(0)]
    public DateTime Created { get; set; }
    [Precision(0)]
    public DateTime LatestReplay { get; set; }
    public int NoUploadTotal { get; set; }
    public int NoUploadDefeats { get; set; }
    [Precision(0)]
    public DateTime LatestNoUpload { get; set; }
    [Precision(0)]
    public DateTime LatestUpload { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}

public class Player
{
    public Player()
    {
        ReplayPlayers = new HashSet<ReplayPlayer>();
        PlayerRatings = new HashSet<PlayerRating>();
    }
    public int PlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int NotUploadCount { get; set; }
    public int DisconnectCount { get; set; }
    public int RageQuitCount { get; set; }
    public int? UploaderId { get; set; }
    public virtual Uploader? Uploader { get; set; }
    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; }
    public virtual ICollection<PlayerRating> PlayerRatings { get; set; }
}

public class PlayerRating
{
    public int PlayerRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public double Rating { get; set; }
    public int Pos { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }
    public int MainCount { get; set; }
    public Commander Main { get; set; }
    [MaxLength(4000)]
    public string MmrOverTime { get; set; } = string.Empty;
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public PlayerRatingChange? PlayerRatingChange { get; set; }
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;
}

public class PlayerRatingChange
{
    public int PlayerRatingChangeId { get; set; }
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
    public int PlayerRatingId { get; set; }
    public PlayerRating PlayerRating { get; set; } = null!;
}

public class Event
{
    public Event()
    {
        ReplayEvents = new HashSet<ReplayEvent>();
    }
    public int EventId { get; set; }
    [MaxLength(200)]
    public string Name { get; set; } = null!;
    public Guid EventGuid { get; set; } = Guid.NewGuid();
    [Precision(0)]
    public DateTime EventStart { get; set; }
    public string? WinnerTeam { get; set; }
    public GameMode GameMode { get; set; }
    public virtual ICollection<ReplayEvent> ReplayEvents { get; set; }
}

public class ReplayEvent
{
    public ReplayEvent()
    {
        Replays = new HashSet<Replay>();
    }
    public int ReplayEventId { get; set; }
    [MaxLength(200)]
    public string Round { get; set; } = null!;
    public string WinnerTeam { get; set; } = null!;
    public string RunnerTeam { get; set; } = null!;
    public Commander Ban1 { get; set; }
    public Commander Ban2 { get; set; }
    public Commander Ban3 { get; set; }
    public Commander Ban4 { get; set; }
    public Commander Ban5 { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public virtual ICollection<Replay> Replays { get; set; }
}

public class Replay
{
    public Replay()
    {
        ReplayPlayers = new HashSet<ReplayPlayer>();
        Uploaders = new HashSet<Uploader>();
    }

    public int ReplayId { get; set; }
    [MaxLength(500)]
    public string FileName { get; set; } = null!;
    public bool TournamentEdition { get; set; }
    [Precision(0)]
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int PlayerPos { get; set; }
    public bool ResultCorrected { get; set; }
    public GameMode GameMode { get; set; }
    public int Objective { get; set; }
    public int Bunker { get; set; }
    public int Cannon { get; set; }
    public int Minkillsum { get; set; }
    public int Maxkillsum { get; set; }
    public int Minarmy { get; set; }
    public int Minincome { get; set; }
    public int Maxleaver { get; set; }
    public byte Playercount { get; set; }
    public string ReplayHash { get; set; } = null!;
    public bool DefaultFilter { get; set; }
    public int Views { get; set; }
    public int Downloads { get; set; }
    [MaxLength(4000)]
    public string Middle { get; set; } = null!;
    public string CommandersTeam1 { get; set; } = null!;
    public string CommandersTeam2 { get; set; } = null!;
    public int? ReplayEventId { get; set; }
    public ReplayEvent? ReplayEvent { get; set; }
    public ReplayRating? ReplayRatingInfo { get; set; }
    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; }
    public virtual ICollection<Uploader> Uploaders { get; set; }
    [NotMapped]
    public int UploaderId { get; set; }
}

public class ReplayPlayer
{
    public ReplayPlayer()
    {
        Spawns = new HashSet<Spawn>();
        Upgrades = new HashSet<PlayerUpgrade>();
    }

    public int ReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    [MaxLength(50)]
    public string? Clan { get; set; }
    public int GamePos { get; set; }
    public int Team { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int Duration { get; set; }
    public Commander Race { get; set; }
    public Commander OppRace { get; set; }
    public int APM { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public bool IsUploader { get; set; }
    public bool IsLeaver { get; set; }
    public bool DidNotUpload { get; set; }
    [MaxLength(300)]
    public string TierUpgrades { get; set; } = null!;
    [MaxLength(300)]
    public string Refineries { get; set; } = null!;
    public string? LastSpawnHash { get; set; }
    public int Downloads { get; set; }
    public int Views { get; set; }
    public int ReplayId { get; set; }
    public virtual Replay Replay { get; set; } = null!;
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;
    public RepPlayerRating? ReplayPlayerRatingInfo { get; set; }
    public virtual ICollection<Spawn> Spawns { get; set; }
    public virtual ICollection<PlayerUpgrade> Upgrades { get; set; }
}

public class Spawn
{
    public Spawn()
    {
        Units = new HashSet<SpawnUnit>();
    }

    public int SpawnId { get; set; }
    public int Gameloop { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradeSpent { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer ReplayPlayer { get; set; } = null!;
    public virtual ICollection<SpawnUnit> Units { get; set; }
}

public class SpawnUnit
{
    public int SpawnUnitId { get; set; }
    public byte Count { get; set; }
    [MaxLength(4000)]
    public string Poss { get; set; } = null!;
    public int UnitId { get; set; }
    public virtual Unit Unit { get; set; } = null!;
    public int SpawnId { get; set; }
    public virtual Spawn Spawn { get; set; } = null!;

}

public class PlayerUpgrade
{
    public int PlayerUpgradeId { get; set; }
    public int Gameloop { get; set; }
    public int UpgradeId { get; set; }
    public virtual Upgrade Upgrade { get; set; } = null!;
    public int ReplayPlayerId { get; set; }
    public virtual ReplayPlayer ReplayPlayer { get; set; } = null!;
}

public class Unit
{
    public int UnitId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
}

public class Upgrade
{
    public Upgrade()
    {
        ReplayPlayers = new HashSet<ReplayPlayer>();
    }
    public int UpgradeId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    public int Cost { get; set; }
    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; }
}

public class ReplayViewCount
{
    public int ReplayViewCountId { get; set; }
    [MaxLength(64)]
    public string ReplayHash { get; set; } = null!;
}

public class ReplayDownloadCount
{
    public int ReplayDownloadCountId { get; set; }
    [MaxLength(64)]
    public string ReplayHash { get; set; } = null!;
}