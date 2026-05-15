namespace dsstats.api.InHouse;

public static class InHouseClaims
{
    public const string UserId = "inhouse_user_id";
}

public static class InHouseRoles
{
    public const string Admin = "admin";
}

public sealed record InHouseTokenValidationResult(
    int UserId,
    Guid PublicId,
    string DisplayName,
    bool IsAdmin,
    DateTime ExpiresAt);
