namespace pax.dsstats.shared;

public record UploaderDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = null!;
    public ICollection<PlayerUploadDto> Players { get; init; } = null!;
    public ICollection<BattleNetInfoDto>? BatteBattleNetInfos { get; init; } = null!;
}

public record BattleNetInfoDto
{
    public int BattleNetId { get; init; }
}

public record PlayerUploadDto
{
    public string Name { get; init; } = null!;
    public int Toonid { get; init; }
}
