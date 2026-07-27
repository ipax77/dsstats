using s2protocol.NET;
using s2protocol.NET.Models;

namespace Sc2DirectStrike.Parser;

public static partial class Sc2DirectStrikeParser
{
    private const int ScanMinimumDataBuild = 97425;
    private const int PaidScanAbilityLink = 1416;
    private const int RaynorScanAbilityLink = 1142;

    private static void SetGameEventData(
        Sc2Replay replay,
        DirectStrikePlayerContext[] playerContexts,
        DirectStrikeReplay directStrikeReplay)
    {
        GameEvents? gameEvents = replay.GameEvents;
        directStrikeReplay.ResumedFromReplay = GetResumedFromReplay(gameEvents);

        if (gameEvents is null)
        {
            return;
        }

        bool analyzeScans = IsScanDataBuildSupported(replay);
        if (analyzeScans)
        {
            foreach (DirectStrikePlayer player in directStrikeReplay.Players)
            {
                player.ScanCount = 0;
            }
        }

        Dictionary<int, DirectStrikePlayer> playersByUserId = GetPlayersByUserId(replay, directStrikeReplay);
        Dictionary<DirectStrikePlayer, DirectStrikePlayerContext> contextsByPlayer = new(playerContexts.Length);
        foreach (DirectStrikePlayerContext context in playerContexts)
        {
            contextsByPlayer.Add(context.Player, context);
        }

        foreach (GameEvent gameEvent in gameEvents.BaseGameEvents)
        {
            if (gameEvent is not SCmdEvent command
                || !playersByUserId.TryGetValue(command.UserId, out DirectStrikePlayer? player))
            {
                continue;
            }

            if (analyzeScans && IsScanCommand(player, command))
            {
                player.ScanCount++;
            }

            if (contextsByPlayer.TryGetValue(player, out DirectStrikePlayerContext? context))
            {
                TrackBuildUnitModificationCommand(context, command);
            }
        }
    }

    private static bool? GetResumedFromReplay(GameEvents? gameEvents)
    {
        if (gameEvents is null)
        {
            return null;
        }

        foreach (GameEvent gameEvent in gameEvents.BaseGameEvents)
        {
            if (gameEvent is SHijackReplayGameEvent or SGameUserJoinEvent { Hijack: true })
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsScanDataBuildSupported(Sc2Replay replay)
    {
        int dataBuild = GetReplayDataBuild(replay);
        return dataBuild >= ScanMinimumDataBuild;
    }

    private static bool IsScanCommand(DirectStrikePlayer player, SCmdEvent command)
    {
        return command is { AbilCmdIndex: 0, TargetX: not null, TargetY: not null }
            && (command.AbilLink == PaidScanAbilityLink
                || (player.Commander == Commander.Raynor && command.AbilLink == RaynorScanAbilityLink));
    }
}
