namespace dsstats.shared;

public class DistributionResponse
{
    public List<DistributionItem> Items { get; set; } = [];
}

public class DistributionItem
{
    public int Rating { get; set; }
    public int Count { get; set; }
}

public class DistributionRequest
{
    public RatingType RatingType { get; set; }
}

public static class DistributionRequestExtensions
{
    public static string GetMemKey(this DistributionRequest request)
    {
        return $"distribution_{request.RatingType}";
    }
}