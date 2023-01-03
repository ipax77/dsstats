using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;

public static partial class Parse
{
    public static void ParseTrackerEventsNg(DsReplay replay, TrackerEvents trackerevents)
    {
        var zeroBornEvents = trackerevents.SUnitBornEvents.Where(x => x.Gameloop == 0).ToList();

        // FixPlayerPos(replay, trackerevents.SPlayerSetupEvents);
        FixPlayerPosNg(replay, trackerevents.SPlayerSetupEvents);

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
    }

    private static void SetMiddle(DsReplay replay, ICollection<SUnitOwnerChangeEvent> sUnitOwnerChangeEvents)
    {
        foreach (var changeEvent in sUnitOwnerChangeEvents.Where(x => x.UnitTagIndex == 20))
        {
            int team = 0;
            if (changeEvent.UpkeepPlayerId == 13)
                team = 1;
            else if (changeEvent.UpkeepPlayerId == 14)
                team = 2;
            replay.Middles.Add(new DsMiddle()
            {
                Gameloop = changeEvent.Gameloop,
                Team = team,
            });
        }
    }

    private static void FixPlayerPos(DsReplay replay, ICollection<SPlayerSetupEvent> setupEvents)
    {
        var playerIds = setupEvents.Select(x => x.PlayerId).OrderBy(o => o).ToList();
        var playerPos = replay.Players.Select(s => s.Pos).OrderBy(o => o).ToList();

        if (!playerIds.SequenceEqual(playerPos))
        {
            if (replay.Players.Any(a => a.Pos != a.WorkingsetSlot))
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


