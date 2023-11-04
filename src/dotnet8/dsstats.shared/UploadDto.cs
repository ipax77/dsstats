namespace dsstats.shared;

public record UploadDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public List<RequestNames> RequestNames { get; init; } = new();
    public string Base64ReplayBlob { get; init; } = "";
}
