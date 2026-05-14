using dsstats.shared.InHouse;

namespace dsstats.api.InHouse;

public interface IInHouseAuthService
{
    Task<InHouseAuthOptionsResponse> BeginRegistrationAsync(InHouseRegisterOptionsRequest request, CancellationToken token);
    Task<InHouseSessionDto> CompleteRegistrationAsync(InHouseRegisterCompleteRequest request, CancellationToken token);
    Task<InHouseAuthOptionsResponse> BeginLoginAsync(InHouseLoginOptionsRequest request, CancellationToken token);
    Task<InHouseSessionDto> CompleteLoginAsync(InHouseLoginCompleteRequest request, CancellationToken token);
    Task<InHouseSessionDto> RefreshAsync(InHouseRefreshRequest request, CancellationToken token);
    Task LogoutAsync(string? accessToken, InHouseRefreshRequest? request, CancellationToken token);
    Task<InHouseUserDto?> GetCurrentUserAsync(int userId, CancellationToken token);
    Task<InHouseDeviceLinkOptionsResponse> CreateDeviceLinkCodeAsync(int userId, CancellationToken token);
    Task<InHouseAuthOptionsResponse> BeginDeviceLinkAsync(InHouseDeviceLinkOptionsRequest request, CancellationToken token);
    Task<InHouseSessionDto> CompleteDeviceLinkAsync(InHouseDeviceLinkCompleteRequest request, CancellationToken token);
    Task<InHouseTokenValidationResult?> ValidateAccessTokenAsync(string accessToken, CancellationToken token);
}
