namespace dsstats.api.InHouse;

public static class InHouseClaims
{
    public const string UserId = "inhouse_user_id";
}

public sealed record InHouseTokenValidationResult(
    int UserId,
    Guid PublicId,
    string DisplayName,
    DateTime ExpiresAt);
