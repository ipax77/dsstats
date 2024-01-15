using dsstats.shared;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.db8.Extensions;

public static class ReplayExtensions
{
    public static void GenHashV2(this Replay replay, MD5 md5hash)
    {
        if (replay.ReplayPlayers.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                ArgumentNullException.ThrowIfNull(pl.Player);
            }
            sb.Append(pl.GamePos.ToString());
            sb.Append(pl.Race.ToString());
            sb.Append(Data.GetPlayerIdString(new(pl.Player.ToonId, pl.Player.RealmId, pl.Player.RegionId)));
        }
        sb.Append(replay.GameMode.ToString());
        sb.Append(replay.Playercount.ToString());
        sb.Append(replay.Minarmy.ToString() + '|');
        // sb.Append(replay.Minkillsum.ToString() + "|");
        // sb.Append(replay.Maxkillsum.ToString() + "|");
        sb.Append(replay.Bunker.ToString() + "|");
        sb.Append(replay.Cannon.ToString());

        replay.ReplayHash = shared.Extensions.ReplayExtensions.GetMd5Hash(md5hash, sb.ToString());
    }

    public static string GenHashV2(this Spawn spawn, ReplayPlayer replayPlayer)
    {
        ArgumentNullException.ThrowIfNull(replayPlayer.Player);

        StringBuilder sb = new();

        sb.Append(Data.GetPlayerIdString(new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId)));
        sb.Append('|' + replayPlayer.GamePos);
        sb.Append('|' + replayPlayer.Race);
        sb.Append(spawn.Gameloop);
        sb.Append(spawn.Income);
        sb.Append(spawn.GasCount);
        sb.Append(spawn.ArmyValue);
        sb.Append(spawn.KilledValue);
        sb.Append(spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.Units.Select(s => $"{s.Unit.Name}{s.Poss}")));

        using var md5Hash = MD5.Create();
        return shared.Extensions.ReplayExtensions.GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GenHashV3(this Spawn spawn, Replay replay, ReplayPlayer replayPlayer, MD5 md5Hash)
    {
        StringBuilder sb = new();

        sb.Append(string.Join('|', replay.ReplayPlayers
            .OrderBy(o => o.GamePos)
            .Select(s => Data.GetPlayerIdString(new(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId)))));
        sb.Append('|' + Data.GetPlayerIdString(new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId)));
        sb.Append('|' + replayPlayer.Race);
        sb.Append('|' + replayPlayer.GamePos);
        sb.Append(replay.CommandersTeam1);
        sb.Append(replay.CommandersTeam2);
        sb.Append(spawn.Gameloop);
        sb.Append('|' + spawn.Income);
        sb.Append('|' + spawn.GasCount);
        sb.Append('|' + spawn.ArmyValue);
        sb.Append('|' + spawn.KilledValue);
        sb.Append('|' + spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.Units.Select(s => $"{s.Unit.Name}{s.Poss}")));

        return shared.Extensions.ReplayExtensions.GetMd5Hash(md5Hash, sb.ToString());
    }
}
