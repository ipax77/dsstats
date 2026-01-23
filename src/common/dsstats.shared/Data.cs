using System.Collections.Frozen;
using System.Text.Json;

namespace dsstats.shared;

public static class Data
{
    public static string GetToonIdString(ToonIdDto toonId)
    {
        return $"{toonId.Id}x{toonId.Region}x{toonId.Realm}";
    }

    public static ToonIdDto? GetToonId(string? toonIdString)
    {
        if (string.IsNullOrEmpty(toonIdString))
        {
            return null;
        }
        var parts = toonIdString.Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return null;
        }
        return new ToonIdDto
        {
            Id = int.Parse(parts[0]),
            Region = int.Parse(parts[1]),
            Realm = int.Parse(parts[2])
        };
    }

    private static readonly JsonSerializerOptions jsonEncodingOptions = new() { PropertyNameCaseInsensitive = true };

    public static List<PlayerDto> DecodePlayersFromBase64(string base64)
    {
        try
        {
            byte[] data = Convert.FromBase64String(base64);
            string json = System.Text.Encoding.UTF8.GetString(data);

            return JsonSerializer.Deserialize<List<PlayerDto>>(json, jsonEncodingOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string EncodePlayersToBase64(IEnumerable<PlayerDto> players)
    {
        string json = JsonSerializer.Serialize(players);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public static string GetRegionString(int regionId)
    {
        return regionId switch
        {
            1 => "NA",
            2 => "EU",
            3 => "AS",
            4 => "CH",
            _ => string.Empty
        };
    }

    public static string GetNumberString(double? num)
    {
        if (num == null)
        {
            return string.Empty;
        }
        if (num > 1000000)
        {
            return (num.Value / 1000000.0).ToString("N2") + "m";
        }
        else if (num > 1000)
        {
            return (num.Value / 1000.0).ToString("N2") + "k";
        }
        else
        {
            return num.Value.ToString();
        }
    }

    public static PercentageInfo GetPercentageInfo(double? percent)
    {
        if (!percent.HasValue)
        {
            return new(string.Empty, string.Empty);
        }
        var colorClass = percent.Value <= 0 ? "text-danger" : "text-success";
        var number = percent.Value.ToString("P1");
        return new(number, colorClass);
    }

    public static TimePeriodInfo GetTimePeriodInfo(TimePeriod timePeriod)
    {
        var now = DateTime.UtcNow;
        DateTime start, end;
        string name;
        bool hasEnd = true;

        switch (timePeriod)
        {
            case TimePeriod.Last90Days:
                end = now.Date;
                start = end.AddDays(-89);
                name = "Last 90 Days";
                hasEnd = false; // don't need to filter by End
                break;

            case TimePeriod.Previous90Days:
                end = now.Date.AddDays(-90);
                start = end.AddDays(-89);
                name = "Previous 90 Days";
                break;

            case TimePeriod.Last12Months:
                end = now.Date;
                start = end.AddMonths(-12).AddDays(1);
                name = "Last 12 Months";
                hasEnd = false;
                break;

            case TimePeriod.Previous12Months:
                end = now.Date.AddMonths(-12);
                start = end.AddMonths(-12).AddDays(1);
                name = "Previous 12 Months";
                break;

            case TimePeriod.ThisYear:
                start = new DateTime(now.Year, 1, 1);
                end = new DateTime(now.Year, 12, 31);
                name = "This Year";
                hasEnd = false;
                break;

            case TimePeriod.LastYear:
                start = new DateTime(now.Year - 1, 1, 1);
                end = new DateTime(now.Year - 1, 12, 31);
                name = "Last Year";
                break;

            case TimePeriod.AllTime:
                start = DateTime.MinValue;
                end = DateTime.MaxValue;
                name = "All Time";
                hasEnd = false;
                break;

            case TimePeriod.Custom:
                throw new InvalidOperationException("Custom requires explicit dates.");

            case TimePeriod.None:
            default:
                return null!;
        }

        return new TimePeriodInfo(start, end, name, hasEnd);
    }

    public static List<TimePeriod> GetBasicTimePeriods()
    {
        return [
                TimePeriod.Last90Days,
                TimePeriod.Previous90Days,
                TimePeriod.Last12Months,
                TimePeriod.Previous12Months,
                TimePeriod.ThisYear ,
                TimePeriod.LastYear ,
                TimePeriod.AllTime ,
        ];
    }

    public static List<Commander> GetStandardCommanders() => [Commander.Protoss, Commander.Terran, Commander.Zerg];
    public static List<Commander> GetCommanders()
    {
        return Enum.GetValues<Commander>().Where(x => (int)x > 3 && x != Commander.Zeratul).ToList();
    }

    public static FrozenDictionary<Commander, string> CmdrColor { get; } = new Dictionary<Commander, string>()
        {
            {     Commander.None, "#0000ff"        },
            {     Commander.Abathur, "#266a1b" },
            {     Commander.Alarak, "#ab0f0f" },
            {     Commander.Artanis, "#edae0c" },
            {     Commander.Dehaka, "#d52a38" },
            {     Commander.Fenix, "#fcf32c" },
            {     Commander.Horner, "#ba0d97" },
            {     Commander.Karax, "#1565c7" },
            {     Commander.Kerrigan, "#b021a1" },
            {     Commander.Mengsk, "#a46532" },
            {     Commander.Nova, "#f6f673" },
            {     Commander.Raynor, "#dd7336" },
            {     Commander.Stetmann, "#ebeae8" },
            {     Commander.Stukov, "#663b35" },
            {     Commander.Swann, "#ab4f21" },
            {     Commander.Tychus, "#342db5" },
            {     Commander.Vorazun, "#07c543" },
            {     Commander.Zagara, "#b01c48" },
            {     Commander.Zeratul, "#a1e7e7"  },
            {     Commander.Protoss, "#fcc828"   },
            {     Commander.Terran, "#4a4684"   },
            {     Commander.Zerg, "#6b1c92"   },
            {     Commander.Random, "#0000ff"   }
        }.ToFrozenDictionary();

    public static bool IsCommanderGameMode(GameMode gameMode)
    {
        return gameMode == GameMode.Commanders || gameMode == GameMode.CommandersHeroic || gameMode == GameMode.BrawlCommanders;
    }

    public static DateTime CanonicalizeGametime(DateTime value)
    {
        return new DateTime(
            (value.Ticks + TimeSpan.TicksPerSecond / 2)
            / TimeSpan.TicksPerSecond
            * TimeSpan.TicksPerSecond,
            DateTimeKind.Utc);
    }

    public const int MinBuildRating = 500;
    public const int MaxBuildRating = 3000;
    public const int MaxBuildPlayers = 6;

    public const int MinDuration = 300;
    public const int MaxDuration = 1200;
}


public sealed record PercentageInfo(string Number, string ColorClass);
public sealed record TimePeriodInfo(DateTime Start, DateTime End, string Name, bool HasEnd);