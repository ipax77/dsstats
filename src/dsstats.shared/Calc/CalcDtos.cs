namespace dsstats.shared.Calc;

public record CalcRating
{
    public PlayerId PlayerId { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public Dictionary<Commander, int> CmdrCounts { get; set; } = new();
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
    public List<int> GameModes { get; set; } = [ (int)GameMode.Commanders, (int)GameMode.CommandersHeroic, (int)GameMode.Standard ];
    public bool Continue { get; set; }
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
    public int ReplayId { get; set; }
    public DateTime GameTime { get; init; }
    public int GameMode { get; set; }
    public int WinnerTeam { get; init; }
    public int Duration { get; init; }
    public bool TournamentEdition { get; init; }
    public bool IsArcade { get; init; }
    public List<PlayerCalcDto> Players { get; init; } = new();
}

public record PlayerCalcDto
{
    public int ReplayPlayerId { get; init; }
    public int GamePos { get; init; }
    public int PlayerResult { get; set; }
    public bool IsLeaver { get; init; }
    public bool IsMvp { get; init; }
    public int Team { get; init; }
    public Commander Race { get; init; }
    public PlayerId PlayerId { get; init; } = null!;
    public bool IsUploader { get; set; }
    public CalcRating CalcRating { get; set; } = new();
}

public static class ClacDtoExtensions
{
    public static int GetRatingType(this CalcDto calcDto)
    {
        if (calcDto.TournamentEdition && calcDto.GameMode == 3)
        {
            return 3;
        }
        else if (calcDto.TournamentEdition && calcDto.GameMode == 7)
        {
            return 4;
        }
        else if (calcDto.GameMode == 3 || calcDto.GameMode == 4)
        {
            return 1;
        }
        else if (calcDto.GameMode == 7)
        {
            return 2;
        }
        else
        {
            return 0;
        }
    }

    public static int GetLeaverTyp(this CalcDto calcDto)
    {
        int leavers = calcDto.Players.Count(c => c.IsLeaver);

        if (leavers == 0)
        {
            return 0;
        }

        if (leavers == 1)
        {
            return 1;
        }

        if (leavers > 2)
        {
            return 4;
        }

        var leaverPlayers = calcDto.Players.Where(x => x.IsLeaver);
        var teamsCount = leaverPlayers.Select(s => s.Team).Distinct().Count();

        if (teamsCount == 1)
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }
}