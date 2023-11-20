namespace dsstats.shared;

public record DsUpdateInfo
{
    public Commander Commander { get; set; }
    public DateTime Time { get; set; }
    public string Id { get; set; } = string.Empty;
    public List<string> Changes { get; set; } = new();
}

public record DiscordChannel
{
    public List<DiscordMessage> Messages { get; set; } = new();
}

public record DiscordMessage
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
}