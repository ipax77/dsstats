namespace dsstats.api.InHouse;

public sealed record InHouseTokenValidationResult(
    int UserId,
    Guid PublicId,
    string DisplayName,
    bool IsAdmin,
    DateTime ExpiresAt);
