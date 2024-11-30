namespace dsstats.shared.Interfaces;

public interface IImportService
{
    bool IsInit { get; }

    Task<int> GetPlayerIdAsync(PlayerId playerId, string name);
    Task<Dictionary<PlayerId, int>> GetPlayerIdDictionary();
    Task<ImportResult> Import(List<ReplayDto> replayDtos, List<PlayerId>? uploaderPlayerIds = null);
    Task Init();
    Task SetPreRatings();
    Task FixArcadePlayers();
}