namespace pax.dsstats.shared.Calc;

public record CalcRating
{
    public PlayerId PlayerId { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
}

public record PlayerId
{
    public int ProfileId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
}

public record ReplayRatingDto
{
    public int RatingType { get; set; }
    public int LeaverType { get; init; }
    public float ExpectationToWin { get; init; } // WinnerTeam
    public int ReplayId { get; set; }
    public bool IsPreRating { get; set; }
    public List<RepPlayerRatingDto> RepPlayerRatings { get; init; } = new();
}

public record RepPlayerRatingDto
{
    public int GamePos { get; init; }
    public float Rating { get; init; }
    public float RatingChange { get; init; }
    public int Games { get; init; }
    public float Consistency { get; init; }
    public float Confidence { get; init; }
    public int ReplayPlayerId { get; init; }
}


public record DsstatsCalcRequest
{
    public DateTime FromDate { get; set; } = new DateTime(2021, 2, 1);
    public List<int> GameModes { get; set; } = new() { 3, 4 };
    public int Skip { get; set; }
    public int Take { get; set; }
}

public record Sc2ArcadeRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<int> GameModes { get; set; } = new() { 3, 4 };
}

public record CalcDto
{
    public int DsstatsReplayId { get; set; }
    public int Sc2ArcadeReplayId { get; set; }
    public DateTime GameTime { get; init; }
    public int GameMode { get; set; }
    public int Duration { get; init; }
    public bool TournamentEdition { get; init; }
    public List<PlayerCalcDto> Players { get; init; } = new();
}

public record PlayerCalcDto
{
    public int ReplayPlayerId { get; init; }
    public int GamePos { get; init; }
    public int PlayerResult { get; init; }
    public bool IsLeaver { get; init; }
    public int Team { get; init; }
    public int ProfileId { get; init; }
    public int RegionId { get; init; }
    public int RealmId { get; init; }
}