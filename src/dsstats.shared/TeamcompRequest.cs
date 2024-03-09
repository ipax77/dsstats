using System.Text;

namespace dsstats.shared;

public record TeamcompReplaysRequest
{
    public TimePeriod TimePeriod { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string? Team2 { get; set; }
    public bool TournementEdition { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public record TeamcompRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public bool TournamentEdition { get; set; }
    public bool WithLeavers { get; set; }
    public string? Interest { get; set; }
}

public record TeamcompResponse
{
    public string? Team { get; set; }
    public List<TeamResponseItem> Items { get; set; } = new();
}

public record TeamResponseItem
{
    public string Team { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
}

public static class TeamcompExtensions
{
    public static string GenMemKey(this TeamcompRequest request)
    {
        StringBuilder sb = new();
        sb.Append("Teamcomp");
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.RatingType.ToString());
        sb.Append(request.WithLeavers.ToString());
        sb.Append(request.TournamentEdition.ToString());
        sb.Append(request.Interest ?? "none");
        return sb.ToString();
    }
}