namespace dsstats.service.Models;

internal sealed class DsstatsConfig
{
    public string UploadUrl { get; set; } = string.Empty;
    public int StartDelayInMinutes { get; set; }
    public int BatchSize { get; set; }
}
