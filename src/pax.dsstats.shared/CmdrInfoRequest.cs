
using pax.dsstats.shared.Arcade;
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record CmdrInfoRequest
{
    public CmdrInfoRequest()
    {

    }

    public CmdrInfoRequest(CmdrInfosRequest request)
    {
        RatingType = request.RatingType;
        TimePeriod = request.TimePeriod;
        Interest = request.Interest;
        WithoutLeavers = request.WithoutLeavers;
        Uploaders = request.Uploaders;
    }

    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public int MaxGap { get; set; }
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
    public bool WithoutLeavers { get; set; }
    public bool Uploaders { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}

public record ReplayCmdrInfo
{
    public int ReplayId { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int Maxleaver { get; set; }
    public float Rating1 { get; set; }
    public float Rating2 { get; set; }
    public float AvgGain { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public int WinnerTeam { get; set; }
    public string Ratings { get; set; } = string.Empty;
    [JsonIgnore]
    public Commander[] Cmdrs1 => Team1.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
    [JsonIgnore]
    public Commander[] Cmdrs2 => Team2.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();    
}

public record CmdrPlayerInfo
{
    public string Name { get; set; } = string.Empty;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; } = 1;
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
    public double AvgRating { get; set; }
    public double TeamRating { get; set; }
    [JsonIgnore]
    public int Pos { get; set; }
    [JsonIgnore]
    public double Strength { get; set; }
}

public record CmdrInfosRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public bool Uploaders { get; set; }
    public int MinExp2Win { get; set; }
    public int MaxExp2Win { get; set; }
    public bool WithoutLeavers { get; set; }
    public PlayerId? PlayerId { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
    public string SearchCmdrs { get; set; } = string.Empty;
    public string SearchNames { get; set; } = string.Empty;
    public bool LinkCmdrName { get; set; }
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
}

public record ReplayCmdrListDto
{
    public string ReplayHash { get; init; } = string.Empty;
    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public string CommandersTeam1 { get; init; } = string.Empty;
    public string CommandersTeam2 { get; init; } = string.Empty;
    public int WinnerTeam { get; init; }
    public int Maxleaver { get; init; }
    public ReplayRatingCmdrDto ReplayRatingInfo { get; init; } = new();
}

public record ReplayRatingCmdrDto
{
    public float ExpectationToWin { get; init; }
    public List<RepPlayerRatingCmdrDto> RepPlayerRatings { get; init; } = new();
}


public record RepPlayerRatingCmdrDto
{
    public int GamePos { get; init; }
    public float Rating { get; init; }
    public float RatingChange { get; init; }
}