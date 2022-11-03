using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;

public static partial class Parse
{
    public static void ParseTrackerEventsNg(DsReplay replay, TrackerEvents trackerevents)
    {
        var zeroBornEvents = trackerevents.SUnitBornEvents.Where(x => x.Gameloop == 0).ToList();

        FixPlayerPos(replay, trackerevents.SPlayerSetupEvents);

        SetReplayLayout(replay, zeroBornEvents);

        SetGamePos(replay);

        SetPlayerUnits(replay, trackerevents.SUnitBornEvents.ToList());

        SetRefineries(replay,
            trackerevents.SUnitTypeChangeEvents.ToList(),
            trackerevents.SUnitBornEvents
            .Where(x => x.Gameloop == 0
                && x.UnitTypeName.StartsWith("MineralField"))
            .ToList());

        SetStats(replay, trackerevents.SPlayerStatsEvents.ToList());



        SetMiddle(replay, trackerevents.SUnitOwnerChangeEvents);

        SetUpgradesNG(replay, trackerevents.SUpgradeEvents.ToList());

        //foreach (var player in replay.Players)
        //{
        //    Console.WriteLine(player);
        //}

        //foreach (var unit in replay.Players[5].Units.Where(x => x.UnitType == UnitType.Spawn))
        //{
        //    Console.WriteLine($"{unit.Gameloop}: {unit.Name}");
        //}

        //foreach (var player in replay.Players)
        //{
        //    Console.WriteLine($"{player.GamePos} {player.Race} {player.Duration} {player.Army} {player.Kills}");
        //    foreach (var stat in player.SpawnStats)
        //    {
        //        Console.WriteLine(stat);
        //    }
        //}

        //Console.WriteLine($"{replay.GameTime} {replay.Duration} {replay.GameMode}");
        //foreach (var player in replay.Players)
        //{
        //    Console.WriteLine($"{player.GamePos} {player.Race} {player.Duration} {player.Army} {player.Kills}");
        //}

        //foreach (var upgrade in replay.Players[1].Upgrades)
        //{
        //    Console.WriteLine(upgrade);
        //}
    }

    private static void FixPlayerPos(DsReplay replay, ICollection<SPlayerSetupEvent> setupEvents)
    {
        var playerIds = setupEvents.Select(x => x.PlayerId).OrderBy(o => o).ToList();
        var playerPos = replay.Players.Select(s => s.Pos).OrderBy(o => o).ToList();

        if (!playerIds.SequenceEqual(playerPos))
        {
            if (replay.Players.Any() && (replay.Players.First().Pos != replay.Players.First().WorkingsetSlot))
            {
                foreach (var player in replay.Players)
                {
                    var setupEvent = setupEvents.FirstOrDefault(f => f.PlayerId == player.WorkingsetSlot);
                    if (setupEvent == null)
                    {
                        throw new ArgumentNullException(nameof(setupEvent));
                    }
                    player.Pos = setupEvent.PlayerId;
                }
            }
            else
            {
                foreach (var player in replay.Players)
                {
                    var setupEvent = setupEvents.FirstOrDefault(f => f.UserId == player.Pos - 1);
                    if (setupEvent == null)
                    {
                        throw new ArgumentNullException(nameof(setupEvent));
                    }
                    player.Pos = setupEvent.PlayerId;
                }
            }
        }
    }
}


