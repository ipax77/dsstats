using System.Text.Json.Serialization;

namespace dsstats.shared;

public class ReplaysRequest
{
    public RatingType RatingType { get; set; } = RatingType.All;
    public string? Name { get; set; }
    public string? Commander { get; set; }
    public bool LinkCommanders { get; set; }
    public List<TableOrder> TableOrders { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20_000;
    public int Skip { get; set; }
    public int Take { get; set; }
    public ReplaysFilter? Filter { get; set; }
    [JsonIgnore]
    public string? ReplayHash { get; set; }
}

public class ReplaysFilter
{
    public int Playercount { get; set; }
    public bool TournamentEdition { get; set; }
    public List<GameMode> GameModes { get; set; } = [];
    public List<ReplaysPosFilter> PosFilters { get; set; } = [];

    public void Reset()
    {
        Playercount = 0;
        TournamentEdition = false;
        GameModes = new() { GameMode.None };
        PosFilters.Clear();
    }
}

public class ReplaysPosFilter
{
    public int GamePos { set; get; }
    public Commander Commander { get; set; }
    public Commander OppCommander { get; set; }
    public string PlayerNameOrId { get; set; } = string.Empty;
    public List<ReplaysPosUnitFilter> UnitFilters { get; set; } = new();
}

public class ReplaysPosUnitFilter
{
    public Breakpoint Breakpoint { set; get; } = Breakpoint.All;
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Min { get; set; } = true;
}

public class ArcadeReplaysRequest
{
    public string? Name { get; set; }
    public List<TableOrder> TableOrders { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20_000;
    public int Skip { get; set; }
    public int Take { get; set; }
    [JsonIgnore]
    public string? ReplayHash { get; set; }
}