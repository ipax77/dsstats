using dsstats.shared;

namespace dsstats.parser;

internal static partial class DsstatsReplayMapper
{
    public static List<SpawnDto> CreateSpawns(DsPlayer player, DsstatsReplay replay)
    {
        if (player.Units.Count == 0)
        {
            return [];
        }

        if (player.Name == "oVANQUISHo")
        {
            Console.WriteLine("Debug");
        }

        // Group units into waves
        var waves = new List<List<DsUnit>>();
        var currentWave = new List<DsUnit>();
        int currentWaveStart = player.Units[0].Gameloop;

        foreach (var unit in player.Units)
        {
            if (unit.Gameloop - currentWaveStart > 20)
            {
                if (currentWave.Count > 0)
                {
                    waves.Add(currentWave);
                }
                currentWave = [];
                currentWaveStart = unit.Gameloop;
            }
            currentWave.Add(unit);
        }

        if (currentWave.Count > 0)
        {
            waves.Add(currentWave);
        }

        var spawns = new List<SpawnDto>();

        for (int i = 0; i < waves.Count; i++)
        {
            var wave = waves[i];
            bool isLastWave = i == waves.Count - 1;

            Breakpoint breakpoint = isLastWave
                ? Breakpoint.All
                : GetBreakpoint(wave.First().Gameloop);

            if (breakpoint == Breakpoint.None)
            {
                continue;
            }

            var spawn = CreateSpawnFromWave(wave, player, replay, breakpoint);
            if (spawn != null)
            {
                spawns.Add(spawn);
            }
        }

        return spawns;
    }

    private static SpawnDto? CreateSpawnFromWave(List<DsUnit> wave, DsPlayer player, DsstatsReplay replay, Breakpoint breakpoint)
    {
        if (wave.Count == 0)
            return null;

        var spawn = new SpawnDto() { Breakpoint = breakpoint };

        // Collect units
        foreach (var group in wave.GroupBy(u => u.Name))
        {
            var unitDto = new UnitDto
            {
                Name = group.Key,
                Count = group.Count(),
                Positions = group.SelectMany(u => new[] { u.Position.X, u.Position.Y }).ToList()
            };
            spawn.Units.Add(unitDto);
        }

        var lastGameloop = wave.Last().Gameloop;

        // Get first stat AFTER last unit gameloop
        var stat = breakpoint == Breakpoint.All ? player.Stats.LastOrDefault() 
            : player.Stats.FirstOrDefault(s => s.Gameloop >= lastGameloop);
        if (stat != null)
        {
            spawn.Income = GetIncome(replay, player, breakpoint);
            spawn.ArmyValue = stat.MineralsUsedActiveForces / 2;
            spawn.KilledValue = stat.MineralsKilledArmy;
            spawn.UpgradeSpent = stat.MineralsUsedCurrentTechnology;
            spawn.GasCount = player.Refineries.Where(x => x.Taken && x.Gameloop <= lastGameloop).Count();
        }

        return spawn;
    }

    private static Breakpoint GetBreakpoint(int gameloop)
    {
        return gameloop switch
        {
            _ when gameloop >= 6240 && gameloop <= 7209 => Breakpoint.Min5,
            _ when gameloop >= 12960 && gameloop <= 13928 => Breakpoint.Min10,
            _ when gameloop >= 19680 && gameloop <= 20649 => Breakpoint.Min15,
            _ => Breakpoint.None
        };
    }

    private static int GetIncome(DsstatsReplay replay, DsPlayer player, Breakpoint bp)
    {
        double baseIncome = 7.5;
        double baseGasIncome = 0.5;
        int[] refineryCosts = [150, 225, 300, 375, 500];

        int gameloop = bp switch
        {
            Breakpoint.Min5 => DsstatsParser.min5,
            Breakpoint.Min10 => DsstatsParser.min10,
            Breakpoint.Min15 => DsstatsParser.min15,
            _ => replay.Duration
        };

        double gasIncome = 0;
        double middleIncome = 0;
        double income = (gameloop / 22.4) * baseIncome;

        int i = 0;
        foreach (var refinery in player.Refineries)
        {
            if (refinery.Taken && refinery.Gameloop < gameloop)
            {
                gasIncome += ((gameloop - refinery.Gameloop) / 22.4) * baseGasIncome;
                gasIncome -= refineryCosts[i];
                i++;
            }
        }
        if (replay.MiddleIncome.TryGetValue(bp, out var teamMiddleIncome)
            && teamMiddleIncome is not null)
        {
            middleIncome = player.TeamId == 1 ? teamMiddleIncome.Team1 : teamMiddleIncome.Team2;
        }

        return (int)(gasIncome + middleIncome + income);
    }
}