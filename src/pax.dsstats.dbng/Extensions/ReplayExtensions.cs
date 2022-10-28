
using pax.dsstats.shared;
using System.Security.Cryptography;
using System.Text;

namespace pax.dsstats.dbng.Extensions;

public static class ReplayExtensions
{
    public static void SetDefaultFilter(this Replay replay)
    {
        if (replay.Playercount == 6
            && replay.Duration >= 300
            && replay.Maxleaver < 90
            && replay.WinnerTeam > 0)
        {
            replay.DefaultFilter = true;
        }
    }

    public static string GenHash(this Replay replay)
    {
        if (!replay.ReplayPlayers.Any())
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(replay));
            }
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
        }
        sb.Append(replay.GameMode + replay.Playercount);
        sb.Append(replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum);

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GenHash(this Spawn spawn)
    {
        StringBuilder sb = new();

        sb.Append(spawn.Gameloop);
        sb.Append(spawn.Income);
        sb.Append(spawn.GasCount);
        sb.Append(spawn.ArmyValue);
        sb.Append(spawn.KilledValue);
        sb.Append(spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.Units.Select(s => $"{s.UnitId}{s.Poss}")));

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static string GenMemKey(this StatsRequest statsRequest)
    {
        var sb = new StringBuilder();
        sb.Append("Stats");
        sb.Append(statsRequest.StartTime.ToString(@"yyyyMMdd"));
        sb.Append((int)statsRequest.Interest);
        sb.Append(statsRequest.EndTime.ToString(@"yyyyMMdd"));
        sb.Append(String.Concat(statsRequest.GameModes.Select(s => (int)s)));
        return sb.ToString();
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