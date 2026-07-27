namespace Sc2DirectStrike.Parser;

public sealed class DirectStrikePlayerStats
{
    public int Gameloop { get; set; }
    public TimeSpan Time { get; set; }
    public int MineralsCollectionRate { get; set; }
    public int MineralsUsedActiveForces { get; set; }
    public int MineralsUsedCurrentTechnology { get; set; }
    public int MineralsKilledArmy { get; set; }
    public int MineralsLostArmy { get; set; }
}
