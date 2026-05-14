using System.Security.Claims;
using dsstats.api.Controllers;
using dsstats.api.Hubs;
using dsstats.api.InHouse;
using dsstats.db;
using dsstats.dbServices;
using dsstats.dbServices.InHouse;
using dsstats.shared;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace dsstats.tests;

[TestClass]
public sealed class InHouseGameSessionServiceTests
{
    [TestMethod]
    public async Task CreateSessionAsync_AddsPublicActiveSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);

        var detail = await fixture.Service.CreateSessionAsync(
            user.InHouseUserId,
            new InHouseCreateGameSessionRequest { Name = "Friday IH" },
            CancellationToken.None);
        var active = await fixture.Service.GetActiveSessionsAsync(CancellationToken.None);

        Assert.AreEqual("Friday IH", detail.Name);
        Assert.AreEqual(user.PublicId, detail.CreatedByUserId);
        Assert.HasCount(1, active);
        Assert.AreEqual(detail.SessionId, active[0].SessionId);
    }

    [TestMethod]
    public async Task UploadReplayAsync_AttachesReplayOnceAndCountsPlayersAndObservers()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var request = new InHouseReplayUploadRequest
        {
            Replay = CreateReplay(),
            Observers =
            [
                new()
                {
                    Name = "Observer",
                    ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 99 },
                    SlotId = 7,
                }
            ],
        };

        await fixture.Service.UploadReplayAsync(session.SessionId, user.InHouseUserId, request, CancellationToken.None);
        var detail = await fixture.Service.UploadReplayAsync(session.SessionId, user.InHouseUserId, request, CancellationToken.None);

        Assert.HasCount(1, detail.Replays);
        Assert.HasCount(7, detail.Players);
        Assert.AreEqual(6, detail.Players.Count(player => player.Games == 1));
        var observer = detail.Players.Single(player => player.Name == "Observer");
        Assert.AreEqual(1, observer.Observes);
        Assert.AreEqual(0, observer.Games);
    }

    [TestMethod]
    public async Task GetSessionAsync_RefreshesSummaryWhenRatingsArriveLater()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        Assert.IsTrue(upload.Players.Where(player => player.Games > 0).All(player => player.RatingsPending));

        await fixture.AddReplayRatingAsync(upload.Replays[0].ReplayHash);
        var refreshed = await fixture.Service.GetSessionAsync(session.SessionId, user.InHouseUserId, CancellationToken.None);

        Assert.IsNotNull(refreshed);
        var winner = refreshed.Players.Single(player => player.ToonId.Id == 1);
        Assert.IsFalse(winner.RatingsPending);
        Assert.AreEqual(1000, winner.RatingStart);
        Assert.AreEqual(1010, winner.RatingEnd);
        Assert.AreEqual(10, winner.RatingDelta);
        Assert.AreEqual(10, winner.AverageGain);
    }

    [TestMethod]
    public async Task CloseSessionAsync_OnlyAllowsCreator()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var other = await fixture.AddUserAsync(2);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.CloseSessionAsync(session.SessionId, other.InHouseUserId, CancellationToken.None));
        var closed = await fixture.Service.CloseSessionAsync(session.SessionId, creator.InHouseUserId, CancellationToken.None);

        Assert.AreEqual("Only the session creator can close this InHouse session.", ex.Message);
        Assert.IsNotNull(closed.ClosedAt);
    }

    [TestMethod]
    public async Task GetSession_UnauthenticatedUserIsRejected()
    {
        var controller = new InHouseSessionsController(
            Mock.Of<IInHouseGameSessionService>(),
            Mock.Of<IHubContext<InHouseHub>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => controller.GetSession(Guid.NewGuid(), CancellationToken.None));

        Assert.AreEqual("You are not signed in.", ex.Message);
    }

    [TestMethod]
    public async Task CloseSession_ControllerCallsServiceForCurrentUser()
    {
        var sessionId = Guid.NewGuid();
        var service = new Mock<IInHouseGameSessionService>();
        service
            .Setup(s => s.CloseSessionAsync(sessionId, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InHouseGameSessionDetailDto { SessionId = sessionId });
        var hubContext = CreateHubContextMock();
        var controller = new InHouseSessionsController(service.Object, hubContext.Object)
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

        var result = await controller.CloseSession(sessionId, CancellationToken.None);

        Assert.IsInstanceOfType<ActionResult<InHouseGameSessionDetailDto>>(result);
        service.Verify(s => s.CloseSessionAsync(sessionId, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IHubContext<InHouseHub>> CreateHubContextMock()
    {
        var clientProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<InHouseHub>>();
        hubContext
            .SetupGet(c => c.Clients)
            .Returns(clients.Object);
        return hubContext;
    }

    private static ReplayDto CreateReplay()
    {
        return new()
        {
            Title = "Direct Strike TE",
            Version = "5.0.0",
            GameMode = GameMode.Standard,
            RegionId = 1,
            Gametime = new DateTime(2024, 1, 1, 20, 0, 0, DateTimeKind.Utc),
            BaseBuild = 1,
            Duration = 1200,
            WinnerTeam = 1,
            Players = Enumerable.Range(1, 6)
                .Select(i => new ReplayPlayerDto
                {
                    Name = $"Player {i}",
                    Race = i % 3 == 0 ? Commander.Zerg : i % 2 == 0 ? Commander.Terran : Commander.Protoss,
                    SelectedRace = Commander.None,
                    TeamId = i <= 3 ? 1 : 2,
                    GamePos = i,
                    Result = i <= 3 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = 1200,
                    Player = new PlayerDto
                    {
                        Name = $"Player {i}",
                        ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = i },
                    },
                })
                .ToList(),
        };
    }

    private sealed class InHouseGameSessionFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ServiceProvider serviceProvider;

        private InHouseGameSessionFixture(SqliteConnection connection, ServiceProvider serviceProvider)
        {
            this.connection = connection;
            this.serviceProvider = serviceProvider;
            Context = serviceProvider.GetRequiredService<DsstatsContext>();
            var importService = new ImportService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<ImportService>.Instance);
            Service = new InHouseGameSessionService(Context, importService);
        }

        public DsstatsContext Context { get; }
        public InHouseGameSessionService Service { get; }

        public static async Task<InHouseGameSessionFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddDbContext<DsstatsContext>(options => options.UseSqlite(connection));
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<DsstatsContext>();
            await context.Database.EnsureCreatedAsync();
            return new InHouseGameSessionFixture(connection, provider);
        }

        public async Task<InHouseUser> AddUserAsync(int seed)
        {
            var user = new InHouseUser
            {
                PublicId = Guid.NewGuid(),
                DisplayName = $"User {seed}",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            user.Passkeys.Add(new InHousePasskeyCredential
            {
                CredentialId = $"credential-{seed}",
                UserHandle = $"handle-{seed}",
                PublicKey = [1, 2, 3],
                SignatureCounter = 1,
                DeviceName = "Browser",
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
            });
            user.Profiles.Add(new InHouseProfile
            {
                Name = $"User {seed}",
                ToonId = new ToonId { Region = 1, Realm = 1, Id = seed },
            });
            Context.InHouseUsers.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        public async Task AddReplayRatingAsync(string replayHash)
        {
            var replay = await Context.Replays
                .Include(replay => replay.Players)
                .FirstAsync(replay => replay.ReplayHash == replayHash);

            replay.Ratings.Add(new ReplayRating
            {
                RatingType = RatingType.StandardTE,
                LeaverType = LeaverType.None,
                ExpectedWinProbability = 0.55,
                IsPreRating = true,
                AvgRating = 1025,
                ReplayPlayerRatings = replay.Players
                    .OrderBy(player => player.GamePos)
                    .Select((player, index) => new ReplayPlayerRating
                    {
                        RatingType = RatingType.StandardTE,
                        ReplayPlayerId = player.ReplayPlayerId,
                        PlayerId = player.PlayerId,
                        RatingBefore = 1000 + index * 10,
                        RatingDelta = player.TeamId == 1 ? 10 : -10,
                        Games = 20,
                    })
                    .ToList(),
            });

            await Context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await serviceProvider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
