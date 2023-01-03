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

    public static (DateTime, DateTime) TimeperiodSelected(string period)
    {
        return period switch
        {
            //"This Month" => (DateTime.Today.AddDays(-(DateTime.Today.Day - 1)), DateTime.Today),
            // "Last Month" => (DateTime.Today.AddDays(-(DateTime.Today.Day - 1)).AddMonths(-1), DateTime.Today.AddDays(-(DateTime.Today.Day - 1)).AddDays(-1)),
            "This Month" => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Today),
            "Last Month" => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1), new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)),
            "This Year" => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today),
            "Last Year" => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), new DateTime(DateTime.Now.Year, 1, 1)),
            "Last Two Years" => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), DateTime.Today),
            "ALL" => (new DateTime(2018, 1, 1), DateTime.Today),
            "Patch 2.60" => (new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today),
            _ => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today)
        };
    }

    public static readonly string[] TimePeriods = new string[] { "This Month", "Last Month", "This Year", "Last Year", "Last Two Years", "Patch 2.60", "ALL" };

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

    public static bool IsMaui { get; set; }
    public static string SqliteConnectionString { get; set; } = string.Empty;
    public static string MysqlConnectionString { get; set; } = string.Empty;
}
