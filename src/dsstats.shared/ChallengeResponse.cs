namespace dsstats.shared;

public record ChallengeResponse
{
    public string Challenge { get; init; } = string.Empty;
    public Commander Commander { get; init; }
    public int TimeTillVictory { get; init; } = 0;
    public string ChallengeFen { get; init; } = string.Empty;
    public string PlayerFen { get; init; } = string.Empty;
    public string? Error { get; init; } = null;
    public RequestNames RequestName { get; init; } = new();
}
