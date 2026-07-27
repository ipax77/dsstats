using System.Collections.ObjectModel;
using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private static ReadOnlyCollection<DirectStrikeObserver> ParseObservers(Sc2Replay replay)
    {
        UserInitialData[] userInitialData = CopyToArray(replay.Initdata?.UserInitialData);
        ICollection<Slot> slots = replay.Initdata?.LobbyState?.Slots ?? [];
        List<DirectStrikeObserver> observers = new(slots.Count);

        foreach (Slot slot in slots)
        {
            if (slot.Observe != 1 || slot.UserId is not { } userId || userId < 0 || userId >= userInitialData.Length)
            {
                continue;
            }

            UserInitialData user = userInitialData[userId];
            if (string.IsNullOrWhiteSpace(user.Name) || !TryParseToonHandle(slot.ToonHandle, out int region, out int realm, out int id))
            {
                continue;
            }

            observers.Add(new()
            {
                Clan = string.IsNullOrWhiteSpace(user.ClanTag) ? null : user.ClanTag,
                Id = id,
                Name = user.Name,
                Realm = realm,
                Region = region,
                SlotId = slot.WorkingSetSlotId,
            });
        }

        return observers.AsReadOnly();
    }

    private static DirectStrikePlayer ParseDetailsPlayer(DetailsPlayer player, MetadataPlayer? metadataPlayer)
    {
        PlayerResult result = metadataPlayer is null ? PlayerResult.None : ParsePlayerResult(metadataPlayer.Result);
        if (result == PlayerResult.None)
        {
            result = ParsePlayerResult(player.Result);
        }

        return new()
        {
            APM = metadataPlayer?.APM ?? 0,
            Name = player.Name,
            Clan = player.ClanName,
            Commander = ParseCommander(player.Race),
            Id = player.Toon.Id,
            Result = result,
            Region = player.Toon.Region,
            Realm = player.Toon.Realm,
            SelectedRace = ParseRace(metadataPlayer?.SelectedRace),
            SlotId = player.WorkingSetSlotId,
        };
    }

    private static MetadataPlayer? GetMetadataPlayer(MetadataPlayer[] metadataPlayers, Dictionary<int, MetadataPlayer> metadataPlayersById, int detailsPlayerIndex)
    {
        return metadataPlayersById.TryGetValue(detailsPlayerIndex + 1, out MetadataPlayer? playerById)
            ? playerById
            : detailsPlayerIndex < metadataPlayers.Length
            ? metadataPlayers[detailsPlayerIndex]
            : null;
    }

    private static Dictionary<int, DirectStrikePlayerContext> GetPlayerContextsByControlPlayerId(Sc2Replay replay, DirectStrikePlayerContext[] playerContexts)
    {
        Dictionary<int, DirectStrikePlayerContext> playerContextsByControlPlayerId = [];
        Slot[] slots = CopyToArray(replay.Initdata?.LobbyState?.Slots);

        Dictionary<(int Region, int Realm, int Id), DirectStrikePlayerContext> playersByToon = [];
        Dictionary<int, DirectStrikePlayerContext> playersByMetadataPlayerId = [];
        for (int i = 0; i < playerContexts.Length; i++)
        {
            DirectStrikePlayerContext context = playerContexts[i];
            playersByToon.TryAdd((context.Player.Region, context.Player.Realm, context.Player.Id), context);

            if (context.MetadataPlayerId is { } metadataPlayerId)
            {
                playersByMetadataPlayerId.TryAdd(metadataPlayerId, context);
            }
        }

        foreach (SPlayerSetupEvent setupEvent in replay.TrackerEvents?.SPlayerSetupEvents ?? [])
        {
            if ((TryGetPlayerContextByUserId(setupEvent.UserId, slots, playersByToon, out DirectStrikePlayerContext? context)
                    || TryGetPlayerContextBySlotId(setupEvent.SlotId, playerContexts, out context)
                    || TryGetPlayerContextByPlayerId(setupEvent.PlayerId, playerContexts, playersByMetadataPlayerId, out context))
                && context is not null)
            {
                playerContextsByControlPlayerId.TryAdd(setupEvent.PlayerId, context);
            }
        }

        return playerContextsByControlPlayerId;
    }

    private static bool TryGetPlayerContextByUserId(int? userId, Slot[] slots, Dictionary<(int Region, int Realm, int Id), DirectStrikePlayerContext> playersByToon, out DirectStrikePlayerContext? context)
    {
        context = null;
        if (userId is null)
        {
            return false;
        }

        foreach (Slot slot in slots)
        {
            if (slot.UserId == userId
                && TryParseToonHandle(slot.ToonHandle, out int region, out int realm, out int id)
                && playersByToon.TryGetValue((region, realm, id), out context))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPlayerContextBySlotId(int slotId, DirectStrikePlayerContext[] playerContexts, out DirectStrikePlayerContext? context)
    {
        if (slotId >= 0 && slotId < playerContexts.Length)
        {
            context = playerContexts[slotId];
            return true;
        }

        context = null;
        return false;
    }

    private static bool TryGetPlayerContextByPlayerId(int playerId, DirectStrikePlayerContext[] playerContexts, Dictionary<int, DirectStrikePlayerContext> playersByMetadataPlayerId, out DirectStrikePlayerContext? context)
    {
        if (playersByMetadataPlayerId.TryGetValue(playerId, out context))
        {
            return true;
        }

        int detailsIndex = playerId - 1;
        if (detailsIndex >= 0 && detailsIndex < playerContexts.Length)
        {
            context = playerContexts[detailsIndex];
            return true;
        }

        context = null;
        return false;
    }

    private static bool TryParseToonHandle(string? toonHandle, out int region, out int realm, out int id)
    {
        region = 0;
        realm = 0;
        id = 0;

        if (string.IsNullOrEmpty(toonHandle))
        {
            return false;
        }

        ReadOnlySpan<char> value = toonHandle.AsSpan();
        int firstSeparator = value.IndexOf('-');
        if (firstSeparator <= 0)
        {
            return false;
        }

        ReadOnlySpan<char> remaining = value[(firstSeparator + 1)..];
        int secondSeparator = remaining.IndexOf('-');
        if (secondSeparator <= 0)
        {
            return false;
        }

        ReadOnlySpan<char> type = remaining[..secondSeparator];
        remaining = remaining[(secondSeparator + 1)..];
        int thirdSeparator = remaining.IndexOf('-');
        return thirdSeparator > 0 && remaining[(thirdSeparator + 1)..].IndexOf('-') < 0 && type.SequenceEqual("S2")
            && int.TryParse(value[..firstSeparator], out region)
            && int.TryParse(remaining[..thirdSeparator], out realm)
            && int.TryParse(remaining[(thirdSeparator + 1)..], out id);
    }

    private static bool TryParseWorkerCommander(string unitTypeName, out Commander commander)
    {
        const string workerPrefix = "Worker";

        commander = Commander.None;
        if (!unitTypeName.StartsWith(workerPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> commanderName = unitTypeName.AsSpan(workerPrefix.Length);
        commander = commanderName switch
        {
            nameof(Commander.Protoss) => Commander.Protoss,
            nameof(Commander.Terran) => Commander.Terran,
            nameof(Commander.Zerg) => Commander.Zerg,
            nameof(Commander.Abathur) => Commander.Abathur,
            nameof(Commander.Alarak) => Commander.Alarak,
            nameof(Commander.Artanis) => Commander.Artanis,
            nameof(Commander.Dehaka) => Commander.Dehaka,
            nameof(Commander.Fenix) => Commander.Fenix,
            nameof(Commander.Horner) => Commander.Horner,
            nameof(Commander.Karax) => Commander.Karax,
            nameof(Commander.Kerrigan) => Commander.Kerrigan,
            nameof(Commander.Mengsk) => Commander.Mengsk,
            nameof(Commander.Nova) => Commander.Nova,
            nameof(Commander.Raynor) => Commander.Raynor,
            nameof(Commander.Stetmann) => Commander.Stetmann,
            nameof(Commander.Stukov) => Commander.Stukov,
            nameof(Commander.Swann) => Commander.Swann,
            nameof(Commander.Tychus) => Commander.Tychus,
            nameof(Commander.Vorazun) => Commander.Vorazun,
            nameof(Commander.Zagara) => Commander.Zagara,
            nameof(Commander.Zeratul) => Commander.Zeratul,
            nameof(Commander.Random) => Commander.Random,
            _ => Commander.None,
        };

        return commander != Commander.None;
    }

    private static Commander ParseCommander(string? commander)
    {
        return commander.AsSpan() switch
        {
            nameof(Commander.Protoss) => Commander.Protoss,
            nameof(Commander.Terran) => Commander.Terran,
            nameof(Commander.Zerg) => Commander.Zerg,
            nameof(Commander.Abathur) => Commander.Abathur,
            nameof(Commander.Alarak) => Commander.Alarak,
            nameof(Commander.Artanis) => Commander.Artanis,
            nameof(Commander.Dehaka) => Commander.Dehaka,
            nameof(Commander.Fenix) => Commander.Fenix,
            nameof(Commander.Horner) => Commander.Horner,
            nameof(Commander.Karax) => Commander.Karax,
            nameof(Commander.Kerrigan) => Commander.Kerrigan,
            nameof(Commander.Mengsk) => Commander.Mengsk,
            nameof(Commander.Nova) => Commander.Nova,
            nameof(Commander.Raynor) => Commander.Raynor,
            nameof(Commander.Stetmann) => Commander.Stetmann,
            nameof(Commander.Stukov) => Commander.Stukov,
            nameof(Commander.Swann) => Commander.Swann,
            nameof(Commander.Tychus) => Commander.Tychus,
            nameof(Commander.Vorazun) => Commander.Vorazun,
            nameof(Commander.Zagara) => Commander.Zagara,
            nameof(Commander.Zeratul) => Commander.Zeratul,
            nameof(Commander.Random) => Commander.Random,
            _ => Commander.None,
        };
    }

    private static Race ParseRace(string? race)
    {
        ReadOnlySpan<char> value = race.AsSpan().Trim();
        return value.Equals("Rand", StringComparison.OrdinalIgnoreCase) || value.Equals("Random", StringComparison.OrdinalIgnoreCase)
            ? Race.Random
            : value.Equals("Terr", StringComparison.OrdinalIgnoreCase) || value.Equals("Terran", StringComparison.OrdinalIgnoreCase)
            ? Race.Terran
            : value.Equals("Prot", StringComparison.OrdinalIgnoreCase) || value.Equals("Protoss", StringComparison.OrdinalIgnoreCase)
            ? Race.Protoss
            : value.Equals("Zerg", StringComparison.OrdinalIgnoreCase) ? Race.Zerg : Race.None;
    }

    private static PlayerResult ParsePlayerResult(string? result)
    {
        ReadOnlySpan<char> value = result.AsSpan().Trim();
        return value.Equals("Win", StringComparison.OrdinalIgnoreCase)
            ? PlayerResult.Win
            : value.Equals("Loss", StringComparison.OrdinalIgnoreCase)
            ? PlayerResult.Loss
            : value.Equals("Undecided", StringComparison.OrdinalIgnoreCase) ? PlayerResult.Undecided : PlayerResult.None;
    }

    private static PlayerResult ParsePlayerResult(int result)
    {
        return result switch
        {
            0 => PlayerResult.Undecided,
            1 => PlayerResult.Win,
            2 => PlayerResult.Loss,
            _ => PlayerResult.None,
        };
    }
}
