using System.Security.Claims;
using dsstats.db;
using dsstats.dbServices;
using dsstats.dbServices.InHouse;
using dsstats.shared;
using dsstats.shared.InHouse;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace dsstats.tests;

[TestClass]
public sealed class InHouseGameSessionServiceTests
{
    [TestMethod]
    public async Task CreateSessionAsync_PersistsSimplifiedSessionAndSnapshot()
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
        Assert.AreEqual(1, await fixture.Context.InHouseGameSessions.CountAsync());
        Assert.AreEqual(1, await fixture.Context.InHouseGameSessionStateSnapshots.CountAsync());
    }

    [TestMethod]
    public async Task GetSessionAsync_RestoresActiveSessionFromSnapshot()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var created = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        await fixture.Service.UploadReplayAsync(
            created.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        var restoredService = fixture.CreateService();
        var restored = await restoredService.GetSessionAsync(created.SessionId, user.InHouseUserId, CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.AreEqual(created.SessionId, restored.SessionId);
        Assert.HasCount(1, restored.Replays);
        Assert.HasCount(6, restored.RosterPlayers);
    }

    [TestMethod]
    public async Task UploadReplayAsync_AttachesReplayOnceAndPersistsObservers()
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

        var first = await fixture.Service.UploadReplayAsync(session.SessionId, user.InHouseUserId, request, CancellationToken.None);
        var second = await fixture.Service.UploadReplayAsync(session.SessionId, user.InHouseUserId, request, CancellationToken.None);

        Assert.IsTrue(first.Changed);
        Assert.IsFalse(second.Changed);
        Assert.HasCount(1, second.State.Replays);
        Assert.HasCount(7, second.State.Players);
        Assert.HasCount(7, second.State.RosterPlayers);
        Assert.AreEqual(6, second.State.Players.Count(player => player.Games == 1));
        var observer = second.State.Players.Single(player => player.Name == "Observer");
        Assert.AreEqual(1, observer.Observes);
        Assert.AreEqual(0, observer.Games);
        Assert.AreEqual(1, await fixture.Context.ReplayObservers.CountAsync());
        Assert.AreEqual(1, fixture.Context.InHouseGameSessions.Single().ReplayIds.Length);
    }

    [TestMethod]
    public async Task UploadReplayAsync_DoesNotPersistReplayObserversWhenObserverListIsEmpty()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);

        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        Assert.IsTrue(upload.Changed);
        Assert.HasCount(1, upload.State.Replays);
        Assert.AreEqual(0, await fixture.Context.ReplayObservers.CountAsync());
    }

    [TestMethod]
    public async Task RemoveReplayAsync_RejectsNonCreatorNonAdmin()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var other = await fixture.AddUserAsync(2);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => fixture.Service.RemoveReplayAsync(
                session.SessionId,
                upload.State.Replays[0].ReplayHash,
                CreatePrincipal(other),
                CancellationToken.None));

        Assert.AreEqual("Only the session creator or an InHouse admin can delete replays from this session.", ex.Message);
        Assert.AreEqual(1, fixture.Context.InHouseGameSessions.Single().ReplayIds.Length);
    }

    [TestMethod]
    public async Task RemoveReplayAsync_CreatorRemovesReplayAndCanUploadItAgain()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        var manual = await fixture.Service.AddRosterPlayerAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseRosterPlayerUpsertRequest
            {
                Name = "Manual",
                ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 900 },
                InitialRating = 1234,
            },
            CancellationToken.None);
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
                },
            ],
        };
        var upload = await fixture.Service.UploadReplayAsync(session.SessionId, creator.InHouseUserId, request, CancellationToken.None);
        await fixture.AddReplayRatingAsync(upload.State.Replays[0].ReplayHash);
        var refreshed = await fixture.Service.GetSessionAsync(session.SessionId, creator.InHouseUserId, CancellationToken.None);
        Assert.IsNotNull(refreshed);

        var removed = await fixture.Service.RemoveReplayAsync(
            session.SessionId,
            refreshed.Replays[0].ReplayHash,
            CreatePrincipal(creator),
            CancellationToken.None);
        var active = await fixture.Service.GetActiveSessionsAsync(CancellationToken.None);

        Assert.HasCount(0, removed.Replays);
        Assert.HasCount(0, removed.Players);
        Assert.HasCount(1, removed.RosterPlayers);
        Assert.AreEqual(manual.RosterPlayers[0].RosterPlayerId, removed.RosterPlayers[0].RosterPlayerId);
        Assert.AreEqual(0, active[0].Games);
        Assert.AreEqual(0, fixture.Context.InHouseGameSessions.Single().ReplayIds.Length);
        Assert.AreEqual(1, await fixture.Context.Replays.CountAsync());
        Assert.AreEqual(1, await fixture.Context.ReplayObservers.CountAsync());

        var uploadedAgain = await fixture.Service.UploadReplayAsync(session.SessionId, creator.InHouseUserId, request, CancellationToken.None);
        Assert.IsTrue(uploadedAgain.Changed);
        Assert.HasCount(1, uploadedAgain.State.Replays);
        Assert.AreEqual(1, await fixture.Context.InHouseGameSessions
            .AsNoTracking()
            .Select(dbSession => dbSession.ReplayIds.Length)
            .SingleAsync());
    }

    [TestMethod]
    public async Task RemoveReplayAsync_AdminRemovesReplayFromOtherCreatorsSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var admin = await fixture.AddUserAsync(2);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        var removed = await fixture.Service.RemoveReplayAsync(
            session.SessionId,
            upload.State.Replays[0].ReplayHash,
            CreatePrincipal(admin, isAdmin: true),
            CancellationToken.None);

        Assert.HasCount(0, removed.Replays);
        Assert.AreEqual(0, fixture.Context.InHouseGameSessions.Single().ReplayIds.Length);
    }

    [TestMethod]
    public async Task RemoveReplayAsync_RejectsClosedSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);
        await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(creator), CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.RemoveReplayAsync(
                session.SessionId,
                upload.State.Replays[0].ReplayHash,
                CreatePrincipal(creator),
                CancellationToken.None));

        Assert.AreEqual("This InHouse session is closed.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveReplayAsync_RebuildsSummariesFromRemainingReplays()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        var first = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);
        var secondReplay = CreateReplay(new DateTime(2024, 1, 2, 20, 0, 0, DateTimeKind.Utc));
        secondReplay.Players[0].Name = "Replacement";
        secondReplay.Players[0].Player.Name = "Replacement";
        secondReplay.Players[0].Player.ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 101 };
        var second = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            creator.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = secondReplay },
            CancellationToken.None);
        await fixture.AddReplayRatingAsync(second.State.Replays[0].ReplayHash);
        var refreshed = await fixture.Service.GetSessionAsync(session.SessionId, creator.InHouseUserId, CancellationToken.None);
        Assert.IsNotNull(refreshed);
        Assert.AreEqual(2, refreshed.Players[0].Games);

        var removed = await fixture.Service.RemoveReplayAsync(
            session.SessionId,
            first.State.Replays[0].ReplayHash,
            CreatePrincipal(creator),
            CancellationToken.None);

        Assert.HasCount(1, removed.Replays);
        Assert.IsTrue(removed.Players.All(player => player.Games == 1));
        Assert.IsTrue(removed.Players.All(player => !player.RatingsPending));
        Assert.IsFalse(removed.Replays[0].RatingsPending);
        Assert.AreEqual(1025, removed.Replays[0].AvgRating);
        Assert.AreEqual(1, fixture.Context.InHouseGameSessions.Single().ReplayIds.Length);
    }

    [TestMethod]
    public async Task ManualRosterChanges_AreMemoryOnlyUntilCloseOrReplayUpload()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var snapshotBefore = await fixture.Context.InHouseGameSessionStateSnapshots
            .Select(snapshot => snapshot.Json)
            .SingleAsync();

        var added = await fixture.Service.AddRosterPlayerAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseRosterPlayerUpsertRequest
            {
                Name = "Manual",
                ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 900 },
                InitialRating = 1234,
            },
            CancellationToken.None);
        var snapshotAfter = await fixture.Context.InHouseGameSessionStateSnapshots
            .Select(snapshot => snapshot.Json)
            .SingleAsync();
        var restored = await fixture.CreateService().GetSessionAsync(session.SessionId, user.InHouseUserId, CancellationToken.None);

        Assert.HasCount(1, added.RosterPlayers);
        Assert.AreEqual(snapshotBefore, snapshotAfter);
        Assert.IsNotNull(restored);
        Assert.HasCount(0, restored.RosterPlayers);
    }

    [TestMethod]
    public async Task SitterToggle_UpdatesMemoryAndDoesNotPersistSnapshot()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var added = await fixture.Service.AddRosterPlayerAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseRosterPlayerUpsertRequest
            {
                Name = "Manual",
                ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 900 },
            },
            CancellationToken.None);
        var snapshotBefore = await fixture.Context.InHouseGameSessionStateSnapshots
            .Select(snapshot => snapshot.Json)
            .SingleAsync();

        var toggled = await fixture.Service.SetRosterPlayerSitterAsync(
            session.SessionId,
            added.RosterPlayers[0].RosterPlayerId,
            user.InHouseUserId,
            true,
            CancellationToken.None);
        var snapshotAfter = await fixture.Context.InHouseGameSessionStateSnapshots
            .Select(snapshot => snapshot.Json)
            .SingleAsync();

        Assert.IsTrue(toggled.RosterPlayers.Single().IsSitter);
        Assert.AreEqual(snapshotBefore, snapshotAfter);
    }

    [TestMethod]
    public async Task CloseSessionAsync_OnlyAllowsCreatorAndPersistsClosedAt()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var other = await fixture.AddUserAsync(2);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(other), CancellationToken.None));
        var closed = await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(creator), CancellationToken.None);

        Assert.AreEqual("Only the session creator or an InHouse admin can close this session.", ex.Message);
        Assert.IsNotNull(closed.ClosedAt);
        Assert.IsNotNull(await fixture.Context.InHouseGameSessions
            .Where(dbSession => dbSession.PublicId == session.SessionId)
            .Select(dbSession => dbSession.ClosedAt)
            .SingleAsync());
        Assert.HasCount(0, await fixture.Service.GetActiveSessionsAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task CloseSessionAsync_AllowsAdminWhoIsNotCreator()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var admin = await fixture.AddUserAsync(2);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);

        var closed = await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(admin, isAdmin: true), CancellationToken.None);

        Assert.IsNotNull(closed.ClosedAt);
        Assert.IsNotNull(await fixture.Context.InHouseGameSessions
            .Where(dbSession => dbSession.PublicId == session.SessionId)
            .Select(dbSession => dbSession.ClosedAt)
            .SingleAsync());
    }

    [TestMethod]
    public async Task CloseSessionAsync_RejectsAlreadyClosedSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var creator = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(creator.InHouseUserId, new(), CancellationToken.None);
        await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(creator), CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(creator), CancellationToken.None));

        Assert.AreEqual("This InHouse session is closed.", ex.Message);
    }

    [TestMethod]
    public async Task CloseInactiveSessionsAsync_ClosesAndRemovesFromActiveList()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);

        var closed = await fixture.Service.CloseInactiveSessionsAsync(TimeSpan.Zero, CancellationToken.None);
        var active = await fixture.Service.GetActiveSessionsAsync(CancellationToken.None);

        Assert.HasCount(1, closed);
        Assert.AreEqual(session.SessionId, closed[0].SessionId);
        Assert.HasCount(0, active);
    }

    [TestMethod]
    public async Task DeleteSessionAsync_RejectsNonAdmin()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);

        var ex = await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => fixture.Service.DeleteSessionAsync(session.SessionId, CreatePrincipal(user), CancellationToken.None));

        Assert.AreEqual("Only InHouse admins can delete game sessions.", ex.Message);
        Assert.AreEqual(1, await fixture.Context.InHouseGameSessions.CountAsync());
    }

    [TestMethod]
    public async Task DeleteSessionAsync_AdminDeletesActiveSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var admin = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(admin.InHouseUserId, new(), CancellationToken.None);

        await fixture.Service.DeleteSessionAsync(session.SessionId, CreatePrincipal(admin, isAdmin: true), CancellationToken.None);
        var active = await fixture.Service.GetActiveSessionsAsync(CancellationToken.None);
        var restored = await fixture.Service.GetSessionAsync(session.SessionId, admin.InHouseUserId, CancellationToken.None);

        Assert.HasCount(0, active);
        Assert.IsNull(restored);
        Assert.AreEqual(0, await fixture.Context.InHouseGameSessions.CountAsync());
        Assert.AreEqual(0, await fixture.Context.InHouseGameSessionStateSnapshots.CountAsync());
    }

    [TestMethod]
    public async Task DeleteSessionAsync_AdminDeletesClosedSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var admin = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(admin.InHouseUserId, new(), CancellationToken.None);
        await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(admin), CancellationToken.None);

        await fixture.Service.DeleteSessionAsync(session.SessionId, CreatePrincipal(admin, isAdmin: true), CancellationToken.None);
        var closed = await fixture.Service.GetClosedSessionsAsync(new InHouseClosedGameSessionsRequest(), CancellationToken.None);

        Assert.AreEqual(0, closed.Total);
        Assert.HasCount(0, closed.Items);
        Assert.AreEqual(0, await fixture.Context.InHouseGameSessions.CountAsync());
    }

    [TestMethod]
    public async Task DeleteSessionAsync_KeepsImportedReplaysAndObservers()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var admin = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(admin.InHouseUserId, new(), CancellationToken.None);
        await fixture.Service.UploadReplayAsync(
            session.SessionId,
            admin.InHouseUserId,
            new InHouseReplayUploadRequest
            {
                Replay = CreateReplay(),
                Observers =
                [
                    new()
                    {
                        Name = "Observer",
                        ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 99 },
                        SlotId = 7,
                    },
                ],
            },
            CancellationToken.None);

        await fixture.Service.DeleteSessionAsync(session.SessionId, CreatePrincipal(admin, isAdmin: true), CancellationToken.None);

        Assert.AreEqual(0, await fixture.Context.InHouseGameSessions.CountAsync());
        Assert.AreEqual(1, await fixture.Context.Replays.CountAsync());
        Assert.AreEqual(1, await fixture.Context.ReplayObservers.CountAsync());
    }

    [TestMethod]
    public async Task GetClosedSessionsAsync_ReturnsPagedClosedSessionsNewestFirst()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var first = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new InHouseCreateGameSessionRequest { Name = "First" }, CancellationToken.None);
        await fixture.Service.CloseSessionAsync(first.SessionId, CreatePrincipal(user), CancellationToken.None);
        var second = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new InHouseCreateGameSessionRequest { Name = "Second" }, CancellationToken.None);
        await fixture.Service.CloseSessionAsync(second.SessionId, CreatePrincipal(user), CancellationToken.None);
        var third = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new InHouseCreateGameSessionRequest { Name = "Third" }, CancellationToken.None);
        await fixture.Service.CloseSessionAsync(third.SessionId, CreatePrincipal(user), CancellationToken.None);

        var closedAt = new Dictionary<Guid, DateTime>
        {
            [first.SessionId] = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            [second.SessionId] = new(2024, 1, 3, 12, 0, 0, DateTimeKind.Utc),
            [third.SessionId] = new(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc),
        };
        foreach (var dbSession in fixture.Context.InHouseGameSessions)
        {
            dbSession.ClosedAt = closedAt[dbSession.PublicId];
        }
        await fixture.Context.SaveChangesAsync();

        var page = await fixture.Service.GetClosedSessionsAsync(
            new InHouseClosedGameSessionsRequest { Page = 1, PageSize = 2 },
            CancellationToken.None);

        Assert.AreEqual(3, page.Total);
        Assert.AreEqual(1, page.Page);
        Assert.AreEqual(2, page.PageSize);
        Assert.HasCount(2, page.Items);
        Assert.AreEqual(second.SessionId, page.Items[0].SessionId);
        Assert.AreEqual(third.SessionId, page.Items[1].SessionId);
    }

    [TestMethod]
    public async Task GetClosedSessionsAsync_NormalizesPagingAndExcludesActiveSessions()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var closed = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new InHouseCreateGameSessionRequest { Name = "Closed" }, CancellationToken.None);
        await fixture.Service.CloseSessionAsync(closed.SessionId, CreatePrincipal(user), CancellationToken.None);
        await fixture.Service.CreateSessionAsync(user.InHouseUserId, new InHouseCreateGameSessionRequest { Name = "Active" }, CancellationToken.None);

        var page = await fixture.Service.GetClosedSessionsAsync(
            new InHouseClosedGameSessionsRequest { Page = -3, PageSize = 500 },
            CancellationToken.None);

        Assert.AreEqual(1, page.Page);
        Assert.AreEqual(50, page.PageSize);
        Assert.AreEqual(1, page.Total);
        Assert.HasCount(1, page.Items);
        Assert.AreEqual(closed.SessionId, page.Items[0].SessionId);
    }

    [TestMethod]
    public async Task GetClosedSessionAsync_ReturnsPublicClosedSessionDetail()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(
            user.InHouseUserId,
            new InHouseCreateGameSessionRequest { Name = "Closed detail" },
            CancellationToken.None);
        await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);
        await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(user), CancellationToken.None);

        var detail = await fixture.CreateService().GetClosedSessionAsync(session.SessionId, CancellationToken.None);

        Assert.IsNotNull(detail);
        Assert.AreEqual(session.SessionId, detail.SessionId);
        Assert.AreEqual("Closed detail", detail.Name);
        Assert.AreEqual(user.PublicId, detail.CreatedByUserId);
        Assert.AreEqual(user.DisplayName, detail.CreatedByDisplayName);
        Assert.HasCount(6, detail.Players);
        Assert.HasCount(1, detail.Replays);
        Assert.IsTrue(detail.ClosedAt > DateTime.MinValue);
    }

    [TestMethod]
    public async Task GetClosedSessionAsync_ReturnsNullForActiveOrUnknownSession()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var active = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);

        var activeDetail = await fixture.Service.GetClosedSessionAsync(active.SessionId, CancellationToken.None);
        var unknownDetail = await fixture.Service.GetClosedSessionAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(activeDetail);
        Assert.IsNull(unknownDetail);
    }

    [TestMethod]
    public async Task GetSessionAsync_RestoresClosedSessionDetailWithoutActiveListCaching()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);
        await fixture.Service.CloseSessionAsync(session.SessionId, CreatePrincipal(user), CancellationToken.None);

        var restored = await fixture.CreateService().GetSessionAsync(session.SessionId, user.InHouseUserId, CancellationToken.None);
        var active = await fixture.CreateService().GetActiveSessionsAsync(CancellationToken.None);

        Assert.IsNotNull(restored);
        Assert.IsNotNull(restored.ClosedAt);
        Assert.HasCount(1, restored.Replays);
        Assert.HasCount(6, restored.Players);
        Assert.HasCount(0, active);
    }

    [TestMethod]
    public async Task CreateSessionAsync_ReusesExistingEmptyActiveSessionForCreator()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);

        var first = await fixture.Service.CreateSessionAsync(
            user.InHouseUserId,
            new InHouseCreateGameSessionRequest { Name = "Original" },
            CancellationToken.None);
        var second = await fixture.Service.CreateSessionAsync(
            user.InHouseUserId,
            new InHouseCreateGameSessionRequest { Name = "Duplicate" },
            CancellationToken.None);

        Assert.AreEqual(first.SessionId, second.SessionId);
        Assert.AreEqual("Original", second.Name);
        Assert.AreEqual(1, await fixture.Context.InHouseGameSessions.CountAsync());
    }

    [TestMethod]
    public async Task GetSessionAsync_RefreshesPendingRatingsWhenRatingsArriveLater()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var upload = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = CreateReplay() },
            CancellationToken.None);

        Assert.IsTrue(upload.State.Players.Where(player => player.Games > 0).All(player => player.RatingsPending));
        Assert.AreEqual(1000, upload.State.RosterPlayers.Single(player => player.ToonId.Id == 2).InitialRating);

        await fixture.AddReplayRatingAsync(upload.State.Replays[0].ReplayHash);
        var refreshed = await fixture.Service.GetSessionAsync(session.SessionId, user.InHouseUserId, CancellationToken.None);

        Assert.IsNotNull(refreshed);
        var winner = refreshed.Players.Single(player => player.ToonId.Id == 1);
        Assert.IsFalse(winner.RatingsPending);
        Assert.AreEqual(1000, winner.RatingStart);
        Assert.AreEqual(1010, winner.RatingEnd);
        Assert.AreEqual(10, winner.RatingDelta);
        Assert.AreEqual(10, winner.AverageGain);
        Assert.AreEqual(1010, refreshed.RosterPlayers.Single(player => player.ToonId.Id == 2).InitialRating);
    }

    [TestMethod]
    public async Task GetSessionAsync_UpdatesObserverRosterRatingWhenPlayerLaterPlays()
    {
        await using var fixture = await InHouseGameSessionFixture.CreateAsync();
        var user = await fixture.AddUserAsync(1);
        var session = await fixture.Service.CreateSessionAsync(user.InHouseUserId, new(), CancellationToken.None);
        var first = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest
            {
                Replay = CreateReplay(),
                Observers =
                [
                    new()
                    {
                        Name = "Observer",
                        ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 99 },
                        SlotId = 7,
                    },
                ],
            },
            CancellationToken.None);

        Assert.AreEqual(1000, first.State.RosterPlayers.Single(player => player.ToonId.Id == 99).InitialRating);

        var secondReplay = CreateReplay(new DateTime(2024, 1, 2, 20, 0, 0, DateTimeKind.Utc));
        var promotedPlayer = secondReplay.Players[2];
        promotedPlayer.Name = "Observer";
        promotedPlayer.Player.Name = "Observer";
        promotedPlayer.Player.ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = 99 };
        var second = await fixture.Service.UploadReplayAsync(
            session.SessionId,
            user.InHouseUserId,
            new InHouseReplayUploadRequest { Replay = secondReplay },
            CancellationToken.None);

        await fixture.AddReplayRatingAsync(second.State.Replays[0].ReplayHash);
        var refreshed = await fixture.Service.GetSessionAsync(session.SessionId, user.InHouseUserId, CancellationToken.None);

        Assert.IsNotNull(refreshed);
        Assert.AreEqual(1020, refreshed.Players.Single(player => player.ToonId.Id == 99).RatingStart);
        Assert.AreEqual(1020, refreshed.RosterPlayers.Single(player => player.ToonId.Id == 99).InitialRating);
    }

    [TestMethod]
    public void Matchmaker_NewManualPlayersAreSelectedFirst()
    {
        var session = CreateMatchmakingSession(2, Enumerable.Range(1, 7)
            .Select(i => CreateRosterPlayer(i, games: i == 7 ? 0 : 1, joinedReplayCount: i == 7 ? 2 : 0, isManual: i == 7))
            .ToList());

        var suggestion = InHouseMatchmaker.CreateSuggestion(session);

        Assert.IsTrue(suggestion.Team1.Concat(suggestion.Team2).Any(player => player.Name == "Player 7"));
    }

    [TestMethod]
    public void Matchmaker_LatestObserversAreSelectedBeforeEqualPlayers()
    {
        var roster = Enumerable.Range(1, 7)
            .Select(i => CreateRosterPlayer(i, games: 1, joinedReplayCount: 0, observedLatestGame: i == 7))
            .ToList();
        var session = CreateMatchmakingSession(2, roster);

        var suggestion = InHouseMatchmaker.CreateSuggestion(session);

        Assert.IsTrue(suggestion.Team1.Concat(suggestion.Team2).Any(player => player.Name == "Player 7"));
    }

    [TestMethod]
    public void Matchmaker_PlayDebtUsesJoinedReplayCount()
    {
        var roster = Enumerable.Range(1, 7)
            .Select(i => CreateRosterPlayer(i, games: i == 7 ? 0 : 2, joinedReplayCount: 0))
            .ToList();
        var session = CreateMatchmakingSession(2, roster);

        var suggestion = InHouseMatchmaker.CreateSuggestion(session);

        Assert.IsTrue(suggestion.Team1.Concat(suggestion.Team2).Any(player => player.Name == "Player 7"));
    }

    [TestMethod]
    public void Matchmaker_BalancesTeamsByInitialRating()
    {
        var ratings = new[] { 1200, 1100, 1000, 1000, 900, 800 };
        var session = CreateMatchmakingSession(1, ratings
            .Select((rating, index) => CreateRosterPlayer(index + 1, rating: rating))
            .ToList());

        var suggestion = InHouseMatchmaker.CreateSuggestion(session);

        Assert.AreEqual(0, suggestion.Scores.BalanceScore);
    }

    [TestMethod]
    public void Matchmaker_PenalizesRepeatedSameTeamPairs()
    {
        var roster = Enumerable.Range(1, 6)
            .Select(i => CreateRosterPlayer(i))
            .ToList();
        var session = CreateMatchmakingSession(1, roster);
        session.Replays[0].Players =
        [
            .. roster.Take(3).Select((player, index) => CreateReplayPlayer(player, 1, index + 1)),
            .. roster.Skip(3).Select((player, index) => CreateReplayPlayer(player, 2, index + 4)),
        ];

        var suggestion = InHouseMatchmaker.CreateSuggestion(session);

        Assert.IsLessThan(6, suggestion.Scores.SameRosterScore);
    }

    [TestMethod]
    public void Matchmaker_ScoreDraftRecomputesManualMoves()
    {
        var ratings = new[] { 1500, 1500, 1500, 500, 500, 500 };
        var roster = ratings.Select((rating, index) => CreateRosterPlayer(index + 1, rating: rating)).ToList();
        var session = CreateMatchmakingSession(1, roster);

        var stacked = InHouseMatchmaker.ScoreDraft(
            session,
            roster.Take(3).Select(player => player.RosterPlayerId).ToList(),
            roster.Skip(3).Select(player => player.RosterPlayerId).ToList());
        var mixed = InHouseMatchmaker.ScoreDraft(
            session,
            [roster[0].RosterPlayerId, roster[3].RosterPlayerId, roster[4].RosterPlayerId],
            [roster[1].RosterPlayerId, roster[2].RosterPlayerId, roster[5].RosterPlayerId]);

        Assert.IsGreaterThan(mixed.BalanceScore, stacked.BalanceScore);
    }

    private static ReplayDto CreateReplay(DateTime? gametime = null)
        => new()
        {
            Title = "Direct Strike TE",
            Version = "5.0.0",
            GameMode = GameMode.Standard,
            RegionId = 1,
            Gametime = gametime ?? new DateTime(2024, 1, 1, 20, 0, 0, DateTimeKind.Utc),
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

    private static InHouseGameSessionDetailDto CreateMatchmakingSession(
        int replayCount,
        List<InHouseRosterPlayerDto> roster)
        => new()
        {
            SessionId = Guid.NewGuid(),
            RosterPlayers = roster,
            Replays = Enumerable.Range(1, replayCount)
                .Select(i => new InHouseGameSessionReplayDto
                {
                    ReplayHash = $"replay-{i}",
                    Gametime = new DateTime(2024, 1, i, 20, 0, 0, DateTimeKind.Utc),
                })
                .ToList(),
        };

    private static InHouseRosterPlayerDto CreateRosterPlayer(
        int seed,
        double rating = 1000,
        int games = 0,
        int observes = 0,
        int joinedReplayCount = 0,
        bool isManual = false,
        bool observedLatestGame = false)
        => new()
        {
            RosterPlayerId = Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}"),
            Name = $"Player {seed}",
            ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = seed },
            InitialRating = rating,
            Games = games,
            Observes = observes,
            JoinedReplayCount = joinedReplayCount,
            IsManual = isManual,
            ObservedLatestGame = observedLatestGame,
        };

    private static InHouseGameSessionReplayPlayerDto CreateReplayPlayer(
        InHouseRosterPlayerDto rosterPlayer,
        int teamId,
        int gamePos)
        => new()
        {
            Name = rosterPlayer.Name,
            ToonId = rosterPlayer.ToonId,
            TeamId = teamId,
            GamePos = gamePos,
        };

    private static ClaimsPrincipal CreatePrincipal(InHouseUser user, bool isAdmin = false)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(InHouseClaims.UserId, user.InHouseUserId.ToString()),
        ];

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, InHouseRoles.Admin));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
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
            Service = CreateService();
        }

        public DsstatsContext Context { get; }
        public InHouseGameSessionService Service { get; }

        public InHouseGameSessionService CreateService()
        {
            var importService = new ImportService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<ImportService>.Instance);
            return new InHouseGameSessionService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                importService);
        }

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
