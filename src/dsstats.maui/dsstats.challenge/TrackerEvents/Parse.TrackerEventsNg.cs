using s2protocol.NET.Models;

namespace dsstats.challenge;

public static partial class Parse
{
    public static void ParseTrackerEventsNg(DsReplay replay, TrackerEvents trackerevents)
    {
        var zeroBornEvents = trackerevents.SUnitBornEvents.Where(x => x.Gameloop == 0).ToList();

        SetReplayLayout(replay, zeroBornEvents);

        SetPlayerUnits(replay, trackerevents.SUnitBornEvents.ToList());

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
}


