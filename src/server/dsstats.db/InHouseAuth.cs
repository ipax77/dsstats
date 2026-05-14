using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class InHouseUser
{
    public int InHouseUserId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    [MaxLength(40)]
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public ICollection<InHousePasskeyCredential> Passkeys { get; set; } = [];
    public ICollection<InHouseProfile> Profiles { get; set; } = [];
    public ICollection<InHouseSession> Sessions { get; set; } = [];
    public ICollection<InHouseDeviceLinkCode> DeviceLinkCodes { get; set; } = [];
}

public sealed class InHousePasskeyCredential
{
    public int InHousePasskeyCredentialId { get; set; }
    public int InHouseUserId { get; set; }
    public InHouseUser? User { get; set; }
    [MaxLength(512)]
    public string CredentialId { get; set; } = string.Empty;
    [MaxLength(256)]
    public string UserHandle { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = [];
    public uint SignatureCounter { get; set; }
    public bool IsBackedUp { get; set; }
    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

public sealed class InHouseProfile
{
    public int InHouseProfileId { get; set; }
    public int InHouseUserId { get; set; }
    public InHouseUser? User { get; set; }
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = null!;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class InHouseSession
{
    public int InHouseSessionId { get; set; }
    public int InHouseUserId { get; set; }
    public InHouseUser? User { get; set; }
    [MaxLength(128)]
    public string AccessTokenHash { get; set; } = string.Empty;
    [MaxLength(128)]
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public sealed class InHouseDeviceLinkCode
{
    public int InHouseDeviceLinkCodeId { get; set; }
    public int InHouseUserId { get; set; }
    public InHouseUser? User { get; set; }
    [MaxLength(128)]
    public string CodeHash { get; set; } = string.Empty;
    [MaxLength(12)]
    public string DisplayCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
