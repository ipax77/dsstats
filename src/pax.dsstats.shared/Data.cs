using System.Security.Cryptography;
using System.Text;

namespace pax.dsstats.shared;

public static class Data
{
    public static Commander GetCommander(string race)
    {
        return race switch
        {
            "Terran" => Commander.Terran,
            "Protoss" => Commander.Protoss,
            "Zerg" => Commander.Zerg,
            "Abathur" => Commander.Abathur,
            "Alarak" => Commander.Alarak,
            "Artanis" => Commander.Artanis,
            "Dehaka" => Commander.Dehaka,
            "Fenix" => Commander.Fenix,
            "Horner" => Commander.Horner,
            "Karax" => Commander.Karax,
            "Kerrigan" => Commander.Kerrigan,
            "Mengsk" => Commander.Mengsk,
            "Nova" => Commander.Nova,
            "Raynor" => Commander.Raynor,
            "Stetmann" => Commander.Stetmann,
            "Stukov" => Commander.Stukov,
            "Swann" => Commander.Swann,
            "Tychus" => Commander.Tychus,
            "Vorazun" => Commander.Vorazun,
            "Zagara" => Commander.Zagara,
            "Zeratul" => Commander.Zeratul,
            _ => Commander.None
        };
    }

    public static GameMode GetGameMode(string gameMode)
    {
        return gameMode switch
        {
            "GameModeBrawlCommanders" => GameMode.BrawlCommanders,
            "GameModeBrawlStandard" => GameMode.BrawlStandard,
            "GameModeBrawl" => GameMode.BrawlStandard,
            "GameModeCommanders" => GameMode.Commanders,
            "GameModeCommandersHeroic" => GameMode.CommandersHeroic,
            "GameModeHeroicCommanders" => GameMode.CommandersHeroic,
            "GameModeGear" => GameMode.Gear,
            "GameModeSabotage" => GameMode.Sabotage,
            "GameModeStandard" => GameMode.Standard,
            "GameModeSwitch" => GameMode.Switch,
            "GameModeTutorial" => GameMode.Tutorial,
            _ => GameMode.None
        };
    }

