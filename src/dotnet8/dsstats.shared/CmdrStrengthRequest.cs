
using System.Text;

namespace dsstats.shared;

public record CmdrStrengthRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public TeamRequest Team { get; set; }
    public bool ComboRating { get; set; }
}

public record CmdrStrengthResult
{
    public List<CmdrStrengthItem> Items { get; init; } = new();
}

public static class CmdrStrengthExtension
{
    public static string GenMemKey(this CmdrStrengthRequest request)
    {
        StringBuilder sb = new();
        sb.Append("cmdrStrength");
        sb.Append(request.RatingType.ToString());
        sb.Append(request.TimePeriod.ToString());
        sb.Append(request.Interest);
        sb.Append('|');
        sb.Append(request.Team);
        sb.Append(request.ComboRating.ToString());
        return sb.ToString();
    }
}