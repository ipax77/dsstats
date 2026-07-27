using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private static Dictionary<int, DirectStrikePlayer> GetPlayersByUserId(
        Sc2Replay replay,
        DirectStrikeReplay directStrikeReplay)
    {
        Dictionary<int, DirectStrikePlayer> playersByUserId = [];
        Dictionary<(int Region, int Realm, int Id), DirectStrikePlayer> playersByToon =
            new(directStrikeReplay.Players.Count);
        Dictionary<int, DirectStrikePlayer> playersBySlotId =
            new(directStrikeReplay.Players.Count);

        foreach (DirectStrikePlayer player in directStrikeReplay.Players)
        {
            playersByToon.TryAdd((player.Region, player.Realm, player.Id), player);
            playersBySlotId.TryAdd(player.SlotId, player);
        }

        foreach (Slot slot in replay.Initdata?.LobbyState?.Slots ?? [])
        {
            if (slot.UserId is not { } userId)
            {
                continue;
            }

            if ((TryParseToonHandle(slot.ToonHandle, out int region, out int realm, out int id)
                    && playersByToon.TryGetValue((region, realm, id), out DirectStrikePlayer? player))
                || playersBySlotId.TryGetValue(slot.WorkingSetSlotId, out player))
            {
                playersByUserId.TryAdd(userId, player);
            }
        }

        return playersByUserId;
    }
}
