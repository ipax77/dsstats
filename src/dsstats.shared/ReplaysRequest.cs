using System.Text.Json.Serialization;

namespace dsstats.shared;

public record ReplaysRequest
{
    public int Skip { get; set; }
    public int Take { get; set; }
    public string Commanders { get; set; } = string.Empty;
    public string Players { get; set; } = string.Empty;
    public bool Link { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public PlayerId? PlayerId { get; set; }
    public PlayerId? PlayerIdVs { get; set; }
    public PlayerId? PlayerIdWith { get; set; }
    public bool Arcade { get; set; }
    public bool MauiInfo { get; set; }
    public ReplaysFilter? Filter { get; set; }
    [JsonIgnore]
    public string? ReplayHash { get; set; }
}

public record ReplaysResponse
{
    public PlayerId? PlayerId { get; set; }
    public List<ReplayListDto> Replays { get; set; } = new();
}

public record ReplayListDto
{
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public GameMode GameMode { get; init; }
    public bool TournamentEdition { get; init; }
    public string ReplayHash { get; init; } = string.Empty;
    public bool DefaultFilter { get; init; }
    public string CommandersTeam1 { get; init; } = string.Empty;
    public string CommandersTeam2 { get; init; } = string.Empty;
    public int MaxLeaver { get; init; }
    public double? Exp2Win { get; init; }
    public int AvgRating { get; init; }
    public ReplayPlayerInfo? PlayerInfo { get; set; }
}

public record ReplayPlayerInfo
{
    public string Name { get; init; } = string.Empty;
    public int Pos { get; init; }
    public double RatingChange { get; init; }
    public Commander Commander { get; init; }
}

public record ReplayListRatingDto : ReplayListDto
{
    public List<ReplayPlayerListDto> ReplayPlayers { get; set; } = new();
}

public record ReplayPlayerListDto
{
    public string Name { get; set; } = string.Empty;
    public int GamePos { get; set; }
    public Commander Race { get; set; }
    public Commander OppRace { get; set; }
    public PlayerId Player { get; set; } = null!;
    public ReplayPlayerRatingListDto? ReplayPlayerRating { get; set; }
}

public record ReplayPlayerRatingListDto
{
    public double RatingChange { get; set; }
    public double Exp2Win { get; set; }
}

public record ReplaysFilter
{
    public int Playercount { get; set; }
    public bool TournamentEdition { get; set; }
    public List<GameMode> GameModes { get; set; } = new() { GameMode.None };
    public List<ReplaysPosFilter> PosFilters { get; set; } = new();
    public ReplaysRatingRequest? ReplaysRatingRequest { get; set; }

    public void Reset()
    {
        Playercount = 0;
        TournamentEdition = false;
        GameModes = new() { GameMode.None };
        PosFilters.Clear();
        ReplaysRatingRequest = null;
    }
}

public record ReplaysPosFilter
{
    public int GamePos {  set; get; }
    public Commander Commander { get; set; }
    public Commander OppCommander { get; set; }
    public string PlayerNameOrId { get; set; } = string.Empty;
    public List<ReplaysPosUnitFilter> UnitFilters { get; set; } = new();
}

public record ReplaysPosUnitFilter
{
    public Breakpoint Breakpoint { set; get; } = Breakpoint.All;
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Min { get; set; } = true;
}

public record ReplaysRatingRequest
{
    public RatingCalcType RatingCalcType { set; get; } = RatingCalcType.Combo;
    public RatingType RatingType { set; get; } = RatingType.Cmdr;
    public bool WithoutLeavers { get; set; }
    public int AvgMinRating { get; set; }
    public int FromExp2Win { get; set; }
    public int ToExp2Win { get; set; }
}

public record PlayerReplaysRequest
{
    public PlayerId PlayerId { get; set; } = null!;
    public PlayerId? PlayerIdVs { get; set; }
    public PlayerId? PlayerIdWith { get; set; }
    public string? ReplayHash { get; set; }
}