
using System.Security.Cryptography;
using System.Text;

namespace pax.dsstats.dbng.Extensions;

public static class ReplayExtensions
{
    public static string GenHash(this Replay replay)
    {
        if (!replay.Players.Any())
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        StringBuilder sb = new();
        foreach (var pl in replay.Players.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(replay));
            }
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
        }
        sb.Append(replay.GameMode + replay.Playercount);
        sb.Append(replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum);

        // if (replay.WinnerTeam == 0)
        // {
        //     sb.Append(replay.Maxkillsum);
        // } else
        // {
        //     sb.Append(replay.Minkillsum);
        // }

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GetMd5Hash(MD5 md5Hash, string input)
    {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }
}