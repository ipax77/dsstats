using pax.dsstats.shared;
using System.Globalization;

namespace sc2dsstats.razorlib.Services;
public static class HelperService
{
    public static string GetImageSrc(Commander commander)
    {
        return $"_content/sc2dsstats.razorlib/images/{commander.ToString().ToLower()}-min.png";
    }

    public static string GetBigNumberString(int num)
    {
        if (num > 1000000)
        {
            return (num / 1000000.0).ToString("N2", CultureInfo.InvariantCulture) + "m";
        }
        else if (num > 1000)
        {
            return (num / 1000.0).ToString("N2", CultureInfo.InvariantCulture) + "k";
        }
        else
        {
            return num.ToString();
        }
    }

    public static string GetPercentageString(int? wins, int? games)
    {
        if (games == null || wins == null || games == 0 || wins == 0)
        {
            return "0";
        }
        return $"{Math.Round(wins.Value * 100.0 / games.Value, 2).ToString(CultureInfo.InvariantCulture)}%";
    }

    public static string GetPlayerCountString(int playerCount)
    {
        return playerCount switch
        {
            1 => "1",
            2 => "1vs1",
            3 => "1vs2",
            4 => "2vs2",
            5 => "2vs3",
            6 => "3vs3",
            _ => ""
        };
    }

    public static string TimeFromGameloop(int gameloop)
    {
        var duration = gameloop / 22.4;
        return duration >= 3600 ?
              TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss")
            : TimeSpan.FromSeconds(duration).ToString(@"mm\:ss");
    }

    public static (int, int[], int) GetMiddleInfo(string middleString, int duration)
    {
        int totalGameloops = (int)(duration * 22.4);

        if (!String.IsNullOrEmpty(middleString))
        {
            var ents = middleString.Split('|').Where(x => !String.IsNullOrEmpty(x)).ToArray();
            var ients = ents.Select(s => int.Parse(s)).ToList();
            ients.Add(totalGameloops);
            int startTeam = ients[0];
            ients.RemoveAt(0);
            return (startTeam, ients.ToArray(), totalGameloops);
        }
        return (0, Array.Empty<int>(), totalGameloops);
    }

    public static (double, double) GetChartMiddle(int startTeam, int[] gameloops, int gameloop)
    {
        if (gameloops.Length < 2)
        {
            return (0, 0);
        }

        int sumTeam1 = 0;
        int sumTeam2 = 0;
        bool isFirstTeam = startTeam == 1;
        int lastLoop = 0;
        bool hasInfo = false;

        for (int i = 0; i < gameloops.Length; i++)
        {
            if (lastLoop > gameloop)
            {
                hasInfo = true;
                break;
            }

            isFirstTeam = !isFirstTeam;
            if (lastLoop > 0)
            {
                if (isFirstTeam)
                {
                    sumTeam1 += gameloops[i] - lastLoop;
                }
                else
                {
                    sumTeam2 += gameloops[i] - lastLoop;
                }
            }
            lastLoop = gameloops[i];
        }

        if (hasInfo)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= lastLoop - gameloop;
            }
            else
            {
                sumTeam2 -= lastLoop - gameloop;
            }
        }
        else if (gameloops.Length > 0)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= gameloops[^1] - gameloop;
            }
            else
            {
                sumTeam2 -= gameloops[^1] - gameloop;
            }
        }

        sumTeam1 = Math.Max(sumTeam1, 0);
        sumTeam2 = Math.Max(sumTeam2, 0);

        return (Math.Round(sumTeam1 * 100.0 / (double)gameloops[^1], 2), Math.Round(sumTeam2 * 100.0 / (double)gameloops[^1], 2));
    }
}

public record ReplaysToonIdRequest
{
    public string Name { get; init; } = "";
    public int ToonId { get; init; }
    public int ToonIdWith { get; init; }
    public int ToonIdVs { get; init; }
    public string? ToonIdName { get; init; }
}
