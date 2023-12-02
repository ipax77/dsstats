
using System.Security.Cryptography;
using System.Text;

namespace dsstats.shared.Extensions;

public static class ReplayExtensions
{
    public static void SetDefaultFilter(this ReplayDto replay)
    {
        if (replay.Playercount == 6
            && replay.Duration >= 300
            && replay.Maxleaver < 90
            && replay.WinnerTeam > 0)
        {
            replay.DefaultFilter = true;
        }
    }

    public static void GenHash(this ReplayDto replay, MD5 md5hash)
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

        replay.ReplayHash = GetMd5Hash(md5hash, sb.ToString());
    }

    public static void GenHashV2(this ReplayDto replay, MD5 md5hash)
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
                throw new ArgumentOutOfRangeException(nameof(replay));
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

        replay.ReplayHash = GetMd5Hash(md5hash, sb.ToString());
    }

    public static string GenHash(this SpawnDto spawn, ReplayDto replay)
    {
        StringBuilder sb = new();

        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(replay));
            }
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId + '|' + pl.Player.RealmId + pl.Player.RegionId);
        }

        sb.Append(spawn.Gameloop);
        sb.Append(spawn.Income);
        sb.Append(spawn.GasCount);
        sb.Append(spawn.ArmyValue);
        sb.Append(spawn.KilledValue);
        sb.Append(spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.Units.Select(s => $"{s.Unit.Name}{s.Poss}")));

        using var md5Hash = MD5.Create();
        return GetMd5Hash(md5Hash, sb.ToString());
    }

    public static MiddleInfo GetMiddleInfo(this ReplayDto replayDto)
    {
        MiddleInfo middleInfo = new()
        {
            Duration = replayDto.Duration,
            Bunker = replayDto.Bunker,
            Cannon = replayDto.Cannon,
            WinnerTeam = replayDto.WinnerTeam
        };

        if (string.IsNullOrEmpty(replayDto.Middle) || replayDto.Duration == 0)
        {
            SetTeamIncome(middleInfo, replayDto.Duration);
            return new();
        }

        var middleEnts = replayDto.Middle.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (middleEnts.Length == 0)
        {
            SetTeamIncome(middleInfo, replayDto.Duration);
            return middleInfo;
        }

        middleInfo.StartTeam = int.Parse(middleEnts[0]);
        middleInfo.MiddleChanges = middleEnts.Skip(1).Select(s => Math.Round(int.Parse(s) / 22.4, 2)).ToList();

        SetTeamIncome(middleInfo, replayDto.Duration);
        return middleInfo;
    }

    private static void SetTeamIncome(MiddleInfo middleInfo, int totalSeconds)
    {
        double team1MiddleSeconds = 0;
        double team2MiddleSeconds = 0;

        if (middleInfo.MiddleChanges.Count > 0)
        {
            int crossingTeam = middleInfo.StartTeam;
            double crossingTime = middleInfo.MiddleChanges[0];

            for (int i = 1; i < middleInfo.MiddleChanges.Count; i++)
            {
                if (totalSeconds < crossingTime)
                {
                    break;
                }

                if (crossingTeam == 1)
                {
                    team1MiddleSeconds += middleInfo.MiddleChanges[i] - crossingTime;
                }
                else
                {
                    team2MiddleSeconds += middleInfo.MiddleChanges[i] - crossingTime;
                }

                crossingTime = middleInfo.MiddleChanges[i];
                crossingTeam = crossingTeam == 1 ? 2 : 1;
            }


            if (crossingTeam == 1)
            {
                team1MiddleSeconds += Math.Max(0, totalSeconds - crossingTime);
            }
            else
            {
                team2MiddleSeconds += Math.Max(0, totalSeconds - crossingTime);
            }
        }
        var baseIncome = totalSeconds * 7.5;
        var team1MiddleIncome = team1MiddleSeconds * 1.0;
        var team2MiddleIncome = team2MiddleSeconds * 1.0;

        middleInfo.Team1Income = Convert.ToInt32(baseIncome + team1MiddleIncome);
        middleInfo.Team2Income = Convert.ToInt32(baseIncome + team2MiddleIncome);

        middleInfo.Team1Percentage = middleInfo.Duration == 0 ? 0
        : Math.Round(team1MiddleSeconds * 100.0 / (double)middleInfo.Duration, 2);
        middleInfo.Team2Percentage = middleInfo.Duration == 0 ? 0
        : Math.Round(team2MiddleSeconds * 100.0 / (double)middleInfo.Duration, 2);
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
