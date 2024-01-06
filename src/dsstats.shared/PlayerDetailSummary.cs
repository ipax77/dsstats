
using System.ComponentModel.DataAnnotations.Schema;

namespace dsstats.shared;

public record PlayerDetailSummary
{
    public List<PlayerGameModeResult> GameModesPlayed { get; set; } = new();
    public List<PlayerRatingDetailDto> Ratings { get; set; } = new();
    public List<CommanderInfo> Commanders { get; set; } = new();
    public List<ReplayPlayerChartDto> ChartDtos { get; set; } = new();
    public double? CmdrPercentileRank { get; set; }
    public double? StdPercentileRank { get; set; }
    public MvpInfo? MvpInfo {  get; set; }
}

public record MvpInfo
{
    public int Games { get; set; }
    public int Mvp { get; set; }
    public int MainCount { get; set; }
    public Commander Main { get; set; }
}

public record CommanderInfo
{
    public Commander Cmdr { get; set; }
    public int Count { get; set; }
}

public record PlayerGameModeResult
{
    public GameMode GameMode { get; init; }
    public int PlayerCount { get; init; }
    public int Count { get; init; }
}

public record PlayerRatingDetailDto
{
    public RatingType RatingType { get; init; }
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public int ArcadeDefeatsSinceLastUpload { get; set; }
    public PlayerRatingPlayerDto Player { get; init; } = null!;
    public PlayerRatingChangeDto? PlayerRatingChange { get; init; }
    [NotMapped]
    public double MmrChange { get; set; }
    [NotMapped]
    public double FakeDiff { get; set; }
}

public record PlayerRatingPlayerDto
{
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public bool IsUploader { get; set; }
    public int ArcadeDefeatsSinceLastUpload { get; set; }
}

public record PlayerRatingChangeDto
{
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
}

public record PlayerId
{
    public PlayerId()
    {

    }

    public PlayerId(int toonId, int realmId, int regionId)
    {
        ToonId = toonId;
        RealmId = realmId;
        RegionId = regionId;
    }

    public int ToonId { get; set; }
    public int RealmId { get; set; }
    public int RegionId { get; set; }
}

public record PlayerRatingDetails
{
    public List<PlayerTeamResult> Teammates { get; set; } = new();
    public List<PlayerTeamResult> Opponents { get; set; } = new();
    public List<PlayerMatchupInfo> Matchups { get; set; } = new();
    public List<PlayerCmdrAvgGain> CmdrsAvgGain { get; set; } = new();
    public double AvgTeamRating { get; set; }
    public double AvgOppRating { get; set; }
}

public record PlayerTeamResult
{
    public string? Name { get; init; }
    public PlayerId PlayerId { get; init; } = null!;
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgGain { get; init; }
}

public record PlayerMatchupInfo
{
    public Commander Commander { get; init; }
    public Commander Versus { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
}

public record PlayerCmdrAvgGain
{
    public Commander Commander { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgGain { get; init; }
}

public record PlayerTeamResultHelper
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
}

public record PlayerDetailResponse
{
    public List<CmdrStrengthItem> CmdrStrengthItems { get; set; } = new();
}

public record CmdrStrengthItem
{
    public Commander Commander { get; init; }
    public int Matchups { get; init; }
    public double AvgRating { get; init; }
    public double AvgRatingGain { get; init; }
    public int Wins { get; init; }
    [NotMapped]
    public double Strength { get; set; }
    [NotMapped]
    public double MarginOfError { get; set; }
}

public record PlayerDetailRequest
{
    public RequestNames RequestNames { get; set; } = new("", 0, 0, 1);
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public bool Complete { get; set; }
}