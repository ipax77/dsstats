﻿namespace dsstats.shared;

public record ReviewRequest
{
    public RequestNames RequestName { get; set; } = null!;
    public RatingType RatingType { get; set; }
    public int Year { get; set; }
}

public record ReviewResponse
{
    public RatingType RatingType { get; set; }
    public int TotalGames { get; set; }
    public int Duration { get; set; }
    public int LongestWinStreak { get; set; }
    public int LongestLosStreak { get; set; }
    public int CurrentStreak { get; set; }
    public List<CommanderReviewInfo> CommanderInfos { get; set; } = new();
    public List<ReplayPlayerReviewDto> RatingInfos { get; set; } = new();
    public string? LongestReplay { get; set; }
    public string? MostCompetitiveReplay { get; set; }
    public string? GreatestComebackReplay { get; set; }
    public bool IsUploader { get; set; }
}

public record ReplayPlayerReviewDto
{
    public ReplayChartDto Replay { get; set; } = new();
    public RepPlayerRatingChartDto? ReplayPlayerRatingInfo { get; set; }
}

public record CommanderReviewInfo
{
    public Commander Cmdr { get; set; }
    public int Count { get; set; }
    public RatingType RatingType { get; set; }
}

public record ReviewYearResponse
{
    public RatingType RatingType { get; set; }
    public int TotalGames { get; set; }
    public int LongestWinStreak { get; set; }
    public int LongestLosStreak { get; set; }
    public List<CommanderReviewInfo> CommanderInfos { get; set; } = new();
    public string? LongestReplay { get; set; }
    public string? MostCompetitiveReplay { get; set; }
    public string? GreatestComebackReplay { get; set; }
}