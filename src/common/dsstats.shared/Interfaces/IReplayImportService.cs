namespace dsstats.shared.Interfaces;

public interface IReplayImportService
{
    Task<ReplayImportResultDto?> ImportReplayWithSpawnPlayback(
        ReplayDto replay,
        SpawnPlaybackEncodedSidecar spawnPlayback,
        CancellationToken token = default);
}

public sealed class ReplayImportResultDto
{
    public bool Success { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public string? Error { get; set; }
}
