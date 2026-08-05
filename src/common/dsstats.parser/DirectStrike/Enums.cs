namespace Sc2DirectStrike.Parser;

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

public enum Race
{
    None = 0,
    Random = 1,
    Terran = 2,
    Protoss = 3,
    Zerg = 4
}

public enum PlayerResult
{
    None = 0,
    Win = 1,
    Loss = 2,
    Undecided = 3
}
