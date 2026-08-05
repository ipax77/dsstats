namespace Sc2DirectStrike.Parser;

public sealed class DirectStrikeObserver
{
    public string Name { get; set; } = string.Empty;
    public string? Clan { get; set; }
    public int Id { get; set; }
    public int Region { get; set; }
    public int Realm { get; set; }
    public int SlotId { get; set; }
}
