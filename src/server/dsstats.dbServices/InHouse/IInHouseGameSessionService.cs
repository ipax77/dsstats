using dsstats.shared.InHouse;

namespace dsstats.dbServices.InHouse;

public interface IInHouseGameSessionService
{
    Task<List<InHouseGameSessionListDto>> GetActiveSessionsAsync(CancellationToken token);
    Task<InHouseClosedGameSessionsPageDto> GetClosedSessionsAsync(InHouseClosedGameSessionsRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto> CreateSessionAsync(int userId, InHouseCreateGameSessionRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto?> GetSessionAsync(Guid sessionId, int userId, CancellationToken token);
    Task<InHouseGameSessionMutationResult> UploadReplayAsync(Guid sessionId, int userId, InHouseReplayUploadRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto> AddRosterPlayerAsync(Guid sessionId, int userId, InHouseRosterPlayerUpsertRequest request, CancellationToken token);
    Task<InHouseGameSessionDetailDto> SetRosterPlayerSitterAsync(Guid sessionId, Guid rosterPlayerId, int userId, bool isSitter, CancellationToken token);
    Task<InHouseGameSessionDetailDto> RemoveRosterPlayerAsync(Guid sessionId, Guid rosterPlayerId, int userId, CancellationToken token);
    Task<InHouseGameSessionDetailDto> CloseSessionAsync(Guid sessionId, int userId, bool isAdmin, CancellationToken token);
    Task DeleteSessionAsync(Guid sessionId, int userId, bool isAdmin, CancellationToken token);
    Task<List<InHouseGameSessionDetailDto>> CloseInactiveSessionsAsync(TimeSpan inactiveFor, CancellationToken token);
}

public sealed record InHouseGameSessionMutationResult(
    InHouseGameSessionDetailDto State,
    bool Changed);
