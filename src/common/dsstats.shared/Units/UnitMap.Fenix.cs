using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> FenixUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["PurifierAdept"] = "Adept",
            ["PurifierImmortal"] = "Immortal",
            ["PurifierTalis"] = "Talis",
            ["PurifierObserver"] = "Observer",
            ["Taldarin"] = "Taldarin",
            ["Conservator"] = "Conservator",
            ["Legionnaire"] = "Legionnaire",
            ["PurifierScout"] = "Scout",
            ["FenixWalker"] = "Fenix - Dragoon",
            ["Mojo"] = "Mojo",
            ["Kaldalis"] = "Kaldalis",
            ["PurifierInterceptor"] = "Interceptor",
            ["FenixPraetor"] = "Fenix - Praetor",
            ["Warbringer"] = "Warbringer",
            ["PurifierCarrier"] = "Carrier",
            ["PurifierDisruptor"] = "Disruptor",
            ["PurifierColossusFenix"] = "Colossus",
            ["Clolarion"] = "Clolarion",
            ["FenixFlyer"] = "Fenix - Arbiter",
            ["Probius"] = "Probius",

            // v2 names
            ["Flyer"] = "Fenix - Arbiter",
            ["Praetor"] = "Fenix - Praetor",
            ["PurifierColossus"] = "Colossus",
            ["Walker"] = "Fenix - Dragoon",
            ["PurifierAdept"] = "Adept",
            ["PurifierCarrier"] = "Carrier",
            ["PurifierDisruptor"] = "Disruptor",
            ["PurifierImmortal"] = "Immortal",
            ["PurifierObserver"] = "Observer",
            ["PurifierScout"] = "Scout",
            ["PurifierTalis"] = "Talis",


            // normalized names
            ["Fenix - Arbiter"] = "Fenix - Arbiter",
            ["Fenix - Dragoon"] = "Fenix - Dragoon",
            ["Fenix - Praetor"] = "Fenix - Praetor",
            ["Photon Cannon"] = "Photon Cannon",

        }.ToFrozenDictionary();

    private static string GetFenixUnitName(string name) =>
        FenixUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
