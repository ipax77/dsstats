namespace dsstats.service.Models;

public class DsstatsConfig
{
    public string UploadUrl { get; set; } = string.Empty;
    public int StartDelayInMinutes { get; set; }
    public int BatchSize { get; set; }
}
