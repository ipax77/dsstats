using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dsstats.db8services;

public partial class BuildService
{
    public async Task<Dictionary<BuildType, Dictionary<BuildType, WinLos>>> BuildDetailsTest()
    {
        int skip = 0;
        int take = 1000;

        var winLossStats = new Dictionary<BuildType, Dictionary<BuildType, WinLos>>();
        var replays = await GetReplays(skip, take);

        while (replays.Count > 0)
        {
            CalculateBuildTypeStats(replays, winLossStats);
            skip += take;
            replays = await GetReplays(skip, take);
        }

        // Calculate win rate for each BuildType against each other BuildType
        var winRateStats = winLossStats.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDictionary(
                innerKvp => innerKvp.Key,
                innerKvp => new
                {
                    Wins = innerKvp.Value.Wins,
                    Losses = innerKvp.Value.Losses,
                    WinRate = innerKvp.Value.Wins + innerKvp.Value.Losses > 0 ?
                        (double)innerKvp.Value.Wins / (innerKvp.Value.Wins + innerKvp.Value.Losses) * 100 : 0
                }
            )
        );

        var jsonWinRateStats = JsonSerializer.Serialize(winLossStats, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText("/data/ds/winrate_stats.json", jsonWinRateStats);

        return winLossStats;
    }

    private void CalculateBuildTypeStats(List<Replay> replays, Dictionary<BuildType, Dictionary<BuildType, WinLos>> winLossStats)
    {
        foreach (var replay in replays)
        {
            foreach (var replayPlayer in replay.ReplayPlayers.Where(x => x.GamePos <= 3).OrderBy(o => o.GamePos))
            {
                var oppPlayer = replayPlayer.GamePos switch
                {
                    1 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 4),
                    2 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 5),
                    3 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 6),
                    _ => null
                };
                if (oppPlayer == null)
                {
                    continue;
                }

                var build = IdentifyBuild(replayPlayer);
                var oppBuild = IdentifyBuild(oppPlayer);

                // Initialize nested dictionaries if not already initialized
                if (!winLossStats.ContainsKey(build.BuildType))
                {
                    winLossStats[build.BuildType] = new Dictionary<BuildType, WinLos>();
                }
                if (!winLossStats[build.BuildType].ContainsKey(oppBuild.BuildType))
                {
                    winLossStats[build.BuildType][oppBuild.BuildType] = new(0, 0);
                }
                if (!winLossStats.ContainsKey(oppBuild.BuildType))
                {
                    winLossStats[oppBuild.BuildType] = new Dictionary<BuildType, WinLos>();
                }
                if (!winLossStats[oppBuild.BuildType].ContainsKey(build.BuildType))
                {
                    winLossStats[oppBuild.BuildType][build.BuildType] = new(0, 0);
                }

