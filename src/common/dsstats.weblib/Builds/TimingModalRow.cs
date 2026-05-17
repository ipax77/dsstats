namespace dsstats.weblib.Builds;

public sealed record TimingModalRow(string Name, double AverageTimeSeconds, int Count, double UsagePercent);
