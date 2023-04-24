using System.Globalization;
using System;
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;


public record PlayerDetailDto
{
    public List<PlayerMatchupInfo> MatchupInfos { get; init; } = new();
    public RavenPlayerDetailsDto PlayerDetails { get; init; } = new();
}

public record PlayerDetailInfo
{
    public List<PlayerMatchupInfo> MatchupInfos = new();
    [JsonIgnore]
    public List<PlayerCmdrInfo> CmdrInfos => MatchupInfos.Any() ? (from m in MatchupInfos
                                                                   group m by m.Commander into g
                                                                   select new PlayerCmdrInfo
                                                                   {
                                                                       Commander = g.Key,
                                                                       Count = g.Sum(s => s.Count),
                                                                       Wins = g.Sum(s => s.Wins)
                                                                   }).ToList() : new();
    [JsonIgnore]
    public PlayerCmdrInfo? MostPlayedCmdrCmdr => CmdrInfos.Where(x => (int)x.Commander > 3).OrderByDescending(o => o.Count).FirstOrDefault();
    [JsonIgnore]
    public PlayerCmdrInfo? LeastPlayedCmdrCmdr => CmdrInfos.Where(x => (int)x.Commander > 3).OrderBy(o => o.Count).FirstOrDefault();
    [JsonIgnore]
    public PlayerCmdrInfo? MostPlayedCmdrStd => CmdrInfos.Where(x => (int)x.Commander <= 3).OrderByDescending(o => o.Count).FirstOrDefault();
    [JsonIgnore]
    public PlayerCmdrInfo? LeastPlayedCmdrStd => CmdrInfos.Where(x => (int)x.Commander <= 3).OrderBy(o => o.Count).FirstOrDefault();
    [JsonIgnore]
    public int SumCmdr => CmdrInfos.Where(x => (int)x.Commander > 3).Sum(o => o.Count);
    [JsonIgnore]
    public int SumStd => CmdrInfos.Where(x => (int)x.Commander <= 3).Sum(o => o.Count);
}

public record PlayerCmdrInfo
{
    public Commander Commander { get; init; }
    public int Count { get; set; }
    public int Wins { get; set; }
}

public record PlayerMatchupInfo
{
    public Commander Commander { get; init; }
    public Commander Versus { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
}

public record ReplayPlayerChartDto
{
    public ReplayChartDto Replay { get; set; } = new();
    public RepPlayerRatingChartDto? ReplayPlayerRatingInfo { get; set; }
}

public record ReplayChartDto
{
    public DateTime GameTime => GetDateTime();
    public int Year { get; set; }
    public int Week { get; set; }

    private DateTime GetDateTime()
    {
        DayOfWeek dayOfWeek = DayOfWeek.Monday;

        DateTime dateOfMonday = new DateTime(Year, 1, 1)
            .AddDays((Week - 1) * 7)
            .AddDays(-(int)(new GregorianCalendar().GetDayOfWeek(new DateTime(Year, 1, 1))) + (int)dayOfWeek + 7);

        if (dateOfMonday.Year < Year)
        {
            dateOfMonday = dateOfMonday.AddDays(7);
        }

        DateTime startOfWeek = dateOfMonday;

        return startOfWeek;
    }
}

public record RepPlayerRatingChartDto
{
    public float Rating { get; set; }
    public int Games { get; set; }
}

public record ChartRatingData
{
    public string X { get; set; } = string.Empty;
    public float Y { get; set; }
}

public record ChartGamesData
{
    public string X { get; set; } = string.Empty;
    public int Y { get; set; }
}