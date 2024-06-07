using System.Diagnostics.Tracing;

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
    Zeratul = 180
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
    None = 0,
    Cmdr = 1,
    Std = 2,
    CmdrTE = 3,
    StdTE = 4
}

public enum TimePeriod
{
    None = 0,
    ThisMonth = 1,
    LastMonth = 2,
    Past90Days = 3,
    ThisYear = 4,
    LastYear = 5,
    Last2Years = 6,
    Patch2_60 = 7,
    All = 8,
    Patch2_71 = 9
}

public enum LeaverType
{
    None = 0,

    OneLeaver = 1,
    OneEachTeam = 2,
    TwoSameTeam = 3,
    MoreThanTwo = 4
}

public enum RatingChangeTimePeriod
{
    None = 0,
    Past24h = 1,
    Past10Days = 2,
    Past30Days = 3
}

public enum TeamRequest
{
    None = 0,
    Team1 = 1,
    Team2 = 2
}

public enum RatingCalcType
{
    Dsstats = 1,
    Arcade = 2,
    Combo = 3
}

public enum DamageChartType
{
    Damage = 0,
    MVP = 1,
    Army = 2,
}

public enum UnitSize
{
    VerySmall = 0,
    Small = 1,
    Normal = 2,
    Big = 3,
    Hero = 4,
    VeryBig = 5,
    AirSmall = 6,
    AirNormal = 7,
    AirBig = 8,
    AirVeryBig = 9
}

[Flags]
public enum UnitType
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
    Color4= 4,
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

public enum FaqLevel
{
    None = 0,
    Basic = 1,
    Intermediate = 2,
    Advanced = 3
}

public enum QueuePriority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public enum PickBanMode
{
    None = 0,
    Standard = 1,
    Commanders = 2,
    Name = 3,
    StdRandom = 4,
    Std1v1 = 5,
    CmdrBanOnly = 6
}