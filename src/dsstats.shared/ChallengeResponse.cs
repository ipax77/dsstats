namespace dsstats.shared;

public record ChallengeResponse
{
    public Commander Commander { get; init; }
    public int TimeTillVictory { get; init; } = 0;
    public string ChallengeFen { get; init; } = string.Empty;
    public string PlayerFen { get; init; } = string.Empty;
    public DateTime GameTime { get; init; } = DateTime.UtcNow;
    public string? Error { get; init; } = null;
    public RequestNames RequestName { get; init; } = new();
}

public record ChallengeDto
{
    public int SpChallengeId { get; set; }
    public GameMode GameMode { get; set; }
    public Commander Commander { get; set; }
    public string Fen { get; set; } = string.Empty;
    public string Base64Image { get; set; } = string.Empty;
    public int Time { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record ChallengeSubmissionDto
{
    public DateTime Submitted { get; set; }
    public DateTime GameTime { get; set; }
    public Commander Commander { get; set; }
    public string Fen { get; set; } = string.Empty;
    public int Time { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

public record ChallengeSubmissionListDto
{
    public int SpChallengeSubmissionId { get; set; }
    public DateTime Submitted { get; set; }
    public DateTime GameTime { get; set; }
    public Commander Commander { get; set; }
    public int Time { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}

public class FinishedChallengeDto
{
    public int SpChallengeId { get; set; }
    public GameMode GameMode { get; set; }
    public Commander Commander { get; set; }
    public string Fen { get; set; } = string.Empty;
    public string Base64Image { get; set; } = string.Empty;
    public int Time { get; set; }
    public DateTime CreatedAt { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public int WinnerTime { get; set; }
    public List<ChallengeSubmissionListDto> TopSubmissions { get; set; } = [];
}

public class PlayerRankingDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int Submissions { get; set; }
    public int Wins { get; set; }
    public int Seconds { get; set; }
    public int Thirds { get; set; }
    public int Rank { get; set; }
}