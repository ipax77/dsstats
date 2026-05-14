using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.InHouse;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dsstats.api.InHouse;

public sealed class InHouseAuthService(
    DsstatsContext context,
    IFido2 fido2,
    IMemoryCache memoryCache,
    IInHouseAccountNotifier accountNotifier,
    IOptions<InHouseAuthOptions> options) : IInHouseAuthService
{
    private readonly InHouseAuthOptions authOptions = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<InHouseAuthOptionsResponse> BeginRegistrationAsync(InHouseRegisterOptionsRequest request, CancellationToken token)
    {
        var displayName = NormalizeDisplayName(request.DisplayName);
        ValidateProfile(request.Profile);

        if (await ProfileExistsAsync(request.Profile.ToonId, token))
        {
            throw new InvalidOperationException("This player profile is already linked to another InHouse user.");
        }

        var publicId = Guid.NewGuid();
        var userHandle = CreateUserHandle(publicId);
        var fidoUser = new Fido2User
        {
            DisplayName = GetPasskeyDisplayName(displayName),
            Name = GetPasskeyName(displayName),
            Id = userHandle,
        };

        var credentialOptions = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = [],
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var challengeId = Guid.NewGuid();
        memoryCache.Set(
            ChallengeKey(challengeId),
            new PendingRegistration(credentialOptions, displayName, CloneProfile(request.Profile), publicId, WebEncoders.Base64UrlEncode(userHandle), string.Empty, null),
            authOptions.ChallengeLifetime);

        return ToOptionsResponse(challengeId, credentialOptions);
    }

    public async Task<InHouseSessionDto> CompleteRegistrationAsync(InHouseRegisterCompleteRequest request, CancellationToken token)
    {
        var pending = GetPending<PendingRegistration>(request.ChallengeId);
        await using var tx = await context.Database.BeginTransactionAsync(token);

        if (await ProfileExistsAsync(pending.Profile.ToonId, token))
        {
            throw new InvalidOperationException("This player profile is already linked to another InHouse user.");
        }

        var attestation = DeserializeCredential<AuthenticatorAttestationRawResponse>(request.Credential);
        var credentialResult = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestation,
            OriginalOptions = pending.Options,
            IsCredentialIdUniqueToUserCallback = async (args, _) =>
                !await context.InHousePasskeyCredentials.AnyAsync(c => c.CredentialId == WebEncoders.Base64UrlEncode(args.CredentialId), token),
        }, token);

        var user = new InHouseUser
        {
            PublicId = pending.PublicId,
            DisplayName = pending.DisplayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        user.Profiles.Add(ToEntity(pending.Profile));
        user.Passkeys.Add(ToCredential(credentialResult, pending.UserHandle, request.DeviceName));
        context.InHouseUsers.Add(user);
        await context.SaveChangesAsync(token);

        var session = await CreateSessionAsync(user, token);
        await tx.CommitAsync(token);
        memoryCache.Remove(ChallengeKey(request.ChallengeId));
        return session;
    }

    public Task<InHouseAuthOptionsResponse> BeginLoginAsync(InHouseLoginOptionsRequest request, CancellationToken token)
    {
        var assertionOptions = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Preferred,
        });

        var challengeId = Guid.NewGuid();
        memoryCache.Set(ChallengeKey(challengeId), new PendingAssertion(assertionOptions), authOptions.ChallengeLifetime);
        return Task.FromResult(ToOptionsResponse(challengeId, assertionOptions));
    }

    public async Task<InHouseSessionDto> CompleteLoginAsync(InHouseLoginCompleteRequest request, CancellationToken token)
    {
        var pending = GetPending<PendingAssertion>(request.ChallengeId);
        var assertion = DeserializeCredential<AuthenticatorAssertionRawResponse>(request.Credential);
        var credentialId = WebEncoders.Base64UrlEncode(assertion.RawId);

        var credential = await context.InHousePasskeyCredentials
            .Include(c => c.User)
            .ThenInclude(u => u!.Profiles)
            .Include(c => c.User)
            .ThenInclude(u => u!.Passkeys)
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, token)
            ?? throw new InvalidOperationException("Unknown passkey credential.");

        var assertionResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertion,
            OriginalOptions = pending.Options,
            StoredPublicKey = credential.PublicKey,
            StoredSignatureCounter = credential.SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
                Task.FromResult(WebEncoders.Base64UrlEncode(args.UserHandle) == credential.UserHandle
                    && WebEncoders.Base64UrlEncode(args.CredentialId) == credential.CredentialId),
        }, token);

        credential.SignatureCounter = assertionResult.SignCount;
        credential.IsBackedUp = assertionResult.IsBackedUp;
        credential.LastUsedAt = DateTime.UtcNow;
        credential.User!.LastLoginAt = DateTime.UtcNow;

        var session = await CreateSessionAsync(credential.User, token);
        await context.SaveChangesAsync(token);
        memoryCache.Remove(ChallengeKey(request.ChallengeId));
        return session;
    }

    public async Task<InHouseSessionDto> RefreshAsync(InHouseRefreshRequest request, CancellationToken token)
    {
        var refreshHash = HashToken(request.RefreshToken);
        var session = await context.InHouseSessions
            .Include(s => s.User)
            .ThenInclude(u => u!.Profiles)
            .Include(s => s.User)
            .ThenInclude(u => u!.Passkeys)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshHash, token)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (session.RevokedAt is not null || session.RefreshExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Refresh token expired or revoked.");
        }

        session.RevokedAt = DateTime.UtcNow;
        var nextSession = await CreateSessionAsync(session.User!, token);
        await context.SaveChangesAsync(token);
        return nextSession;
    }

    public async Task LogoutAsync(string? accessToken, InHouseRefreshRequest? request, CancellationToken token)
    {
        var hashes = new List<string>();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            hashes.Add(HashToken(accessToken));
        }

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            hashes.Add(HashToken(request.RefreshToken));
        }

        if (hashes.Count == 0)
        {
            return;
        }

        var sessions = await context.InHouseSessions
            .Where(s => hashes.Contains(s.AccessTokenHash) || hashes.Contains(s.RefreshTokenHash))
            .ToListAsync(token);

        foreach (var session in sessions)
        {
            session.RevokedAt ??= DateTime.UtcNow;
        }

        await context.SaveChangesAsync(token);
    }

    public async Task<InHouseUserDto?> GetCurrentUserAsync(int userId, CancellationToken token)
    {
        var user = await context.InHouseUsers
            .Include(u => u.Profiles)
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.InHouseUserId == userId, token);

        return user?.ToDto();
    }

    public async Task<InHouseUserDto> AddProfileAsync(int userId, InHouseProfileDto profile, CancellationToken token)
    {
        ValidateProfile(profile);

        var user = await context.InHouseUsers
            .Include(u => u.Profiles)
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        if (user.Profiles.Any(p => SameToonId(p.ToonId, profile.ToonId)))
        {
            throw new InvalidOperationException("This player profile is already linked to your account.");
        }

        if (await ProfileExistsAsync(profile.ToonId, token))
        {
            throw new InvalidOperationException("This player profile is already linked to another InHouse user.");
        }

        user.Profiles.Add(ToEntity(profile));
        await context.SaveChangesAsync(token);
        await accountNotifier.NotifyAccountChangedAsync(user.PublicId, InHouseAccountChangeReasons.Profiles);
        return user.ToDto();
    }

    public async Task<InHouseUserDto> RemoveProfileAsync(int userId, InHouseProfileDto profile, CancellationToken token)
    {
        var user = await context.InHouseUsers
            .Include(u => u.Profiles)
            .Include(u => u.Passkeys)
            .FirstOrDefaultAsync(u => u.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        if (user.Profiles.Count <= 1)
        {
            throw new InvalidOperationException("An InHouse account must keep at least one linked player profile.");
        }

        var linkedProfile = user.Profiles.FirstOrDefault(p => SameToonId(p.ToonId, profile.ToonId))
            ?? throw new InvalidOperationException("This player profile is not linked to your account.");

        user.Profiles.Remove(linkedProfile);
        context.InHouseProfiles.Remove(linkedProfile);
        await context.SaveChangesAsync(token);
        await accountNotifier.NotifyAccountChangedAsync(user.PublicId, InHouseAccountChangeReasons.Profiles);
        return user.ToDto();
    }

    public async Task<InHouseUserDto> RemovePasskeyAsync(int userId, int passkeyId, CancellationToken token)
    {
        await using var tx = await context.Database.BeginTransactionAsync(token);

        var user = await context.InHouseUsers
            .Include(u => u.Profiles)
            .Include(u => u.Passkeys)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        if (user.Passkeys.Count <= 1)
        {
            throw new InvalidOperationException("Add another passkey before removing your only sign-in method.");
        }

        var passkey = user.Passkeys.FirstOrDefault(p => p.InHousePasskeyCredentialId == passkeyId)
            ?? throw new InvalidOperationException("This passkey is not linked to your account.");

        context.InHousePasskeyCredentials.Remove(passkey);
        user.Passkeys.Remove(passkey);
        foreach (var session in user.Sessions)
        {
            session.RevokedAt ??= DateTime.UtcNow;
        }

        await context.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        return user.ToDto();
    }

    public async Task DeleteAccountAsync(int userId, CancellationToken token)
    {
        var user = await context.InHouseUsers.FirstOrDefaultAsync(u => u.InHouseUserId == userId, token)
            ?? throw new InvalidOperationException("Unknown InHouse user.");

        context.InHouseUsers.Remove(user);
        await context.SaveChangesAsync(token);
    }

    public async Task<InHouseDeviceLinkOptionsResponse> CreateDeviceLinkCodeAsync(int userId, CancellationToken token)
    {
        var userExists = await context.InHouseUsers.AnyAsync(u => u.InHouseUserId == userId, token);
        if (!userExists)
        {
            throw new InvalidOperationException("Unknown InHouse user.");
        }

        var code = CreateDisplayCode();
        var link = new InHouseDeviceLinkCode
        {
            InHouseUserId = userId,
            DisplayCode = code,
            CodeHash = HashToken(NormalizeCode(code)),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(authOptions.DeviceLinkLifetime),
        };

        context.InHouseDeviceLinkCodes.Add(link);
        await context.SaveChangesAsync(token);
        return new InHouseDeviceLinkOptionsResponse { Code = code, ExpiresAt = link.ExpiresAt };
    }

    public async Task<InHouseAuthOptionsResponse> BeginDeviceLinkAsync(InHouseDeviceLinkOptionsRequest request, CancellationToken token)
    {
        var codeHash = HashToken(NormalizeCode(request.Code));
        var link = await context.InHouseDeviceLinkCodes
            .Include(l => l.User)
            .ThenInclude(u => u!.Passkeys)
            .FirstOrDefaultAsync(l => l.CodeHash == codeHash, token)
            ?? throw new InvalidOperationException("Invalid link code.");

        if (link.UsedAt is not null || link.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Link code expired or already used.");
        }

        var user = link.User!;
        var userHandle = user.Passkeys.FirstOrDefault()?.UserHandle ?? WebEncoders.Base64UrlEncode(CreateUserHandle(user.PublicId));
        var fidoUser = new Fido2User
        {
            DisplayName = GetPasskeyDisplayName(user.DisplayName),
            Name = GetPasskeyName(user.DisplayName),
            Id = WebEncoders.Base64UrlDecode(userHandle),
        };

        var credentialOptions = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = user.Passkeys.Select(c => new PublicKeyCredentialDescriptor(WebEncoders.Base64UrlDecode(c.CredentialId))).ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var challengeId = Guid.NewGuid();
        memoryCache.Set(
            ChallengeKey(challengeId),
            new PendingRegistration(credentialOptions, user.DisplayName, new InHouseProfileDto(), user.PublicId, userHandle, request.DeviceName, link.InHouseDeviceLinkCodeId),
            authOptions.ChallengeLifetime);

        return ToOptionsResponse(challengeId, credentialOptions);
    }

    public async Task<InHouseSessionDto> CompleteDeviceLinkAsync(InHouseDeviceLinkCompleteRequest request, CancellationToken token)
    {
        var pending = GetPending<PendingRegistration>(request.ChallengeId);
        if (pending.DeviceLinkCodeId is null)
        {
            throw new InvalidOperationException("Challenge is not a device-link challenge.");
        }

        await using var tx = await context.Database.BeginTransactionAsync(token);
        var link = await context.InHouseDeviceLinkCodes
            .Include(l => l.User)
            .ThenInclude(u => u!.Profiles)
            .Include(l => l.User)
            .ThenInclude(u => u!.Passkeys)
            .FirstOrDefaultAsync(l => l.InHouseDeviceLinkCodeId == pending.DeviceLinkCodeId, token)
            ?? throw new InvalidOperationException("Invalid link code.");

        if (link.UsedAt is not null || link.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Link code expired or already used.");
        }

        var attestation = DeserializeCredential<AuthenticatorAttestationRawResponse>(request.Credential);
        var credentialResult = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestation,
            OriginalOptions = pending.Options,
            IsCredentialIdUniqueToUserCallback = async (args, _) =>
                !await context.InHousePasskeyCredentials.AnyAsync(c => c.CredentialId == WebEncoders.Base64UrlEncode(args.CredentialId), token),
        }, token);

        link.User!.Passkeys.Add(ToCredential(credentialResult, pending.UserHandle, pending.DeviceName));
        link.UsedAt = DateTime.UtcNow;
        link.User.LastLoginAt = DateTime.UtcNow;

        var session = await CreateSessionAsync(link.User, token);
        await context.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        memoryCache.Remove(ChallengeKey(request.ChallengeId));
        await accountNotifier.NotifyAccountChangedAsync(link.User.PublicId, InHouseAccountChangeReasons.Passkeys);
        return session;
    }

    public async Task<InHouseTokenValidationResult?> ValidateAccessTokenAsync(string accessToken, CancellationToken token)
    {
        var tokenHash = HashToken(accessToken);
        var session = await context.InHouseSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.AccessTokenHash == tokenHash, token);

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTime.UtcNow || session.User is null)
        {
            return null;
        }

        return new InHouseTokenValidationResult(session.User.InHouseUserId, session.User.PublicId, session.User.DisplayName, session.ExpiresAt);
    }

    private async Task<InHouseSessionDto> CreateSessionAsync(InHouseUser user, CancellationToken token)
    {
        var accessToken = CreateToken();
        var refreshToken = CreateToken();
        var session = new InHouseSession
        {
            User = user,
            AccessTokenHash = HashToken(accessToken),
            RefreshTokenHash = HashToken(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(authOptions.AccessTokenLifetime),
            RefreshExpiresAt = DateTime.UtcNow.Add(authOptions.RefreshTokenLifetime),
        };

        context.InHouseSessions.Add(session);
        await context.SaveChangesAsync(token);

        return new InHouseSessionDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = session.ExpiresAt,
            RefreshExpiresAt = session.RefreshExpiresAt,
            User = user.ToDto(),
        };
    }

    private static InHousePasskeyCredential ToCredential(RegisteredPublicKeyCredential credential, string userHandle, string deviceName)
        => new()
        {
            CredentialId = WebEncoders.Base64UrlEncode(credential.Id),
            UserHandle = userHandle,
            PublicKey = credential.PublicKey,
            SignatureCounter = credential.SignCount,
            IsBackedUp = credential.IsBackedUp,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Passkey" : deviceName[..Math.Min(deviceName.Length, 100)],
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        };

    private static InHouseProfile ToEntity(InHouseProfileDto profile)
        => new()
        {
            Name = profile.Name[..Math.Min(profile.Name.Length, 30)],
            Active = profile.Active,
            ToonId = new ToonId
            {
                Region = profile.ToonId.Region,
                Realm = profile.ToonId.Realm,
                Id = profile.ToonId.Id,
            },
            CreatedAt = DateTime.UtcNow,
        };

    private async Task<bool> ProfileExistsAsync(ToonIdDto toonId, CancellationToken token)
        => await context.InHouseProfiles.AnyAsync(p =>
            p.ToonId.Region == toonId.Region
            && p.ToonId.Realm == toonId.Realm
            && p.ToonId.Id == toonId.Id, token);

    private static bool SameToonId(ToonId left, ToonIdDto right)
        => left.Region == right.Region
           && left.Realm == right.Realm
           && left.Id == right.Id;

    private static T DeserializeCredential<T>(JsonElement credential)
        => JsonSerializer.Deserialize<T>(credential.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException("Invalid WebAuthn credential response.");

    private static InHouseAuthOptionsResponse ToOptionsResponse(Guid challengeId, object credentialOptions)
        => new()
        {
            ChallengeId = challengeId,
            Options = JsonSerializer.SerializeToElement(credentialOptions, credentialOptions.GetType(), JsonOptions),
        };

    private T GetPending<T>(Guid challengeId)
    {
        if (!memoryCache.TryGetValue(ChallengeKey(challengeId), out T? pending) || pending is null)
        {
            throw new InvalidOperationException("Challenge expired or invalid.");
        }

        return pending;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        displayName = displayName.Trim();
        if (displayName.Length is < 2 or > 40)
        {
            throw new InvalidOperationException("Display name must be between 2 and 40 characters.");
        }

        return displayName;
    }

    private static string GetPasskeyDisplayName(string displayName)
        => $"mydsstats InHouse - {displayName}";

    private static string GetPasskeyName(string displayName)
        => $"{displayName}@mydsstats";

    private static void ValidateProfile(InHouseProfileDto profile)
    {
        profile.Name = profile.Name.Trim();
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = "Player";
        }

        if (profile.ToonId.Id <= 0 || profile.ToonId.Region <= 0 || profile.ToonId.Realm < 0)
        {
            throw new InvalidOperationException("A valid player profile is required.");
        }
    }

    private static InHouseProfileDto CloneProfile(InHouseProfileDto profile)
        => new()
        {
            Name = profile.Name,
            Active = profile.Active,
            ToonId = new ToonIdDto
            {
                Region = profile.ToonId.Region,
                Realm = profile.ToonId.Realm,
                Id = profile.ToonId.Id,
            },
        };

    private static byte[] CreateUserHandle(Guid publicId)
        => Encoding.UTF8.GetBytes(publicId.ToString("N"));

    private static string CreateToken()
        => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string CreateDisplayCode()
    {
        Span<byte> bytes = stackalloc byte[5];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes[..4]) % 1_000_000;
        return $"{value:000000}";
    }

    private static string NormalizeCode(string code)
        => new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string ChallengeKey(Guid challengeId)
        => $"inhouse:challenge:{challengeId:N}";

    private sealed record PendingRegistration(
        CredentialCreateOptions Options,
        string DisplayName,
        InHouseProfileDto Profile,
        Guid PublicId,
        string UserHandle,
        string DeviceName,
        int? DeviceLinkCodeId);

    private sealed record PendingAssertion(AssertionOptions Options);
}

internal static class InHouseEntityMapping
{
    public static InHouseUserDto ToDto(this InHouseUser user)
        => new()
        {
            UserId = user.PublicId,
            DisplayName = user.DisplayName,
            Profiles = user.Profiles.Select(profile => new InHouseProfileDto
            {
                Name = profile.Name,
                Active = profile.Active,
                ToonId = new ToonIdDto
                {
                    Region = profile.ToonId.Region,
                    Realm = profile.ToonId.Realm,
                    Id = profile.ToonId.Id,
                },
            }).ToList(),
            Passkeys = user.Passkeys
                .OrderByDescending(passkey => passkey.LastUsedAt)
                .Select(passkey => new InHousePasskeyDto
                {
                    PasskeyId = passkey.InHousePasskeyCredentialId,
                    DeviceName = passkey.DeviceName,
                    CreatedAt = passkey.CreatedAt,
                    LastUsedAt = passkey.LastUsedAt,
                    IsBackedUp = passkey.IsBackedUp,
                }).ToList(),
        };
}
