namespace dsstats.shared;

public sealed class ReplayTourneyInfoDto
{
    public List<TourneyPlayerDto> Players { get; set; } = [];
}

public sealed class TourneyPlayerDto
{
    public PlayerDto Player { get; set; } = new();
    public bool Observer { get; set; }
    public Commander AssignedRace { get; set; }
    public Commander SelectedRace { get; set; }
    public int WorkingSetSlotId { get; set; }
    public PlayerColorDto PlayerColor { get; set; } = new();
}

public sealed class PlayerColorDto
{
    public int A { get; set; }
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}