
namespace dsstats.shared;

public record IhMatch
{
    public IhTeam[] Teams { get; set; } = [new(), new()];
    public int AgainstScore { get; set; }
    public int RatingGap { get; set; }
}

public record IhTeam
{
    public IhSlot[] Slots { get; set; } = [new(), new(), new()];
    public int WithScore { get; set; }
    public int Rating { get; set; }
}

public record IhSlot
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int Rating { get; set; }
}