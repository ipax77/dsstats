using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> AbathurUnitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Brutalisk
        ["Brutalisk"] = "Brutalisk",

        // Devourer
        ["Devourer"] = "Devourer",
        ["DevourerStarlight"] = "Devourer",

        // Guardian
        ["Guardian"] = "Guardian",
        ["GuardianStarlight"] = "Guardian",

        // Leviathan
        ["Leviathan"] = "Leviathan",
        ["LeviathanStarlight"] = "Leviathan",

        // Mutalisk
        ["Mutalisk"] = "Mutalisk",
        ["AbathurMutalisk"] = "Mutalisk",

        // Overseer
        ["Overseer"] = "Overseer",
        ["AbathurOverseer"] = "Overseer",

        // Ravager
        ["Ravager"] = "Ravager",
        ["RavagerAbathur"] = "Ravager",

        // Swarm Host
        ["SwarmHost"] = "Swarm Host",
        ["SwarmHostMP"] = "Swarm Host",

        // Swarm Queen
        ["SwarmQueen"] = "Swarm Queen",
        ["SwarmQueenStarlight"] = "Swarm Queen",

        // Vile Roach
        ["VileRoach"] = "Vile Roach",

        // Viper
        ["Viper"] = "Viper",
        ["ViperAbathur"] = "Viper",
    }.ToFrozenDictionary();

    private static string GetAbathurUnitName(string name) =>
        AbathurUnitMap.TryGetValue(name, out var mapped) ? mapped : name;

}
