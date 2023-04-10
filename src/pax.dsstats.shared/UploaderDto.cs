namespace pax.dsstats.shared;

public record UploaderDto
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
