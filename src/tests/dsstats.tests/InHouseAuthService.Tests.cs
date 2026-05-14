using System.Security.Claims;
using System.Reflection;
using dsstats.api.Controllers;
using dsstats.api.Hubs;
using dsstats.api.InHouse;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.InHouse;
using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.RateLimiting;
using Moq;

namespace dsstats.tests;

[TestClass]
public sealed class InHouseAuthServiceTests
{
    [TestMethod]
    public async Task GetCurrentUserAsync_IncludesPasskeyMetadata()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 1);
        var passkey = user.Passkeys.Single();
        passkey.DeviceName = "Laptop";
        passkey.CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        passkey.LastUsedAt = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc);
        passkey.IsBackedUp = true;

        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();

        var dto = await fixture.Service.GetCurrentUserAsync(user.InHouseUserId, CancellationToken.None);

        Assert.IsNotNull(dto);
        Assert.HasCount(1, dto.Passkeys);
        Assert.AreEqual(passkey.InHousePasskeyCredentialId, dto.Passkeys[0].PasskeyId);
        Assert.AreEqual("Laptop", dto.Passkeys[0].DeviceName);
        Assert.AreEqual(passkey.CreatedAt, dto.Passkeys[0].CreatedAt);
        Assert.AreEqual(passkey.LastUsedAt, dto.Passkeys[0].LastUsedAt);
        Assert.IsTrue(dto.Passkeys[0].IsBackedUp);
    }

    [TestMethod]
    public async Task RemovePasskeyAsync_RemovesOwnedPasskey()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 2);
        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();
        var removedPasskeyId = user.Passkeys.First().InHousePasskeyCredentialId;

        var dto = await fixture.Service.RemovePasskeyAsync(user.InHouseUserId, removedPasskeyId, CancellationToken.None);

        Assert.HasCount(1, dto.Passkeys);
        Assert.IsFalse(await fixture.Context.InHousePasskeyCredentials.AnyAsync(p => p.InHousePasskeyCredentialId == removedPasskeyId));
        Assert.AreEqual(1, await fixture.Context.InHousePasskeyCredentials.CountAsync(p => p.InHouseUserId == user.InHouseUserId));
    }

    [TestMethod]
    public async Task RemovePasskeyAsync_BlocksFinalPasskey()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 1);
        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();
        var passkeyId = user.Passkeys.Single().InHousePasskeyCredentialId;

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.RemovePasskeyAsync(user.InHouseUserId, passkeyId, CancellationToken.None));

        Assert.AreEqual("Add another passkey before removing your only sign-in method.", ex.Message);
        Assert.AreEqual(1, await fixture.Context.InHousePasskeyCredentials.CountAsync(p => p.InHouseUserId == user.InHouseUserId));
    }

    [TestMethod]
    public async Task RemovePasskeyAsync_RejectsAnotherUsersPasskey()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 2);
        var otherUser = CreateUser(2, passkeyCount: 1);
        fixture.Context.InHouseUsers.AddRange(user, otherUser);
        await fixture.Context.SaveChangesAsync();
        var otherPasskeyId = otherUser.Passkeys.Single().InHousePasskeyCredentialId;

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.RemovePasskeyAsync(user.InHouseUserId, otherPasskeyId, CancellationToken.None));

        Assert.AreEqual("This passkey is not linked to your account.", ex.Message);
        Assert.IsTrue(await fixture.Context.InHousePasskeyCredentials.AnyAsync(p => p.InHousePasskeyCredentialId == otherPasskeyId));
    }

    [TestMethod]
    public async Task RemovePasskeyAsync_RevokesAllUserSessions()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 2, sessionCount: 2);
        var otherUser = CreateUser(2, passkeyCount: 1, sessionCount: 1);
        fixture.Context.InHouseUsers.AddRange(user, otherUser);
        await fixture.Context.SaveChangesAsync();
        var removedPasskeyId = user.Passkeys.First().InHousePasskeyCredentialId;

        await fixture.Service.RemovePasskeyAsync(user.InHouseUserId, removedPasskeyId, CancellationToken.None);

        var userSessions = await fixture.Context.InHouseSessions
            .Where(s => s.InHouseUserId == user.InHouseUserId)
            .ToListAsync();
        var otherSession = await fixture.Context.InHouseSessions
            .SingleAsync(s => s.InHouseUserId == otherUser.InHouseUserId);
        Assert.IsTrue(userSessions.All(s => s.RevokedAt is not null));
        Assert.IsNull(otherSession.RevokedAt);
    }

    [TestMethod]
    public async Task RemovePasskeyAsync_DoesNotNotifyBecauseAllSessionsAreRevoked()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 2, sessionCount: 1);
        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();
        var removedPasskeyId = user.Passkeys.First().InHousePasskeyCredentialId;

        await fixture.Service.RemovePasskeyAsync(user.InHouseUserId, removedPasskeyId, CancellationToken.None);

        fixture.AccountNotifier.Verify(
            n => n.NotifyAccountChangedAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddProfileAsync_NotifiesAccountProfileChange()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 1);
        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.AddProfileAsync(user.InHouseUserId, new InHouseProfileDto
        {
            Name = "Second profile",
            ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 101 },
        }, CancellationToken.None);

        fixture.AccountNotifier.Verify(
            n => n.NotifyAccountChangedAsync(user.PublicId, InHouseAccountChangeReasons.Profiles),
            Times.Once);
    }

    [TestMethod]
    public async Task RemoveProfileAsync_NotifiesAccountProfileChange()
    {
        await using var fixture = await InHouseAuthFixture.CreateAsync();
        var user = CreateUser(1, passkeyCount: 1);
        user.Profiles.Add(new InHouseProfile
        {
            Name = "Second profile",
            Active = true,
            ToonId = new ToonId { Region = 1, Realm = 1, Id = 101 },
            CreatedAt = DateTime.UtcNow,
        });
        fixture.Context.InHouseUsers.Add(user);
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.RemoveProfileAsync(user.InHouseUserId, new InHouseProfileDto
        {
            Name = "Second profile",
            ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 101 },
        }, CancellationToken.None);

        fixture.AccountNotifier.Verify(
            n => n.NotifyAccountChangedAsync(user.PublicId, InHouseAccountChangeReasons.Profiles),
            Times.Once);
    }

    [TestMethod]
    public async Task InHouseAccountNotifier_SendsAccountChangedToAccountGroup()
    {
        var publicUserId = Guid.NewGuid();
        var clientProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        var hubContext = new Mock<IHubContext<InHouseHub>>();
        clients
            .Setup(c => c.Group(InHouseHub.GetAccountGroupName(publicUserId)))
            .Returns(clientProxy.Object);
        hubContext
            .SetupGet(c => c.Clients)
            .Returns(clients.Object);
        var notifier = new InHouseAccountNotifier(hubContext.Object);

        await notifier.NotifyAccountChangedAsync(publicUserId, InHouseAccountChangeReasons.Passkeys);

        clientProxy.Verify(
            p => p.SendCoreAsync(
                InHouseHub.AccountChangedEvent,
                It.Is<object?[]>(args => args.Length == 1
                    && (string)args[0]! == InHouseAccountChangeReasons.Passkeys),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RemovePasskey_CallsServiceForCurrentUser()
    {
        var authService = new Mock<IInHouseAuthService>();
        authService
            .Setup(s => s.RemovePasskeyAsync(42, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InHouseUserDto());
        var controller = new AuthController(authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(InHouseClaims.UserId, "42"),
                    ])),
                },
            },
        };

        var result = await controller.RemovePasskey(new InHouseRemovePasskeyRequest { PasskeyId = 7 }, CancellationToken.None);

        Assert.IsInstanceOfType<ActionResult<InHouseUserDto>>(result);
        authService.Verify(s => s.RemovePasskeyAsync(42, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void DeviceLinkOptions_UsesAttemptRateLimitPolicy()
        => AssertRateLimitPolicy(nameof(AuthController.DeviceLinkOptions), "inhouse-device-link-attempt");

    [TestMethod]
    public void DeviceLinkComplete_UsesAttemptRateLimitPolicy()
        => AssertRateLimitPolicy(nameof(AuthController.DeviceLinkComplete), "inhouse-device-link-attempt");

    [TestMethod]
    public void DeviceLinkCode_UsesCreateRateLimitPolicy()
        => AssertRateLimitPolicy(nameof(AuthController.DeviceLinkCode), "inhouse-device-link-create");

    private static InHouseUser CreateUser(int seed, int passkeyCount, int sessionCount = 0)
    {
        var user = new InHouseUser
        {
            PublicId = Guid.NewGuid(),
            DisplayName = $"Player {seed}",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        user.Profiles.Add(new InHouseProfile
        {
            Name = $"Player {seed}",
            Active = true,
            ToonId = new ToonId
            {
                Region = 1,
                Realm = 1,
                Id = seed,
            },
            CreatedAt = DateTime.UtcNow,
        });

        for (var i = 1; i <= passkeyCount; i++)
        {
            user.Passkeys.Add(new InHousePasskeyCredential
            {
                CredentialId = $"credential-{seed}-{i}",
                UserHandle = $"handle-{seed}",
                PublicKey = [1, 2, 3],
                SignatureCounter = 1,
                DeviceName = $"Device {i}",
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                LastUsedAt = DateTime.UtcNow.AddHours(-i),
            });
        }

        for (var i = 1; i <= sessionCount; i++)
        {
            user.Sessions.Add(new InHouseSession
            {
                AccessTokenHash = $"access-{seed}-{i}",
                RefreshTokenHash = $"refresh-{seed}-{i}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(20),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            });
        }

        return user;
    }

    private static void AssertRateLimitPolicy(string actionName, string policyName)
    {
        var method = typeof(AuthController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Missing action {actionName}.");
        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.IsNotNull(attribute);
        Assert.AreEqual(policyName, attribute.PolicyName);
    }

    private sealed class InHouseAuthFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly MemoryCache memoryCache = new(new MemoryCacheOptions());

        private InHouseAuthFixture(SqliteConnection connection, DsstatsContext context, Mock<IInHouseAccountNotifier> accountNotifier)
        {
            this.connection = connection;
            Context = context;
            AccountNotifier = accountNotifier;
            Service = new InHouseAuthService(
                context,
                Mock.Of<IFido2>(),
                memoryCache,
                accountNotifier.Object,
                Options.Create(new InHouseAuthOptions()));
        }

        public DsstatsContext Context { get; }
        public Mock<IInHouseAccountNotifier> AccountNotifier { get; }
        public InHouseAuthService Service { get; }

        public static async Task<InHouseAuthFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite(connection)
                .Options;
            var context = new DsstatsContext(options);
            await context.Database.EnsureCreatedAsync();
            return new InHouseAuthFixture(connection, context, new Mock<IInHouseAccountNotifier>());
        }

        public async ValueTask DisposeAsync()
        {
            memoryCache.Dispose();
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
