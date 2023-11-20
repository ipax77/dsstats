
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace dsstats.shared;

public record ArcadePlayerRatingDto
{
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public ArcadePlayerRatingPlayerDto ArcadePlayer { get; init; } = null!;
    public ArcadePlayerRatingChangeDto? ArcadePlayerRatingChange { get; init; }
}

public record ArcadePlayerRatingPlayerDto
{
    public int ArcadePlayerId { get; set; }
    public string Name { get; set; } = null!;
    public int ProfileId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
}

public record ArcadePlayerRatingChangeDto
{
    public float Change24h { get; set; }
    public float Change10d { get; set; }
    public float Change30d { get; set; }
}

public record ArcadeReplayListDto
{
    public int ArcadeReplayId { get; set; }
    public DateTime CreatedAt { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public int WinnerTeam { get; set; }
    public int Duration { get; set; }
    public double MmrChange { get; set; }
}


public record ArcadeReplayDto
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public int WinnerTeam { get; set; }
    public int Duration { get; set; }
    public ArcadeReplayRatingDto? ArcadeReplayRating { get; set; }
    public List<ArcadeReplayPlayerDto> ArcadeReplayPlayers { get; set; } = new();
}

public record ArcadeReplayPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public ArcadePlayerReplayDto ArcadePlayer { get; set; } = new();
}

public record ArcadePlayerReplayDto
{
    public int ProfileId { get; set; }
    public int RealmId { get; set; }
    public int RegionId { get; set; }
}

public record ArcadeReplayRatingDto
{
    public RatingType RatingType { get; set; }
    public float ExpectationToWin { get; set; }
    public List<ArcadeReplayPlayerRatingDto> ArcadeReplayPlayerRatings { get; set; } = new();
}

public record ArcadeReplayPlayerRatingDto
{
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
}

public record ArcadePlayerDto
{
    public int ArcadePlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public List<ArcadePlayerRatingDetailDto> ArcadePlayerRatings { get; set; } = new();
}

public record ArcadePlayerRatingDetailDto
{
    public RatingType RatingType { get; set; }
    public double Rating { get; set; }
    public int Pos { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public ArcadePlayerRatingChangeDto? ArcadePlayerRatingChange { get; set; }
}

public record ArcadeReplayListRatingDto : ArcadeReplayListDto
{
    public ArcadeReplayRatingListDto? ArcadeReplayRating { get; set; }
    public List<ArcadeReplayPlayerListDto> ArcadeReplayPlayers { get; set; } = new();
}

public record ArcadeReplayPlayerListDto
{
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public ArcadePlayerListDto ArcadePlayer { get; set; } = null!;
}

public record ArcadePlayerListDto
{
    public int ArcadePlayerId { get; set; }
}

public record ArcadeReplayRatingListDto
{
    public List<ArcadeReplayPlayerRatingListDto> ArcadeReplayPlayerRatings { get; set; } = new();
}

public record ArcadeReplayPlayerRatingListDto
{
    public int GamePos { get; set; }
    public float RatingChange { get; set; }
}

public record ArcadeRatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int PlayerId { get; set; }
    public int RegionId { get; set; }
    [JsonIgnore]
    public RatingChangeTimePeriod TimePeriod { get; set; } = RatingChangeTimePeriod.Past10Days;
}

public record DistributionRequest
{
    public RatingCalcType RatingCalcType { get; set; }
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; } = TimePeriod.All;
    public Commander Interest { get; set; }
}

public record DistributionResponse
{
    public List<MmrDevDto> MmrDevs { get; set; } = new();
}

public record MmrDevDto
{
    public double Mmr { get; init; }
    public int Count { get; set; }
}

public record ArcadeReplaysRequest
{
    public string? Search { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public bool TournamentEdition { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "CreatedAt" } };
    public int ReplayId { get; set; }
    public int PlayerId { get; set; }
    public int PlayerIdWith { get; set; }
    public int PlayerIdVs { get; set; }
    public string? ProfileName { get; set; }
}

public record ArcadePlayerDetails
{
    public ArcadePlayerDto? ArcadePlayer { get; set; }
    public List<ArcadePlayerRatingDetailDto> PlayerRatings { get; set; } = new();
    public double? CmdrPercentileRank { get; set; }
    public double? StdPercentileRank { get; set; }
}

public record ArcadePlayerMoreDetails
{
    public List<PlayerTeamResult> Teammates { get; set; } = new();
    public List<PlayerTeamResult> Opponents { get; set; } = new();
    public double AvgTeamRating { get; set; }
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
    public double Rating { get; set; }
    public int Games { get; set; }
}

public static class DistributionRequestExtension
{
    public static string GenMemKey(this DistributionRequest request)
    {
        StringBuilder sb = new();
        sb.Append("Distribution");
        sb.Append(request.RatingCalcType);
        sb.Append(request.RatingType);
        sb.Append(request.TimePeriod);
        sb.Append(request.Interest);
        return sb.ToString();
    }
}