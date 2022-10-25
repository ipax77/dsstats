using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc2dsstats.shared
{
    public static class DSData
    {
        public static string[] Cmdrs = new string[] {
            "Abathur",
            "Alarak",
            "Artanis",
            "Dehaka",
            "Fenix",
            "Horner",
            "Karax",
            "Kerrigan",
            "Mengsk",
            "Nova",
            "Raynor",
            "Stetmann",
            "Stukov",
            "Swann",
            "Tychus",
            "Vorazun",
            "Zagara",
            "Zeratul",
        };

        public enum Commander
        {
            None = 0,
            Protoss = 1,
            Terran = 2,
            Zerg = 3,
            Abathur = 10,
            Alarak = 20,
            Artanis = 30,
            Dehaka = 40,
            Fenix = 50,
            Horner = 60,
            Karax = 70,
            Kerrigan = 80,
            Mengsk = 90,
            Nova = 100,
            Raynor = 110,
            Stetmann = 120,
            Stukov = 130,
            Swann = 140,
            Tychus = 150,
            Vorazun = 160,
            Zagara = 170,
            Zeratul = 180
        }

        public static List<Commander> GetCommanders => Enum.GetValues<Commander>().Where(x => (int)x > 3).ToList();
        public static List<Commander> GetStdCommanders => Enum.GetValues<Commander>().Where(x => (int)x > 0 && (int)x <= 3).ToList();

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

        public static string[] stds = new string[]
        {
            "Terran",
            "Protoss",
            "Zerg"
        };

        public static List<string> StdTeams()
        {
            List<string> teams = new List<string>() { "ALL" };
            for (int p1 = 1; p1 < 4; p1++)
            {
                for (int p2 = 1; p2 < 4; p2++)
                {
                    for (int p3 = 1; p3 < 4; p3++)
                    {
                        teams.Add(((Commander)p1).ToString() + ((Commander)p2).ToString() + ((Commander)p3).ToString());
                    }
                }
            }
            return teams;
        }

        public static string GetImageSource(string cmdr)
        {
            return $"_content/sc2dsstats.rlib/images/{cmdr.ToLower()}-min.png";
        }

        public static Dictionary<string, string> CMDRcolor { get; } = new Dictionary<string, string>()
        {
            {     "", "#0000ff"        },
            {     "ALL", "#0000ff"        },
            {     "global", "#0000ff"  },
            {     "Abathur", "#266a1b" },
            {     "Alarak", "#ab0f0f" },
            {     "Artanis", "#edae0c" },
            {     "Dehaka", "#d52a38" },
            {     "Fenix", "#fcf32c" },
            {     "Horner", "#ba0d97" },
            {     "Karax", "#1565c7" },
            {     "Kerrigan", "#b021a1" },
            {     "Mengsk", "#a46532" },
            {     "Nova", "#f6f673" },
            {     "Raynor", "#dd7336" },
            {     "Stetmann", "#ebeae8" },
            {     "Stukov", "#663b35" },
            {     "Swann", "#ab4f21" },
            {     "Tychus", "#342db5" },
            {     "Vorazun", "#07c543" },
            {     "Zagara", "#b01c48" },
            {     "Zeratul", "#a1e7e7"  },
            {     "Protoss", "#fcc828"   },
            {     "Terran", "#4a4684"   },
            {     "Zerg", "#6b1c92"   }
        };

        public static string GetColor(string interest)
        {
            if (CMDRcolor.ContainsKey(interest))
                return CMDRcolor[interest];
            else
                return CMDRcolor["global"];
        }

        public static string[] timespans { get; } = new string[]
        {
            "This Month",
            "Last Month",
            "This Year",
            "Last Year",
            "Last Two Years",
            "ALL",
            "Patch 2.60",
        };

        public enum StatTimeSpan
        {
            ThisMonth,
            LastMonth,
            ThisYear,
            LastYear,
            LastTwoYears,
            All,
            Path2_60
        }

        public static List<string> Timestrings(string timespan, bool mothsonly = false)
        {
            if (mothsonly == false)
            {
                return timespan switch
                {
                    "ALL" => GetAllTimeStrings(),
                    "This Year" => new List<string>() { DateTime.Today.ToString("yyyy") },
                    "Last Year" => new List<string>() { DateTime.Today.AddYears(-1).ToString("yyyy") },
                    "Last Two Years" => new List<string>() { DateTime.Today.ToString("yyyy"), DateTime.Today.AddYears(-1).ToString("yyyy") },
                    "This Month" => new List<string>() { DateTime.Today.ToString("yyyyMM") },
                    "Last Month" => new List<string>() { DateTime.Today.AddMonths(-1).ToString("yyyyMM") },
                    "Patch 2.60" => GetPatch260TimeStrings(),
                    _ => new List<string>() { DateTime.Today.ToString("yyyy") }
                };
            }
            else
            {
                (DateTime start, DateTime end) = TimeperiodSelected(timespan);
                return MonthsBetween(start, end).ToList();
            }
        }

        private static List<string> GetAllTimeStrings()
        {
            List<string> timestrings = new List<string>();
            DateTime start = new DateTime(2018, 01, 1);
            do
            {
                timestrings.Add(start.ToString("yyyy"));
                start = start.AddYears(1);
            } while (start < DateTime.Today);
            return timestrings;
        }

        private static List<string> GetPatch260TimeStrings()
        {
            DateTime start = new DateTime(2020, 08, 1);
            List<string> timestrings = new List<string>()
            {
                "202008",
                "202009",
                "202010",
                "202011",
                "202012",
            };

            start = start.AddYears(1);
            do
            {
                timestrings.Add(start.ToString("yyyy"));
                start = start.AddYears(1);
            } while (start < DateTime.Today);
            return timestrings;
        }

        private static IEnumerable<string> MonthsBetween(
                DateTime startDate,
                DateTime endDate)
        {
            DateTime iterator;
            DateTime limit;

            if (endDate > startDate)
            {
                iterator = new DateTime(startDate.Year, startDate.Month, 1);
                limit = endDate;
            }
            else
            {
                iterator = new DateTime(endDate.Year, endDate.Month, 1);
                limit = startDate;
            }

            while (iterator <= limit)
            {
                yield return iterator.ToString("yyyyMM");
                iterator = iterator.AddMonths(1);
            }
        }

        public static (DateTime, DateTime) TimeperiodSelected(string period)
        {
            return period switch
            {
                "This Month" => (DateTime.Today.AddDays(-(DateTime.Today.Day - 1)), DateTime.Today),
                "Last Month" => (DateTime.Today.AddDays(-(DateTime.Today.Day - 1)).AddMonths(-1), DateTime.Today.AddDays(-(DateTime.Today.Day - 1)).AddDays(-1)),
                "This Year" => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today),
                "Last Year" => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), new DateTime(DateTime.Now.Year, 1, 1)),
                "Last Two Years" => (new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1), DateTime.Today),
                "ALL" => (new DateTime(2018, 1, 1), DateTime.Today),
                "Patch 2.60" => (new DateTime(2020, 07, 28, 5, 23, 0), DateTime.Today),
                _ => (new DateTime(DateTime.Now.Year, 1, 1), DateTime.Today)
            };
        }

        public static string[] modes { get; } = new string[]
        {
            "Winrate",
            "MVP",
            "DPS",
            "Synergy",
            "AntiSynergy",
            "Timeline",
            "Duration",
            "Count",
            "Standard"
        };

        public static string[] durations { get; } = new string[]
        {
            "5-8",
            "8-11",
            "11-14",
            "14-17",
            "17-20",
            "20-23",
            "23-26",
            "26-29",
            "29-32",
            "32-35",
            "35"
        };

        public static string[] gamemodes { get; } = new string[]
        {
            "GameModeBrawlCommanders",
            "GameModeBrawlStandard",
            "GameModeCommanders",
            "GameModeCommandersHeroic",
            "GameModeGear",
            "GameModeSabotage",
            "GameModeStandard",
            "GameModeSwitch",
            "GameModeTutorial"
        };

        public enum Gamemode
        {
            BrawlCommanders,
            BrawlStandard,
            Commanders,
            CommandersHeroic,
            Gear,
            Sabotage,
            Standard,
            Switch,
            Tutorial
        }

        public static Gamemode GetGameMode(string mode)
        {
            return mode switch
            {
                "GameModeBrawlCommanders" => Gamemode.BrawlCommanders,
                "GameModeBrawlStandard" => Gamemode.BrawlStandard,
                "GameModeBrawl" => Gamemode.BrawlStandard,
                "GameModeCommanders" => Gamemode.Commanders,
                "GameModeCommandersHeroic" => Gamemode.CommandersHeroic,
                "GameModeGear" => Gamemode.Gear,
                "GameModeSabotage" => Gamemode.Sabotage,
                "GameModeStandard" => Gamemode.Standard,
                "GameModeSwitch" => Gamemode.Switch,
                "GameModeTutorial" => Gamemode.Tutorial,

                "BrawlCommanders" => Gamemode.BrawlCommanders,
                "BrawlStandard" => Gamemode.BrawlStandard,
                "Brawl" => Gamemode.BrawlStandard,
                "Commanders" => Gamemode.Commanders,
                "CommandersHeroic" => Gamemode.CommandersHeroic,
                "Gear" => Gamemode.Gear,
                "Sabotage" => Gamemode.Sabotage,
                "Standard" => Gamemode.Standard,
                "Switch" => Gamemode.Switch,
                "Tutorial" => Gamemode.Tutorial,

                _ => Gamemode.Commanders
            };
        }

        public static string ChartInfo(string mode) => mode switch
        {
            "Winrate" => "Winrate: Shows the winrate for each commander. When selecting a commander on the left it shows the winrate of the selected commander when matched vs the other commanders.",
            "MVP" => "MVP: Shows the % for the most ingame damage for each commander based on mineral value killed. When selecting a commander on the left it shows the mvp of the selected commander when matched vs the other commanders.",
            "DPS" => "DPS: Shows the damage delt for each commander based on mineral value killed / game duration (or army value, or minerals collected). When selecting a commander on the left it shows the damage of the selected commander when matched vs the other commanders.",
            "Synergy" => "Synergy: Shows the winrate for the selected commander when played together with the other commanders",
            "AntiSynergy" => "Antisynergy: Shows the winrate for the selected commander when played vs the other commanders (at any position)",
            "Build" => "Builds: Shows the average unit count for the selected commander at the selected game duration. When selecting a vs commander it shows the average unit count of the selected commander when matched vs the other commanders.",
            "Timeline" => "Timeline: Shows the winrate development for the selected commander over the given time period.",
            "Duration" => "Duration: Shows the winrate development for the selected commander over the given game duration.",
            "Count" => "Count: Shows the number of Matchups for each Commander.",
            "Standard" => "Standard: Shows the winrate for each team composition in standard mode.",
            _ => ""
        };

        public static double MIN5 = 6240;
        public static double MIN10 = 13440;
        public static double MIN15 = 20640;

        public static Dictionary<string, double> BreakpointMid = new Dictionary<string, double>()
        {
            { "MIN5", MIN5 },
            { "MIN10", MIN10 },
            { "MIN15", MIN15 },
            { "ALL", 0 }
        };


        public static string Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }
                return Convert.ToBase64String(mso.ToArray());
            }
        }

        public static string Unzip(string base64string)
        {
            var bytes = Convert.FromBase64String(base64string);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static async Task<string> UnzipAsync(string base64string)
        {
            var bytes = Convert.FromBase64String(base64string);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    await gs.CopyToAsync(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }


    }
}
