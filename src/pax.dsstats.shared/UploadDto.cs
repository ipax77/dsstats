
namespace pax.dsstats.shared;

public record UploadDto
{
    public Guid AppGuid { get; init; }
    public DateTime LatestReplays { get; init; }
    public string Base64ReplayBlob { get; init; } = "";
}
