namespace dsstats.shared;

public record WinrateEnt
{
    public Commander Commander { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgRating { get; set; }
    public double AvgGain { get; set; }
    public int Replays { get; set; }
}


public record WinrateRequest : StatsRequest
{
    public WinrateRequest() { }
    public WinrateRequest(StatsRequest request, WinrateType winrateType)
    {
        this.TimePeriod = request.TimePeriod;
        this.RatingType = request.RatingType;
        this.Interest = request.Interest;
        this.ComboRating = request.ComboRating;
        this.Filter = request.Filter;
        this.WinrateType = winrateType;
    }

    public WinrateType WinrateType { get; set; }
}

public record WinrateResponse
{
    public Commander Interest { get; set; }
    public List<WinrateEnt> WinrateEnts { get; set; } = new();
}

public enum WinrateType
{
    AvgGain = 0,
    Winrate = 1,
    Matchups = 2,
    AvgRating = 3,
}

//public static class WinrateRequestExtension
//{
//    public static string GenMemKey(this WinrateRequest request)
//    {
//        StringBuilder sb = new();
//        sb.Append("StatsWinrate");
//        sb.Append(request.Filter.Exp2Win?.FromExp2Win.ToString());
//        sb.Append(request.TimePeriod.ToString());
//        sb.Append(request.Filter.Exp2Win?.ToExp2Win.ToString());
//        sb.Append(request.RatingType.ToString());
//        sb.Append(request.Filter.Rating?.FromRating.ToString());
//        sb.Append(request.Interest.ToString());
//        sb.Append(request.Filter.Rating?.ToRating.ToString());
//        sb.Append(request.ComboRating);
//        return sb.ToString();
//    }
//}