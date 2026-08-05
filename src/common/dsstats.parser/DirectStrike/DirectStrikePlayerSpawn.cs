using System.Collections.ObjectModel;

namespace Sc2DirectStrike.Parser;

public sealed class DirectStrikePlayerSpawn
{
    public int Number { get; set; }
    public int StartGameloop { get; set; }
    public int EndGameloop { get; set; }
    public DirectStrikePlayerStats? SummaryStats { get; set; }
    public ReadOnlyCollection<DirectStrikeSpawnUnit> Units { get; set; } = [];
}
