using pax.dsstats.shared;
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