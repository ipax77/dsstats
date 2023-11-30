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
        sb.Append(replay.Minkillsum.ToString() + "|");
        sb.Append(replay.Maxkillsum.ToString() + "|");
        sb.Append(replay.Bunker.ToString() + "|");
        sb.Append(replay.Cannon.ToString());

        replay.ReplayHash = shared.Extensions.ReplayExtensions.GetMd5Hash(md5hash, sb.ToString());
    }
}
