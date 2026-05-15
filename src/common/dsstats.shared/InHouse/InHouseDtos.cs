using System.Text.Json;

namespace dsstats.shared.InHouse;

public static class InHouseAccountChangeReasons
{
    public const string Passkeys = "passkeys";
    public const string Profiles = "profiles";
}

public sealed class InHouseProfileDto
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public bool Active { get; set; } = true;
}

public sealed class InHouseUserDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<InHouseProfileDto> Profiles { get; set; } = [];
    public List<InHousePasskeyDto> Passkeys { get; set; } = [];
}

public sealed class InHousePasskeyDto
{
    public int PasskeyId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public bool IsBackedUp { get; set; }
}

public sealed class InHouseSessionDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public InHouseUserDto User { get; set; } = new();
}

public sealed class InHouseRegisterOptionsRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public InHouseProfileDto Profile { get; set; } = new();
}

public sealed class InHouseRegisterCompleteRequest
{
    public Guid ChallengeId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public JsonElement Credential { get; set; }
}

public sealed class InHouseLoginOptionsRequest
{
    public string? DisplayName { get; set; }
}

public sealed class InHouseLoginCompleteRequest
{
    public Guid ChallengeId { get; set; }
    public JsonElement Credential { get; set; }
}

public sealed class InHouseRefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class InHouseDeviceLinkOptionsResponse
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class InHouseDeviceLinkOptionsRequest
{
    public string Code { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
}

public sealed class InHouseDeviceLinkCompleteRequest
{
    public Guid ChallengeId { get; set; }
    public JsonElement Credential { get; set; }
}

public sealed class InHouseRemovePasskeyRequest
{
    public int PasskeyId { get; set; }
}

public sealed class InHouseAuthOptionsResponse
{
    public Guid ChallengeId { get; set; }
    public JsonElement Options { get; set; }
}

public sealed class InHouseConnectedPlayersCount
{
    public int Count { get; set; }
}
