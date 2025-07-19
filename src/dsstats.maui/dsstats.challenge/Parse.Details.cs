using dsstats.shared;
using s2protocol.NET.Models;

namespace dsstats.challenge;

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
                RegionId = player.Toon.Region,
                RealmId = player.Toon.Realm,
                Clan = player.ClanName,
                Race = player.Race,
                Control = player.Control,
                Pos = failsafe_pos,
                WorkingsetSlot = player.WorkingSetSlotId,
                GamePos = 1,
            });
        }
        replay.Players.Add(new()
        {
            Name = "Challenge",
            ToonId = 0,
            RegionId = 0,
            RealmId = 0,
            Race = "Zerg",
            Control = 0,
            Pos = 2,
            WorkingsetSlot = 2,
            GamePos = 4
        });

        replay.GameTime = DateTime.FromFileTimeUtc(details.TimeUTC);
    }


}
