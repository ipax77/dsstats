namespace dsstats.shared.Upload;

public sealed class UploadDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public List<RequestNames> RequestNames { get; init; } = [];
    public string Base64ReplayBlob { get; init; } = string.Empty;
}

public sealed class UploadRequestDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public List<RequestNames> RequestNames { get; init; } = [];
    public List<ReplayDto> Replays { get; init; } = [];
}

public sealed class RequestNames
{
    public string Name { get; init; } = string.Empty;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public int RealmId { get; init; }
}

public static class RequestNamesExtensions
{
    public static ToonIdDto ToToonIdDto(this RequestNames requestNames)
    {
        return new ToonIdDto
        {
            Id = requestNames.ToonId,
            Region = requestNames.RegionId,
            Realm = requestNames.RealmId
        };
    }

    public static ToonIdRec ToToonIdRec(this RequestNames requestNames)
    {
        return new ToonIdRec(requestNames.RegionId, requestNames.RealmId, requestNames.ToonId);
    }
}