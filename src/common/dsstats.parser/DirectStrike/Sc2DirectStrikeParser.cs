using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    public static DirectStrikeReplay Parse(Sc2Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(replay.Details);

        if (!replay.Details.Title.StartsWith("Direct Strike", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("no direct strike replay.");
        }

        MetadataPlayer[] metadataPlayers = CopyToArray(replay.Metadata?.Players);
        Dictionary<int, MetadataPlayer> metadataPlayersById = new(metadataPlayers.Length);
        foreach (MetadataPlayer metadataPlayer in metadataPlayers)
        {
            metadataPlayersById.TryAdd(metadataPlayer.PlayerID, metadataPlayer);
        }

        DetailsPlayer[] detailsPlayers = CopyToArray(replay.Details.Players);
        DirectStrikePlayerContext[] playerContexts = new DirectStrikePlayerContext[detailsPlayers.Length];
        DirectStrikePlayer[] players = new DirectStrikePlayer[detailsPlayers.Length];
        for (int i = 0; i < detailsPlayers.Length; i++)
        {
            MetadataPlayer? metadataPlayer = GetMetadataPlayer(metadataPlayers, metadataPlayersById, i);
            DirectStrikePlayer player = ParseDetailsPlayer(detailsPlayers[i], metadataPlayer);
            playerContexts[i] = new(player, i, metadataPlayer?.PlayerID);
            players[i] = player;
        }

        DirectStrikeReplay directStrikeReplay = new()
        {
            BaseBuild = replay.Metadata?.BaseBuild ?? string.Empty,
            GameMode = GetGameMode(GetGameModeUpgradeNames(replay), detailsPlayers.Length),
            GameTime = replay.Details.DateTimeUTC,
            Observers = ParseObservers(replay),
            TE = replay.Details.Title.EndsWith("TE", StringComparison.OrdinalIgnoreCase),
            Players = Array.AsReadOnly(players),
        };

        SetTrackerData(replay, playerContexts, directStrikeReplay);
        SetGameEventData(replay, playerContexts, directStrikeReplay);
        FinalizeBuildUnitModifications(playerContexts);
        SetReplayDuration(directStrikeReplay);

        return directStrikeReplay;
    }

    private static void SetReplayDuration(DirectStrikeReplay replay)
    {
        if (replay.GameEndTime > TimeSpan.Zero)
        {
            replay.Duration = replay.GameEndTime;
            return;
        }

        TimeSpan duration = TimeSpan.Zero;
        foreach (DirectStrikePlayer player in replay.Players)
        {
            if (player.Duration > duration)
            {
                duration = player.Duration;
            }
        }

        replay.Duration = duration;
    }

    private static T[] CopyToArray<T>(ICollection<T>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        T[] array = new T[values.Count];
        values.CopyTo(array, 0);
        return array;
    }

}
