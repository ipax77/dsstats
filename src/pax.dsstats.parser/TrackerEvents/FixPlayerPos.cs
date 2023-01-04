using pax.dsstats.shared;
using s2protocol.NET.Models;
using System.Linq;

namespace pax.dsstats.parser;

public static partial class Parse
{
    private static void FixPlayerPosNg(DsReplay replay, ICollection<SPlayerSetupEvent> setupEvents)
    {
        var playerIds = setupEvents.Select(x => x.PlayerId).OrderBy(o => o).ToList();
        var playerPos = replay.Players.Select(s => s.Pos).OrderBy(o => o).ToList();

        if (!playerIds.SequenceEqual(playerPos))
        {
            // replay.Players.Pos === setupEvents.PlayerId

            // try workingsetSlotIds + 1
            var workingsetSlotIdsIncremented = replay.Players.Select(s => s.WorkingsetSlot + 1).OrderBy(o => o).ToList();
            if (playerIds.SequenceEqual(workingsetSlotIdsIncremented))
            {
                replay.Players.ForEach(f => f.Pos = f.WorkingsetSlot + 1);
                return;
            }

            // try workingsetSlotIds
            var workingsetSlotIds = replay.Players.Select(s => s.WorkingsetSlot).OrderBy(o => o).ToList();
            if (playerIds.SequenceEqual(workingsetSlotIds))
            {
                replay.Players.ForEach(f => f.Pos = f.WorkingsetSlot);
                return;
            }

            // try workingsetSlotIds with 0 + 1
            var workingsetSlotIdsWithZeroToOne = GetZeroToOneAdjustedWorkingsetSlotIds(replay.Players);
            if (playerIds.SequenceEqual(workingsetSlotIdsWithZeroToOne))
            {
                replay.Players.ForEach(f => f.Pos = f.WorkingsetSlot == 0 ? 1 : f.WorkingsetSlot);
                return;
            }
            
            throw new ArgumentNullException(nameof(setupEvents));
        }
    }

    private static List<int> GetZeroToOneAdjustedWorkingsetSlotIds(List<DsPlayer> players)
    {
        HashSet<int> ids = new();
        foreach (var player in players.OrderBy(o => o.WorkingsetSlot))
        {
            if (player.WorkingsetSlot == 0)
            {
                ids.Add(1);
            }
            else
            {
                ids.Add(player.WorkingsetSlot);
            }
        }
        return ids.ToList();
    }
}