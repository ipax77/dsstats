
using System.Security.Cryptography;
using dsstats.shared;
using dsstats.shared.DsFen;
using s2protocol.NET;

namespace dsstats.challenge.Services;

public partial class ChallengeService
{
    public static ChallengeResponse GetChallengeResponse(Sc2Replay sc2Replay)
    {
        try
        {
            return GetData(sc2Replay);
        }
        catch (Exception ex)
        {
            return new()
            {
                Error = ex.Message
            };
        }
    }
    private static ChallengeResponse GetData(Sc2Replay sc2Replay)
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
        var p2 = dsReplay.Players.FirstOrDefault(f => f.GamePos == 4);
        ArgumentNullException.ThrowIfNull(p2, "player 2 not found.");

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
        var challengePlayer = replayDto.ReplayPlayers.FirstOrDefault(f => f.GamePos == 4);
        ArgumentNullException.ThrowIfNull(replayPlayer);
        ArgumentNullException.ThrowIfNull(challengePlayer);
        var victory = replayPlayer.Upgrades.FirstOrDefault(f => f.Upgrade.Name.Equals("PlayerStateVictory", StringComparison.OrdinalIgnoreCase));

        ArgumentNullException.ThrowIfNull(victory, "No victory found.");
        var timeTillVictory = Convert.ToInt32((victory.Gameloop - firstSpawn) / 22.4);

        var challengeFen = DsFen.GetFen(new()
        {
            Commander = challengePlayer.Race,
            Team = 2,
            Upgrades = challengePlayer.Upgrades.ToList(),
            Spawn = challengePlayer.Spawns.First(),
        });
        var playerFen = DsFen.GetFen(new()
        {
            Commander = replayPlayer.Race,
            Team = 1,
            Upgrades = replayPlayer.Upgrades.ToList(),
            Spawn = replayPlayer.Spawns.First(),
        });
        return new()
        {
            Commander = replayPlayer.Race,
            TimeTillVictory = timeTillVictory,
            ChallengeFen = challengeFen,
            PlayerFen = playerFen,
            RequestName = new RequestNames(replayPlayer.Name, replayPlayer.Player.ToonId, replayPlayer.Player.RegionId, replayPlayer.Player.RealmId)
        };
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