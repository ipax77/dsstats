namespace dsstats.parser;

public static partial class DsstatsParser
{
    internal static void SetCompatStats(DsstatsReplay replay)
    {
        foreach (var player in replay.Players)
        {
            var playerUnits = player.Units.OrderBy(x => x.Gameloop).ToList();
            var playerStats = player.Stats.Where(x => x.MineralsCollectionRate > 0).ToList();

            if (playerUnits.Count == 0)
            {
                continue;
            }

            int lastLoop = playerUnits.First().Gameloop;

            List<DsUnit> spawnUnits = [];
            List<PlayerSpawnStats> spawnStats = [];

            foreach (var unit in player.Units.OrderBy(o => o.Gameloop).ToArray())
            {
                if ((unit.Gameloop - lastLoop) > 400 && spawnUnits.Count != 0)
                {
                    var gameloop = spawnUnits.Last().Gameloop;
                    var nextStat = playerStats.FirstOrDefault(f => f.Gameloop > gameloop);
                    if (nextStat == null)
                    {
                        break;
                    }

                    spawnStats.Add(GetSpawnStats(gameloop, nextStat, playerStats, spawnUnits, spawnStats.LastOrDefault()));
                    spawnUnits = new List<DsUnit>();
                    lastLoop = unit.Gameloop;
                }
                spawnUnits.Add(unit);
            }

            if (spawnUnits.Count != 0)
            {
                var lastUnit = spawnUnits.Last();
                var nextStat = playerStats.FirstOrDefault(f => f.Gameloop > lastUnit.Gameloop);
                if (nextStat != null)
                {
                    spawnStats.Add(GetSpawnStats(lastUnit.Gameloop, nextStat, playerStats, spawnUnits, spawnStats.LastOrDefault()));
                }
            }

            var lastStat = replay.Duration == 0 ? playerStats.LastOrDefault() : playerStats.LastOrDefault(f => f.Gameloop < replay.Duration);
            if (lastStat != null)
            {
                player.SpawnStats = new()
                {
                    Income = playerStats.Where(x => x.Gameloop <= lastStat.Gameloop).Sum(s => s.MineralsCollectionRate),
                    ArmyValue = spawnStats.Sum(s => s.ArmyValue),
                    KilledValue = lastStat.MineralsKilledArmy,
                    UpgradesSpent = lastStat.MineralsUsedCurrentTechnology,
                };
            }
        }
    }

    private static PlayerSpawnStats GetSpawnStats(int gameloop,
                                                  PlayerStats statsEvent,
                                                  List<PlayerStats> playerStatEvents,
                                                  List<DsUnit> units,
                                                  PlayerSpawnStats? lastStat)
    {
        var armyValue = statsEvent.MineralsUsedActiveForces / 2;


        return new()
        {
            Income = playerStatEvents.Where(x => x.Gameloop <= gameloop).Sum(s => s.MineralsCollectionRate),
            ArmyValue = armyValue,
            KilledValue = statsEvent.MineralsKilledArmy,
            UpgradesSpent = statsEvent.MineralsUsedCurrentTechnology,
        };
    }
}