    public static Dictionary<Commander, string> CmdrColor { get; } = new Dictionary<Commander, string>()
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
            {     Commander.Zerg, "#6b1c92"   }
        };

    public static string GetBackgroundColor(Commander cmdr, string transparency = "33")
    {
        return $"{CmdrColor[cmdr]}{transparency}";
    }

    public static List<Commander> GetCommanders(CmdrGet cmdrGet)
    {
        return cmdrGet switch
        {
            CmdrGet.All => Enum.GetValues(typeof(Commander)).Cast<Commander>().ToList(),
            CmdrGet.NoNone => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => x != Commander.None).ToList(),
            CmdrGet.NoStd => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => (int)x > 3).ToList(),
            CmdrGet.Std => Enum.GetValues(typeof(Commander)).Cast<Commander>().Where(x => x != Commander.None && (int)x <= 3).ToList(),
            _ => Enum.GetValues(typeof(Commander)).Cast<Commander>().ToList(),
        };
    }

    public enum CmdrGet
    {
        All = 0,
        NoNone = 1,
        NoStd = 2,
        Std = 3
    }

    public static Breakpoint GetBreakpoint(int gameloop)
    {
        // 5min: 6240, 6720, 7200
        // 10min: 12960, 13440, 13920
        // 15min: 19680, 20160, 20640

        return gameloop switch
        {
            > 20645 => Breakpoint.All,
            >= 19680 => Breakpoint.Min15,
            >= 13930 => Breakpoint.All,
            >= 12960 => Breakpoint.Min10,
            >= 7210 => Breakpoint.All,
            >= 6240 => Breakpoint.Min5,
            _ => Breakpoint.All,
        };
    }

    public static string GenHash(ReplayDto replay)
    {
        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
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
        StringBuilder sBuilder = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }
    public static List<RequestNames> GetDefaultRequestNames()
    {
        return new() {
                new() { Name = "PAX", ToonId = 226401, RegionId = 2 },
                new() { Name = "PAX", ToonId = 10188255, RegionId = 1 },
                new() { Name = "Feralan", ToonId = 8497675, RegionId = 1 },
                new() { Name = "Feralan", ToonId = 1488340, RegionId = 2 }
            };
    }

    public static string GetRegionString(int? regionId)
    {
        return regionId switch
        {
            1 => "Am",
            2 => "Eu",
            3 => "As",
            _ => ""
        };
    }

    public static string GetRatingTypeLongName(RatingType ratingType)
    {
        return ratingType switch
        {
            RatingType.Cmdr => "Commanders 3v3",
            RatingType.Std => "Standard 3v3",
            RatingType.CmdrTE => "Cmdrs 3v3 TE",
            RatingType.StdTE => "Std 3v3 TE",
            _ => ""
        };
    }

    public static List<TimePeriod> GetTimePeriods(TimePeriodGet timePeriodGet = TimePeriodGet.None)
    {
        return timePeriodGet switch
        {
            TimePeriodGet.NoNone => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => x != TimePeriod.None).ToList(),
            TimePeriodGet.Builds => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => x != TimePeriod.None && (int)x < 6).ToList(),
            TimePeriodGet.PlayerDetails => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().Where(x => (int)x >= 6).ToList(),
            _ => Enum.GetValues(typeof(TimePeriod)).Cast<TimePeriod>().ToList(),
        };
    }

    public enum TimePeriodGet
    {
        None = 0,
        NoNone = 1,
        Builds = 2,
        PlayerDetails = 3
    }

    public static (DateTime, DateTime) TimeperiodSelected(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Past90Days => (DateTime.Today.AddDays(-90), DateTime.Today),
            TimePeriod.ThisMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Today),
            TimePeriod.LastMonth => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1), new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
            TimePeriod.ThisYear => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today),
            TimePeriod.LastYear => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), new DateTime(DateTime.Now.Year, 1, 1)),
            TimePeriod.Last2Years => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), DateTime.Today),
            TimePeriod.Patch2_60 => (new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today),
            TimePeriod.Patch2_71 => (new DateTime(2023, 01, 22), DateTime.Today),
            _ => (new DateTime(2018, 1, 1), DateTime.Today),
        };
    }

    public static string GetTimePeriodLongName(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Past90Days => "Past 90 Days",
            TimePeriod.ThisMonth => "This Month",
            TimePeriod.ThisYear => "This Year",
            TimePeriod.Last2Years => "Last Two Years",
            TimePeriod.Patch2_60 => "Patch 2.60",
            TimePeriod.LastMonth => "Last Month",
            TimePeriod.LastYear => "Last Year",
            TimePeriod.Patch2_71 => "Patch 2.71",
            _ => "All"
        };
    }

    public static TimePeriod GetTimePeriodFromDeprecatedString(string timePeriod)
    {
        return timePeriod switch
        {
            "This Month" => TimePeriod.ThisMonth,
            "Last Month" => TimePeriod.LastMonth,
            "This Year" => TimePeriod.ThisYear,
            "Last Year" => TimePeriod.LastYear,
            "Last Two Years" => TimePeriod.Last2Years,
            "Patch 2.60" => TimePeriod.Patch2_60,
            "Patch 2.71" => TimePeriod.Patch2_71,
            _ => TimePeriod.None
        };
    }
    public static readonly int MinBuildRating = 500;
    public static readonly int MaxBuildRating = 2500;

    public static bool IsMaui { get; set; }
    public static int MauiWidth { get; set; }
    public static int MauiHeight { get; set; }
    public static RequestNames? MauiRequestNames { get; set; }
    public static string SqliteConnectionString { get; set; } = string.Empty;
    public static string MysqlConnectionString { get; set; } = string.Empty;
}

public class LatestReplayEventArgs : EventArgs
{
    public ReplayDetailsDto? LatestReplay { get; init; }
}