namespace pax.dsstats.shared;

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
    Std = 2
}

public enum TimePeriod
{
    None = 0,
    Past90Days = 1,
    ThisMonth = 2,
    ThisYear = 3,
    Last2Years = 4,
    Patch2_60 = 5,
    LastMonth = 6,
    LastYear = 7,
    All = 8
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