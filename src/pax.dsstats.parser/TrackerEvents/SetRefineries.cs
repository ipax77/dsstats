using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;

public static partial class Parse
{
    private static void SetRefineries(DsReplay replay, List<SUnitTypeChangeEvent> sUnitTypeChangeEvents, List<SUnitBornEvent> mineralBornEvents)
    {
        for (int i = 0; i < sUnitTypeChangeEvents.Count; i++)
        {
            var changeEvent = sUnitTypeChangeEvents[i];

            if (changeEvent.UnitTypeName.StartsWith("RefineryMinerals")
                || changeEvent.UnitTypeName.StartsWith("AssimilatorMinerals")
                || changeEvent.UnitTypeName.StartsWith("ExtractorMinerals"))
            {
                var refinery = mineralBornEvents.FirstOrDefault(f => f.UnitTagIndex == changeEvent.UnitTagIndex && f.UnitTagRecycle == changeEvent.UnitTagRecycle);
                if (refinery != null)
                {
                    var player = replay.Players.FirstOrDefault(f => f.Pos == refinery.ControlPlayerId);

                    if (player != null)
                    {
                        player.Refineries.Add(new() { Gameloop = changeEvent.Gameloop });
                    }
                }
            }
        }
    }

}