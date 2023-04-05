namespace dsstats.sc2arcade.api.Models;

public record LobbyHistoryResponse
{
    public ResponsePage Page { get; set; } = new();
    public List<LobbyResult> Results { get; set; } = new();
}

public record ResponsePage
{
    public string? Prev { get; set; }
    public string? Next { get; set; }
}


public record Map
{
    public int RegionId { get; set; }

    public int BnetId { get; set; }

    public string? IconHash { get; set; }

    public string Name { get; set; } = string.Empty;

    public int MainCategoryId { get; set; }
}

public record LobbyResult
{
    public int Id { get; set; }

    public int RegionId { get; set; }

    public int BnetBucketId { get; set; }

    public int BnetRecordId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public int MapBnetId { get; set; }

    public int? ExtModBnetId { get; set; }

    public int? MultiModBnetId { get; set; }

    public int? MapVariantIndex { get; set; }

    public string MapVariantMode { get; set; } = string.Empty;

    public string LobbyTitle { get; set; } = string.Empty;

    public string HostName { get; set; } = string.Empty;

    public int? SlotsHumansTotal { get; set; }

    public int? SlotsHumansTaken { get; set; }

    public Match? Match { get; set; }

    public Map Map { get; set; } = new();

    public object? ExtMod { get; set; }

    public object? MultiMod { get; set; }

    public List<Slot> Slots { get; set; } = new();
}

public record Slot
{
    public int? SlotNumber { get; set; }

    public int? Team { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public record Match
{
    public int Result { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PlayerResult> ProfileMatches { get; set; } = new();
}

public record PlayerResult
{
    public string Decision { get; set; } = string.Empty;
    public PlayerProfile Profile { get; set; } = new();
}

public record PlayerProfile
{
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Discriminator { get; set; }
    public string? Avatar { get; set; }
}