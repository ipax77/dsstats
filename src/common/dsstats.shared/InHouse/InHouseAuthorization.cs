using System.Security.Claims;

namespace dsstats.shared.InHouse;

public static class InHouseClaims
{
    public const string UserId = "inhouse_user_id";
}

public static class InHouseRoles
{
    public const string Admin = "Admin";
}

public static class InHousePolicies
{
    public const string CloseSession = "CloseSession";
}

public interface IInHouseSessionAuthorizationResource
{
    Guid CreatedByUserId { get; }
    DateTime? ClosedAt { get; }
}

public static class InHouseAuthorization
{
    public static bool CanCloseSession(ClaimsPrincipal? user, IInHouseSessionAuthorizationResource? session)
        => user?.Identity?.IsAuthenticated == true
            && session is { ClosedAt: null }
            && (IsAdmin(user) || IsSessionCreator(user, session));

    public static bool IsAdmin(ClaimsPrincipal? user)
        => user?.IsInRole(InHouseRoles.Admin) == true;

    public static int GetRequiredInternalUserId(ClaimsPrincipal? user)
        => int.TryParse(user?.FindFirst(InHouseClaims.UserId)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("You are not signed in.");

    private static bool IsSessionCreator(ClaimsPrincipal user, IInHouseSessionAuthorizationResource session)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var publicUserId)
            && publicUserId == session.CreatedByUserId;
}