                // Update win/loss stats based on match result
                if (replayPlayer.PlayerResult == PlayerResult.Win)
                {
                    // replayPlayer's BuildType won against oppPlayer's BuildType
                    winLossStats[build.BuildType][oppBuild.BuildType] = new(
                        winLossStats[build.BuildType][oppBuild.BuildType].Wins + 1,
                        winLossStats[build.BuildType][oppBuild.BuildType].Losses
                    );

                    // oppPlayer's BuildType lost to replayPlayer's BuildType
                    winLossStats[oppBuild.BuildType][build.BuildType] = new(
                        winLossStats[oppBuild.BuildType][build.BuildType].Wins,
                        winLossStats[oppBuild.BuildType][build.BuildType].Losses + 1
                    );
                }
                else
                {
                    // oppPlayer's BuildType won against replayPlayer's BuildType
                    winLossStats[oppBuild.BuildType][build.BuildType] = new(
                        winLossStats[oppBuild.BuildType][build.BuildType].Wins + 1,
                        winLossStats[oppBuild.BuildType][build.BuildType].Losses
                    );

                    // replayPlayer's BuildType lost to oppPlayer's BuildType
                    winLossStats[build.BuildType][oppBuild.BuildType] = new(
                        winLossStats[build.BuildType][oppBuild.BuildType].Wins,
                        winLossStats[build.BuildType][oppBuild.BuildType].Losses + 1
                    );
                }
            }
        }
    }

    private async Task<List<Replay>> GetReplays(int skip, int take)
    {
        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
                        .ThenInclude(i => i.Unit)
            .OrderByDescending(o => o.GameTime)
            .Where(x => x.GameMode == GameMode.Standard
                && x.TournamentEdition
                && x.Duration > 300
                && x.Maxleaver < 90
                && x.Playercount == 6
                && x.GameTime > new DateTime(2024, 1, 1))
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }


    public static BuildDetails IdentifyBuild(ReplayPlayer replayPlayer)
    {
        var tierTiming = GetTierTiming(replayPlayer.TierUpgrades);
        var gasTiming = GetGasTiming(replayPlayer.Refineries);
        var orderedUnits = GetOrderedUnits(replayPlayer.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.Min5));
        var buildType = replayPlayer.Race switch
        {
            Commander.Terran => IdentifyTerranOpener(orderedUnits),
            Commander.Protoss => IdentifyProtossOpener(orderedUnits),
            Commander.Zerg => IdentifyZergOpener(orderedUnits),
            _ => BuildType.None
        };
        //if (orderedUnits.Count > 0 && buildType == BuildType.None)
        //{
        //    Console.WriteLine("indahouse");
        //}
        return new(tierTiming, gasTiming, buildType);
    }

    private static BuildType IdentifyZergOpener(List<BuildUnit> orderedUnits)
    {
        if (orderedUnits.Count == 0)
        {
            return BuildType.None;
        }

        var unit = orderedUnits[0];

        if (unit.Name == "Zergling" && unit.Count >= 50)
        {
            return BuildType.Zerg_Zerglings;
        }

        if (unit.Name == "Zergling" && orderedUnits.Any(a => a.Name == "Baneling"))
        {
            return BuildType.Zerg_LingBanes;
        }

        if (unit.Name == "Zergling" && orderedUnits.Any(a => a.Name == "Hydralisk" && a.Count > 6))
        {
            return BuildType.Zerg_Hydras;
        }

        if (unit.Name == "Zergling" && orderedUnits.Any(a => a.Name == "Roach" && a.Count >= 5))
        {
            return BuildType.Zerg_LingRoach;
        }

        if (unit.Name == "Roach" && orderedUnits.Any(u => u.Name == "Queen"))
        {
            return BuildType.Zerg_RoachQueen;
        }

        if (unit.Name == "Hydralisk")
        {
            return BuildType.Zerg_Hydras;
        }

        if (unit.Name == "SwarmHost")
        {
            return BuildType.Zerg_Swarmhosts;
        }

        if (unit.Name == "Ravager" || (unit.Name == "Roach" && (orderedUnits.Count > 1 && orderedUnits[1].Name == "Ravager")))
        {
            return BuildType.Zerg_Ravagers;
        }

        if (unit.Name == "Queen" && orderedUnits.Any(u => u.Name == "Lurker"))
        {
            return BuildType.Zerg_QueenLurker;
        }

        if (unit.Name == "Mutalisk")
        {
            return BuildType.Zerg_Mutalisk;
        }

        if (unit.Name == "Ultralisk")
        {
            return BuildType.Zerg_Ultras;
        }

        return BuildType.None;
    }


    private static BuildType IdentifyProtossOpener(List<BuildUnit> orderedUnits)
    {
        if (orderedUnits.Count == 0)
        {
            return BuildType.None;
        }

        var unit = orderedUnits[0];

        if (unit.Name == "Stalker" && unit.Count >= 10)
        {
            return BuildType.Protoss_Stalker;
        }

        if (unit.Name == "Zealot")
        {
            return BuildType.Protoss_Zealots;
        }

        if ((unit.Name == "Adept" || unit.Name == "Stalker")
            && orderedUnits.Count > 1 && (orderedUnits[1].Name == "Stalker" || orderedUnits[1].Name == "Adept"))
        {
            return BuildType.Protoss_AdeptStalker;
        }

        if ((unit.Name == "Zealot" || unit.Name == "Stalker")
            && orderedUnits.Count > 1 && (orderedUnits[1].Name == "Stalker" || orderedUnits[1].Name == "Zealot"))
        {
            return BuildType.Protoss_ZealotStalker;
        }

        if (unit.Name == "Immortal" || unit.Name == "Archon" || unit.Name == "DarkTemplar" || unit.Name == "Disruptor")
        {
            return BuildType.Protoss_Tier2;
        }

        if (unit.Name == "Carrier")
        {
            return BuildType.Protoss_Carriers;
        }

        return BuildType.None;
    }

    private static BuildType IdentifyTerranOpener(List<BuildUnit> orderedUnits)
    {
        if (orderedUnits.Count == 0)
        {
            return BuildType.None;
        }

        var unit = orderedUnits[0];

        if (unit.Name == "Battlecruiser")
        {
            return BuildType.Terran_Battlecruiser;
        }

        if (unit.Name == "Banshee")
        {
            return BuildType.Terran_Banshees;
        }

        if (unit.Name == "Liberator")
        {
            return BuildType.Terran_Liberators;
        }

        if (unit.Name == "Raven")
        {
            return BuildType.Terran_Ravens;
        }

        if (unit.Name == "Tank" || unit.Name == "Hellbat" || unit.Name == "Cyclone" || unit.Name == "Hellion")
        {
            return BuildType.Terran_Mech;
        }

        if (unit.Name == "Marine" && unit.Count >= 20)
        {
            return BuildType.Terran_Marines;
        }

        if (unit.Name == "Marauder" || unit.Name == "Reaper"
            && orderedUnits.Count > 1 && (orderedUnits[1].Name == "Marauder" || orderedUnits[1].Name == "Reaper"))
        {
            return BuildType.Terran_MarauderReaper;
        }

        if (unit.Name == "Marine" || unit.Name == "Marauder" || unit.Name == "Reaper")
        {
            return BuildType.Terran_Bio;
        }

        return BuildType.None;
    }

    private static List<BuildUnit> GetOrderedUnits(Spawn? spawn)
    {
        if (spawn is null)
        {
            return [];
        }

        List<BuildUnit> units = [];

        foreach (var spawnUnit in spawn.Units)
        {
            units.Add(new(spawnUnit.Unit.Name, spawnUnit.Count));
        }
        return units.OrderByDescending(o => o.Count).ToList();
    }

    private static TierTiming GetTierTiming(string tierUpgrades)
    {
        if (string.IsNullOrEmpty(tierUpgrades))
        {
            return TierTiming.None;
        }

        var tierUpgrdes = tierUpgrades.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (tierUpgrdes.Length == 0)
        {
            return TierTiming.Tier1;
        }

        var times = new List<TimeSpan>();
        for (int i = 0; i < tierUpgrdes.Length; i++)
        {
            if (int.TryParse(tierUpgrdes[i], out var tierTime))
            {
                var time = TimeSpan.FromSeconds(tierTime / 22.4);
                times.Add(time);

            }
        }

        if (times.Count == 2 && times[1] < TimeSpan.FromMinutes(7))
        {
            return TierTiming.Fast3;
        }

        if (times.Count > 0 && times[0] < TimeSpan.FromMinutes(2))
        {
            return TierTiming.Fast2;
        }

        return TierTiming.Tier1;
    }

    private static GasTiming GetGasTiming(string refineries)
    {
        if (string.IsNullOrEmpty(refineries))
        {
            return GasTiming.None;
        }

        var gasTimes = refineries.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var times = new List<TimeSpan>();

        for (int i = 0; i < gasTimes.Length; i++)
        {
            if (int.TryParse(gasTimes[i], out var gasTime))
            {
                times.Add(TimeSpan.FromSeconds(gasTime / 22.4));
            }
        }

        if (times.Count == 0)
        {
            return GasTiming.None;
        }

        if (times.Count > 1 && times[1] < TimeSpan.FromMinutes(5))
        {
            return GasTiming.TwoGasFirst;
        }

        if (times.Count > 0 && times[0] < TimeSpan.FromSeconds(30))
        {
            return GasTiming.GasFirst;
        }

        return GasTiming.None;
    }

}

public enum TierTiming
{
    None = 0,
    Tier1 = 1,
    Fast2 = 2,
    Fast3 = 3,
}

public enum GasTiming
{
    None = 0,
    GasFirst = 1,
    TwoGasFirst = 2,
}



public sealed record RaceBuild<TBuild>(TBuild BuildType)
    where TBuild : Enum;

public sealed record BuildUnit(string Name, int Count);

public sealed record BuildDetails(TierTiming TierTiming, GasTiming GasTiming, BuildType BuildType);