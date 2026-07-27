namespace Sc2DirectStrike.Parser;

public sealed class DirectStrikeSpawnUnit
{
    public int UnitIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Gameloop { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int? DiedGameloop { get; set; }
    public int? DiedX { get; set; }
    public int? DiedY { get; set; }
}
