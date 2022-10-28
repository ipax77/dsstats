
namespace pax.dsstats.shared;

public record ReplayDsRDto
{
    public DateTime GameTime { get; init; }
    public byte Playercount { get; init; }
    public int Maxleaver { get; init; }
    public int WinnerTeam { get; set; }
    public int Duration { get; init; }
    public List<ReplayPlayerDsRDto> ReplayPlayers { get; init; } = new();
}

public record ReplayPlayerDsRDto
{
    public int Team { get; init; }
    public PlayerResult PlayerResult { get; init; }
    public PlayerDsRDto Player { get; init; } = null!;
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Duration { get; init; }
    public bool IsUploader { get; init; }
    public Commander Race { get; set; }
}

public record PlayerDsRDto
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
}

public record PlayerRatingDto
{
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public double Mmr { get; init; }
    public double MmrStd { get; init; }
}