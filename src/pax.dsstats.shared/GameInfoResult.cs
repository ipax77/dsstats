namespace pax.dsstats.shared;

public record GameInfoResult
{
    public List<PlayerInfo> PlayerInfos { get; set; } = new();
}

public record PlayerInfo
{
    public string Name { get; init; } = null!;
    public RequestNames? RequestNames { get; init; }
    public List<PlayerRatingInfoDto> Ratings { get; init; } = new();
}

public record GameInfoRequest
{
    public List<string> PlayerNames { get; init; } = new();
}