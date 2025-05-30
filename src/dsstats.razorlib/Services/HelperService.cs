﻿using dsstats.shared;
using System.Numerics;
using System.Text.Json.Serialization;

namespace dsstats.razorlib.Services;
public static class HelperService
{
    public static string GetImageSrc(Commander commander)
    {
        return $"_content/dsstats.razorlib/images/{commander.ToString().ToLower()}-min.png";
    }

    public static string GetBigNumberString(int num)
    {
        if (num > 1000000)
        {
            return (num / 1000000.0).ToString("N2") + "m";
        }
        else if (num > 1000)
        {
            return (num / 1000.0).ToString("N2") + "k";
        }
        else
        {
            return num.ToString();
        }
    }

    public static string GetBigNumberString(double num)
    {
        if (num > 1000000)
        {
            return (num / 1000000.0).ToString("N2") + "m";
        }
        else if (num > 1000)
        {
            return (num / 1000.0).ToString("N2") + "k";
        }
        else
        {
            return num.ToString("N2");
        }
    }

    public static string GetPercentageString(int? wins, int? games)
    {
        if (games == null || wins == null || games == 0 || wins == 0)
        {
            return "0";
        }
        return (wins.Value / (double)games.Value).ToString("P2");
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
        var duration = (int)(gameloop / 22.4);
        return duration >= 3600 ?
              TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss")
            : TimeSpan.FromSeconds(duration).ToString(@"mm\:ss");
    }

    public static string TimeFromSeconds(int seconds)
    {
        return seconds >= 3600 ?
              TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss")
            : TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
    }

    public static string TimeFromSecondsWithHour(int seconds)
    {
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }


    public static List<Commander> GetCommanders(string? cmdrString)
    {
        if (string.IsNullOrEmpty(cmdrString))
        {
            return new();
        }

        var intCmdrs = cmdrString.Split('|', StringSplitOptions.RemoveEmptyEntries);
        List<Commander> cmdrs = new();
        foreach (var intCmdr in intCmdrs)
        {
            if (int.TryParse(intCmdr, out var i))
            {
                cmdrs.Add((Commander)i);
            }
        }
        return cmdrs;
    }

    public static string SanitizePlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName) || playerName.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return "N/A";
        }

        char[] invalidChars = ['<', '>', '&', '"'];

        string sanitizedName = new(playerName.Select(c => invalidChars.Contains(c) ? ' ' : c).ToArray());

        return sanitizedName;
    }

    public static int GetGasIncome(ReplayPlayerDto replayPlayer)
    {
        if (string.IsNullOrEmpty(replayPlayer.Refineries))
        {
            return 0;
        }

        var refTimes = replayPlayer.Refineries.Split('|', StringSplitOptions.RemoveEmptyEntries);

        List<int> refineryValues = new();

        for (int i = 0; i < refTimes.Length; i++)
        {
            if (int.TryParse(refTimes[i], out int refTimeInt))
            {
                refineryValues.Add(GetRefValue(i, refTimeInt, replayPlayer.Duration));
            }
        }
        return refineryValues.Sum();
    }

    private static int GetRefValue(int i, int refTime, int duration)
    {
        int refCost = i switch
        {
            0 => 150,
            1 => 225,
            2 => 300,
            3 => 375,
            _ => 150
        };

        var refDuration = duration - Convert.ToInt32(refTime / 22.4);
        return Convert.ToInt32(refDuration * 0.5) - refCost;
    }

    public static (double, double) GetChartMiddle(MiddleInfo middleInfo, int atSecond)
    {
        if (middleInfo.MiddleChanges.Count < 1)
        {
            return (0, 0);
        }

        double sumTeam1 = 0;
        double sumTeam2 = 0;
        double lastSeconds = middleInfo.MiddleChanges[0];
        bool isFirstTeam = middleInfo.StartTeam == 1;
        isFirstTeam = !isFirstTeam;

        for (int i = 0; i < middleInfo.MiddleChanges.Count; i++)
        {
            var seconds = middleInfo.MiddleChanges[i];

            if (seconds > atSecond)
            {
                break;
            }

            if (isFirstTeam)
            {
                sumTeam1 += seconds - lastSeconds;
            }
            else
            {
                sumTeam2 += seconds - lastSeconds;
            }

            lastSeconds = seconds;
            isFirstTeam = !isFirstTeam;
        }

        if (isFirstTeam)
        {
            sumTeam1 += Math.Max(0, atSecond - lastSeconds);
        }
        else
        {
            sumTeam2 += Math.Max(0, atSecond - lastSeconds);
        }

        return (Math.Round(sumTeam1 * 100.0 / middleInfo.Duration, 2),
            Math.Round(sumTeam2 * 100.0 / middleInfo.Duration, 2));
    }

    public static string GetGameMode(ReplayListDto replay)
    {
        if (!replay.TournamentEdition)
        {
            return replay.GameMode.ToString();
        }
        else
        {
            if (replay.GameMode == GameMode.Commanders)
            {
                return "Cmdrs TE";
            }
            if (replay.GameMode == GameMode.Standard)
            {
                return "Std TE";
            }
            return $"{replay.GameMode} TE";
        }
    }

    public static string GetGameMode(GameMode gameMode, bool tournamentEdition)
    {
        if (!tournamentEdition)
        {
            return gameMode.ToString();
        }
        else
        {
            if (gameMode == GameMode.Commanders)
            {
                return "Cmdrs TE";
            }
            if (gameMode == GameMode.Standard)
            {
                return "Std TE";
            }
            return $"{gameMode} TE";
        }
    }

    public static string GetTinyGameMode(GameMode gameMode, bool tournamentEdition)
    {
        if (!tournamentEdition)
        {
            return gameMode switch
            {
                GameMode.Commanders => "Cmdrs",
                GameMode.CommandersHeroic => "Heroic",
                GameMode.Standard => "Std",
                _ => "Unrated"
            };
        }
        else
        {
            return gameMode switch
            {
                GameMode.Commanders => "Cmdrs TE",
                GameMode.Standard => "Std TE",
                _ => "Unrated"
            };
        }
    }

    public static string GetUnitColor(UnitColor unitColor)
    {
        return unitColor switch
        {
            UnitColor.Color1 => "#C0392B",
            UnitColor.Color2 => "#9B59B6",
            UnitColor.Color3 => "#2980B9",
            UnitColor.Color4 => "#1ABC9C",
            UnitColor.Color5 => "#F1C40F",
            UnitColor.Color6 => "#D35400",
            UnitColor.Color7 => "#ECF0F1",
            UnitColor.Color8 => "#7F8C8D",
            UnitColor.Color9 => "#943126",
            UnitColor.Color10 => "#5B2C6F",
            UnitColor.Color11 => "#21618C",
            UnitColor.Color12 => "#0E6655",
            UnitColor.Color13 => "#9C640C",
            UnitColor.Color14 => "#797D7F",
            UnitColor.Color15 => "#515A5A",
            _ => "#F9EBEA"
        };
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

public record Position
{
    public Position()
    {

    }

    public Position(int x, int y) : base()
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public static readonly Position Zero = new() { X = 0, Y = 0 };
    public static readonly Position Center1 = new() { X = 128, Y = 120 };
    public static readonly Position Center2 = new() { X = 120, Y = 120 };
    public static readonly Position Center3 = new() { X = 128, Y = 122 };

    [JsonIgnore]
    public Vector2 Vector2 => new Vector2(X, Y);
}