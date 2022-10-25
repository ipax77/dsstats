using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;

public static partial class Parse
{
    public static void SetStats(DsReplay replay, List<SPlayerStatsEvent> statEvents)
    {
        foreach (var player in replay.Players)
        {
            var playerUnits = player.Units.Where(x => x.UnitType == UnitType.Spawn).OrderBy(x => x.Gameloop).ToList();
            var playerStats = statEvents.Where(x => x.PlayerId == player.Pos && x.MineralsCollectionRate > 0).ToList();

            if (!playerUnits.Any())
            {
                continue;
            }

            int lastLoop = playerUnits.First().Gameloop;

            List<DsUnit> spawnUnits = new List<DsUnit>();

            foreach (var unit in player.Units.Where(x => x.UnitType == UnitType.Spawn).OrderBy(o => o.Gameloop).ToArray())
            {
                if ((unit.Gameloop - lastLoop) > 400 && spawnUnits.Any())
                {
                    var gameloop = spawnUnits.Last().Gameloop;
                    var nextStat = playerStats.FirstOrDefault(f => f.Gameloop > gameloop);
                    if (nextStat == null)
                    {
                        break;
                    }

                    player.SpawnStats.Add(GetSpawnStats(gameloop, nextStat, playerStats, spawnUnits, player.Refineries, player.SpawnStats.LastOrDefault()));
                    spawnUnits = new List<DsUnit>();
                    lastLoop = unit.Gameloop;
                }
                spawnUnits.Add(unit);
            }

            if (spawnUnits.Any())
            {
                var lastUnit = spawnUnits.Last();
                var nextStat = playerStats.FirstOrDefault(f => f.Gameloop > lastUnit.Gameloop);
                if (nextStat != null)
                {
                    player.SpawnStats.Add(GetSpawnStats(lastUnit.Gameloop, nextStat, playerStats, spawnUnits, player.Refineries, player.SpawnStats.LastOrDefault()));
                }
            }

            var lastStat = replay.Duration == 0 ? playerStats.LastOrDefault() : playerStats.LastOrDefault(f => f.Gameloop < replay.Duration);
            if (lastStat != null)
            {
                player.Duration = lastStat.Gameloop;
                player.Income = playerStats.Where(x => x.Gameloop <= lastStat.Gameloop).Sum(s => s.MineralsCollectionRate);
                player.Army = player.SpawnStats.Sum(s => s.ArmyValue);
                player.Kills = lastStat.MineralsKilledArmy;
                player.UpgradesSpent = lastStat.MineralsUsedCurrentTechnology;
            }
        }
    }

    private static PlayerSpawnStats GetSpawnStats(int gameloop,
                                                  SPlayerStatsEvent statsEvent,
                                                  List<SPlayerStatsEvent> playerStatEvents,
                                                  List<DsUnit> units,
                                                  List<DsRefinery> refinieries,
                                                  PlayerSpawnStats? lastStat)
    {
        var armyValue = statsEvent.MineralsUsedActiveForces / 2;

        List<DsUnit> surwivers = new List<DsUnit>();

        if (lastStat != null)
        {
            surwivers = lastStat.Units.Where(x => x.DiedGameloop > gameloop).ToList();
            if (lastStat.Surwivers.Any())
            {
                surwivers.AddRange(lastStat.Surwivers.Where(x => x.DiedGameloop > gameloop));
            }
        }

        return new()
        {
            Gameloop = gameloop,
            Units = units.Where(x => x.UnitType == UnitType.Spawn).ToList(),
            Income = playerStatEvents.Where(x => x.Gameloop <= gameloop).Sum(s => s.MineralsCollectionRate),
            ArmyValue = armyValue,
            KilledValue = statsEvent.MineralsKilledArmy,
            UpgradesSpent = statsEvent.MineralsUsedCurrentTechnology,
            Surwivers = surwivers,
            GasCount = refinieries.Where(x => x.Gameloop <= gameloop).Count()
        };
    }
}