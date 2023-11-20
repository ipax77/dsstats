namespace dsstats.shared;

public record UploadDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public List<RequestNames> RequestNames { get; init; } = new();
    public string Base64ReplayBlob { get; init; } = "";
}

public record UploadDtoV6
{
    public Guid AppGuid { get; init; }
    public DateTime LatestReplays { get; init; }
    public string Base64ReplayBlob { get; init; } = "";
}

public record UploaderDtoV6
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = null!;
    public ICollection<BattleNetInfoDto> BattleNetInfos { get; init; } = new List<BattleNetInfoDto>();
}

public record BattleNetInfoDto
{
    public int BattleNetId { get; init; }
    public ICollection<PlayerUploadDto> PlayerUploadDtos { get; init; } = new List<PlayerUploadDto>();
}

public record PlayerUploadDto
{
    public string Name { get; init; } = null!;
    public int RegionId { get; init; }
    public int ToonId { get; init; }
    public int RealmId { get; init; }
}