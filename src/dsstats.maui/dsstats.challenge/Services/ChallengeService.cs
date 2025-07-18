
using System.Security.Cryptography;
using dsstats.shared;
using s2protocol.NET;

namespace dsstats.challenge.Services;

public partial class ChallengeService
{
    public static void GetData(Sc2Replay sc2Replay)
    {
        var enemyMessage = sc2Replay.ChatMessages?
            .OrderBy(o => o.Gameloop)
            .LastOrDefault(f => f.Message.StartsWith("enemy ", StringComparison.OrdinalIgnoreCase));

        ArgumentNullException.ThrowIfNull(enemyMessage, "No enemy build.");

        DsReplay? dsReplay = Parse.GetDsReplay(sc2Replay);
        ArgumentNullException.ThrowIfNull(dsReplay, "failed parsing replay.");

        var cmdrString = enemyMessage.Message.Split(" ", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (Enum.TryParse(typeof(Commander), cmdrString, out var enemyCmdrObj)
            && enemyCmdrObj is Commander enemyCmdr)
        {
            var enemyPlayer = dsReplay.Players.First(f => f.Name == "Challenge");
            enemyPlayer.Race = enemyCmdr.ToString();
        }
        else
        {
            throw new InvalidOperationException("No enemy commander found.");
        }

        var p1 = dsReplay.Players.FirstOrDefault(f => f.GamePos == 1);
        ArgumentNullException.ThrowIfNull(p1, "player 1 not found.");

        var clearMessage = sc2Replay.ChatMessages?
            .LastOrDefault(f => f.Message.Equals("clear battle", StringComparison.OrdinalIgnoreCase))?.Gameloop ?? 0;

        int firstSpawn = 0;
        foreach (var player in dsReplay.Players)
        {
            player.Units = player.Units.Where(x => x.Gameloop >= clearMessage).ToList();
            player.SpawnStats = player.SpawnStats.Where(x => x.Gameloop >= clearMessage).ToList();
            firstSpawn = player.Units.FirstOrDefault()?.Gameloop ?? clearMessage;
        }

        var md5 = MD5.Create();
        var replayDto = Parse.GetReplayDto(dsReplay, md5, true);

        ArgumentNullException.ThrowIfNull(replayDto);
        if (replayDto.GameMode != GameMode.Tutorial)
        {
            throw new InvalidOperationException("No tutorial map.");
        }
        ValidateNoChanges(replayDto);

        var replayPlayer = replayDto.ReplayPlayers.FirstOrDefault(f => f.GamePos == 1);
        ArgumentNullException.ThrowIfNull(replayPlayer);
        var victory = replayPlayer.Upgrades.FirstOrDefault(f => f.Upgrade.Name.Equals("PlayerStateVictory", StringComparison.OrdinalIgnoreCase));

        ArgumentNullException.ThrowIfNull(victory, "No victory found.");
        var timeTillVictory = TimeSpan.FromSeconds((victory.Gameloop - firstSpawn) / 22.4);
        Console.WriteLine(timeTillVictory.ToString(@"hh\:mm\:ss"));
    }

    private static void ValidateNoChanges(ReplayDto replayDto)
    {
        foreach (var player in replayDto.ReplayPlayers)
        {
            int army = 0;
            int upgrades = 0;
            foreach (var spawn in player.Spawns)
            {
                if (army == 0)
                {
                    army = spawn.ArmyValue;
                }
                else
                {
                    if (army != spawn.ArmyValue)
                    {
                        throw new InvalidOperationException("Army changed.");
                    }
                }
                if (upgrades == 0)
                {
                    upgrades = spawn.UpgradeSpent;
                }
                else
                {
                    if (upgrades != spawn.UpgradeSpent)
                    {
                        throw new InvalidOperationException("Upgrades changed.");
                    }
                }
            }
        }
    }
}