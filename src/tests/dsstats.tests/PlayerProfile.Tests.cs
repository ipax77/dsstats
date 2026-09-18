using dsstats.api.Controllers;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public class PlayerProfileTests
{
    internal static PlayerProfileRequest Request(int id = 1, RatingType type = RatingType.Commanders) =>
        new() { ToonId = new() { Id = id, Region = 1, Realm = 1 }, RatingType = type };

    [TestMethod]
    public async Task OverviewAndDetails_MatchLegacyWithoutRepeatingOverviewPayload()
    {
        using var fixture = new PlayerServiceTests.Fixture();
        using (var db = fixture.CreateContext())
        {
            for (int i = 1; i <= 15; i++) PlayerServiceTests.AddReplay(db, i, 1, RatingType.Commanders);
            PlayerServiceTests.AddReplay(db, 16, 1, RatingType.Standard);
            PlayerServiceTests.AddReplay(db, 17, 2, RatingType.Commanders);
            await db.SaveChangesAsync();
            var rows = await db.ReplayRatings.Where(r => r.ReplayId <= 15).ToListAsync();
            foreach (var row in rows)
            {
                db.ReplayPlayerRatings.Add(new()
                {
                    PlayerId = 2, ReplayRatingId = row.ReplayRatingId, RatingType = row.RatingType,
                    ReplayPlayer = new() { PlayerId = 2, ReplayId = row.ReplayId, TeamId = 2, GamePos = 4, Race = Commander.Artanis },
                    RatingBefore = 1400, RatingDelta = -7
                });
            }
            // A rated replay with missing rating data for the selected player.
            var missing = await db.ReplayPlayerRatings.FirstAsync(r => r.PlayerId == 1 && r.ReplayPlayer!.ReplayId == 1);
            db.Remove(missing);
            await db.SaveChangesAsync();
        }
        var request = Request();
        var legacy = await fixture.Service.GetPlayerStats(new() { ToonId = request.ToonId, RatingType = request.RatingType });
        var overview = (await fixture.Service.GetOverview(request))!;
        var details = (await fixture.Service.GetDetails(request))!;
        var old = legacy.RatingDetails.Single();
        EqualJson(old.Ratings, overview.History);
        EqualJson(old.Replays, overview.Replays);
        EqualJson(old.LongestWinStreak, overview.LongestWinStreak);
        EqualJson(old.LongestLoseStreak, overview.LongestLoseStreak);
        EqualJson(old.CurrentStreak, overview.CurrentStreak);
        EqualJson(old.TopRating, overview.TopRating);
        EqualJson(old.GameModes, details.GameModes);
        EqualJson(old.Commanders, details.Commanders);
        EqualJson(old.PosStats, details.Positions);
        EqualJson(old.OpponentStats, details.Opponents);
        EqualJson(old.AvgGainResponses.Single(), details.CommanderPerformance);
        Assert.AreEqual(old.AvgOpponentRating, details.AvgOpponentRating);
        Assert.AreEqual(legacy.Name, overview.Name);
        Assert.HasCount(12, overview.Replays);
        Assert.HasCount(1, details.Opponents);
        var standard = (await fixture.Service.GetOverview(Request(type: RatingType.Standard)))!;
        Assert.AreEqual("16", standard.Replays.Single().ReplayHash);
        Assert.IsFalse(JsonSerializer.Serialize(details).Contains("\"History\""));
    }

    [TestMethod]
    public async Task Overview_ParticipantQueriesAreBoundedToTwelveKeys()
    {
        var capture = new QueryCapture();
        using var fixture = new PlayerServiceTests.Fixture(capture);
        using (var db = fixture.CreateContext())
        {
            for (int i = 1; i <= 30; i++) PlayerServiceTests.AddReplay(db, i, 1, RatingType.Commanders);
            await db.SaveChangesAsync();
        }
        capture.Commands.Clear();
        var overview = (await fixture.Service.GetOverview(Request()))!;
        Assert.HasCount(12, overview.Replays);
        var selfQuery = capture.Commands.Single(c => c.Contains("PlayerOverview: self history"));
        Assert.IsFalse(selfQuery.Contains("ToonId"));
        Assert.IsFalse(selfQuery.Contains("ReplayHash"));
        // The history reads self scalars. Both collection queries use the bounded recent keys.
        var collectionQueries = capture.Commands.Where(c => c.Contains("ReplayHash") || c.Contains("ExpectedWinProbability")).ToList();
        Assert.HasCount(2, collectionQueries);
        foreach (var query in collectionQueries) StringAssert.Contains(query, "IN");
        Assert.IsTrue(capture.Commands.Any(c => c.Contains("LIMIT")));
        Assert.IsTrue(overview.Replays.All(r => int.Parse(r.ReplayHash) >= 19));
    }

    [TestMethod]
    public async Task Overview_AveragesOnlyAvailableScanCounts()
    {
        using var fixture = new PlayerServiceTests.Fixture();
        using (var db = fixture.CreateContext())
        {
            PlayerServiceTests.AddReplay(db, 1, 1, RatingType.Commanders);
            PlayerServiceTests.AddReplay(db, 2, 1, RatingType.Commanders);
            PlayerServiceTests.AddReplay(db, 3, 1, RatingType.Commanders);
            await db.SaveChangesAsync();
            var players = await db.ReplayPlayers.Where(x => x.PlayerId == 1).OrderBy(x => x.ReplayId).ToListAsync();
            players[0].ScanCount = 0;
            players[1].ScanCount = 4;
            await db.SaveChangesAsync();
        }

        var overview = (await fixture.Service.GetOverview(Request()))!;

        Assert.AreEqual(2, overview.ScanCountReplays);
        Assert.AreEqual(2.0, overview.AverageScanCount);
    }

    [TestMethod]
    public async Task UnknownAndUnratedPlayers_AreDistinctAndCancellationPropagates()
    {
        using var fixture = new PlayerServiceTests.Fixture();
        Assert.IsNull(await fixture.Service.GetOverview(Request(99)));
        Assert.IsNull(await fixture.Service.GetDetails(Request(99)));
        Assert.IsEmpty((await fixture.Service.GetOverview(Request(3)))!.History);
        Assert.IsEmpty((await fixture.Service.GetDetails(Request(3)))!.Teammates);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.GetOverview(Request(), cancellation.Token));
    }

    [TestMethod]
    public async Task V11Controller_UsesDistinctRouteAndReturns404()
    {
        var service = new Mock<IPlayerProfileService>();
        var controller = new PlayersV11Controller(service.Object);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Overview(Request(), default));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(Request(), default));
        var route = typeof(PlayersV11Controller).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Single();
        Assert.AreEqual("api11/Players", route.Template);
        service.Setup(s => s.GetOverview(It.IsAny<PlayerProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerOverview { Name = "Alpha" });
        Assert.IsInstanceOfType<OkObjectResult>(await controller.Overview(Request(), default));
        var legacyRoute = typeof(PlayersController).GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Single();
        Assert.AreEqual("api10/[controller]", legacyRoute.Template);
    }

    [TestMethod]
    public async Task Proxy_UsesV11AndDistinguishesMissingPlayersFromHttpFailure()
    {
        var handler = new ProfileHandler();
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("api")).Returns(client);
        var proxy = new dsstats.apiServices.PlayerProfileService(factory.Object);
        Assert.AreEqual("Alpha", (await proxy.GetOverview(Request()))!.Name);
        Assert.AreEqual("/api11/Players/overview", handler.Path);
        handler.Status = HttpStatusCode.NotFound;
        Assert.IsNull(await proxy.GetDetails(Request()));
        Assert.AreEqual("/api11/Players/details", handler.Path);
        handler.Status = HttpStatusCode.TooManyRequests;
        await Assert.ThrowsAsync<HttpRequestException>(() => proxy.GetOverview(Request()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => proxy.GetOverview(Request(), cancellation.Token));
    }

    private static void EqualJson<T>(T expected, T actual) => Assert.AreEqual(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));

    private sealed class ProfileHandler : HttpMessageHandler
    {
        public string? Path;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Path = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = JsonContent.Create(new PlayerOverview { Name = "Alpha" }) });
        }
    }

    internal sealed class QueryCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
