namespace dsstats.api.Services;

public sealed class UploadStorageOptions
{
    public const string SectionName = "UploadStorage";

    public string BlobBaseDir { get; set; } = "/data/ds/replayblobs";
    public string ReplayBaseDir { get; set; } = "/data/ds/replayblobs";
}
