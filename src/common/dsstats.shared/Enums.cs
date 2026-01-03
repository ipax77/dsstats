namespace dsstats.shared;

public enum GameMode
{
    None = 0,
    BrawlCommanders = 1,
    BrawlStandard = 2,
    Commanders = 3,
    CommandersHeroic = 4,
    Gear = 5,
    Sabotage = 6,
    Standard = 7,
    Switch = 8,
    Tutorial = 9
}

public enum Commander
{
    None = 0,
    Protoss = 1,
    Terran = 2,
    Zerg = 3,
    Abathur = 10,
    Alarak = 20,
    Artanis = 30,
    Dehaka = 40,
    Fenix = 50,
    Horner = 60,
    Karax = 70,
    Kerrigan = 80,
    Mengsk = 90,
    Nova = 100,
    Raynor = 110,
    Stetmann = 120,
    Stukov = 130,
    Swann = 140,
    Tychus = 150,
    Vorazun = 160,
    Zagara = 170,
    Zeratul = 180,
    Random = 500,
}

public enum Breakpoint
{
    None = 0,
    Min5 = 1,
    Min10 = 2,
    Min15 = 3,
    All = 4
}

public enum PlayerResult
{
    None = 0,
    Win = 1,
    Los = 2
}

public enum RatingType
{
    All = 0,
    Standard = 1,
    Commanders = 2,
    StandardTE = 3,
    CommandersTE = 4,
}

public enum LeaverType
{
    None = 0,
    OneLeaver = 1,
    OneEachTeam = 2,
    TwoSameTeam = 3,
    MoreThanTwo = 4
}

public enum UnitType
{
    None = 0,
    Ground = 1,
    Air = 2,
    AirAndGround = 3,
}

public enum UnitSize
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    VeryLarge = 4,
}

public enum TimePeriod
{
    None = 0,
    Last90Days = 3,
    Previous90Days = 4,
    Last12Months = 5,
    Previous12Months = 6,
    ThisYear = 11,
    LastYear = 12,
    AllTime = 13,
    Custom = 14
}

public enum StatsType
{
    Winrate = 0,
    Synergy = 1,
    Stats = 99,
}


[Flags]
public enum DsUnitType
{
    None = 0,
    Armored = 1 << 0,
    Biological = 1 << 1,
    Detector = 1 << 2,
    Frencied = 1 << 3,
    Heroic = 1 << 4,
    ImportantHero = 1 << 5,
    Light = 1 << 6,
    Massive = 1 << 7,
    Mechanical = 1 << 8,
    Psionic = 1 << 9,
    Structure = 1 << 10
}

[Flags]
public enum WeaponTarget
{
    None = 0,
    Air = 1 << 0,
    Ground = 1 << 1
}

[Flags]
public enum AbilityTarget
{
    None = 0,
    Self = 1 << 0,
    Single = 1 << 1,
    AOE = 1 << 2,
    Multiple = 1 << 3,
}

public enum UnitColor
{
    None = 0,
    Color1 = 1,
    Color2 = 2,
    Color3 = 3,
    Color4 = 4,
    Color5 = 5,
    Color6 = 6,
    Color7 = 7,
    Color8 = 8,
    Color9 = 9,
    Color10 = 10,
    Color11 = 11,
    Color12 = 12,
    Color13 = 13,
    Color14 = 14,
    Color15 = 15,
}

public static class EnumExtensions
{
    public static TEnum ParseEnumOrDefault<TEnum>(int? value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value))
        {
            return (TEnum)(object)value.Value;
        }

        return defaultValue;
    }

    public static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum defaultValue)
    where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<TEnum>(value, true, out var parsed) &&
            Enum.IsDefined(parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

}