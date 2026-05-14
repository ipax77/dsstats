using dsstats.shared.InHouse;

namespace dsstats.dbServices.InHouse;

public interface IInHouseGameSessionService
{
    Task<List<InHouseGameSessionListDto>> GetActiveSessionsAsync(CancellationToken token);
    Task<InHouseGameSessionDetailDto> CreateSessionAsync(int userId, InHouseCreateGameSessionRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto?> GetSessionAsync(Guid sessionId, int userId, CancellationToken token);
    Task<InHouseGameSessionDetailDto> UploadReplayAsync(Guid sessionId, int userId, InHouseReplayUploadRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto> CloseSessionAsync(Guid sessionId, int userId, CancellationToken token);
}
