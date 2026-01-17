namespace dsstats.service.Models;

internal class DsstatsConfig
{
    public string UploadUrl { get; set; } = string.Empty;
    public int StartDelayInMinutes { get; set; }
    public int BatchSize { get; set; }
}
