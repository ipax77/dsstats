namespace dsstats.shared.Builder;

public sealed record BuilderRequest(
    Commander Commander,
    int Team,
    SpawnDto Spawn,
    IReadOnlyList<UpgradeDto>? Upgrades = null,
    BuilderPreparationOptions? Preparation = null,
    bool Mirror = false);

public sealed record BuilderPreparationOptions(
    bool ClearUnits = false,
    bool ResetResearch = false,
    bool ToggleBase = false,
    bool EnableVision = false);

public interface IBuilderService
{
    bool IsAvailable { get; }
    bool Supports(Commander commander);
    Task BuildAsync(BuilderRequest request, CancellationToken cancellationToken = default);
    Task BuildAsync(IReadOnlyList<BuilderRequest> requests, CancellationToken cancellationToken = default);
}

public sealed class UnavailableBuilderService : IBuilderService
{
    public bool IsAvailable => false;
    public bool Supports(Commander commander) => false;

    public Task BuildAsync(BuilderRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException(new PlatformNotSupportedException("The Direct Strike builder is only available in the Windows MAUI app."));

    public Task BuildAsync(IReadOnlyList<BuilderRequest> requests, CancellationToken cancellationToken = default) =>
        Task.FromException(new PlatformNotSupportedException("The Direct Strike builder is only available in the Windows MAUI app."));
}
