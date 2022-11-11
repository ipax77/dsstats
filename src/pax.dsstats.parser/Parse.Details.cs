using pax.dsstats.shared;
using s2protocol.NET.Models;

namespace pax.dsstats.parser;
public static partial class Parse
{
    private static void ParseDetails(DsReplay replay, Details details)
    {
        int failsafe_pos = 0;
        foreach (var player in details.Players)
        {
            if (player.Observe > 0)
            {
                continue;
            }

            failsafe_pos++;

            replay.Players.Add(new DsPlayer()
            {
                Name = player.Name,
                ToonId = player.Toon.Id,
                Clan = player.ClanName,
                Race = player.Race,
                Control = player.Control,
                Pos = failsafe_pos,
                WorkingsetSlot = player.WorkingSetSlotId > 0 ? player.WorkingSetSlotId : failsafe_pos,
            });
        }
        replay.GameTime = DateTime.FromFileTimeUtc(details.TimeUTC);
    }
}
