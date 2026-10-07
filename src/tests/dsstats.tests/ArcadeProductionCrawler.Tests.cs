using System.Net;
using dsstats.dbServices;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sc2arcade.crawler;

namespace dsstats.tests;

[TestClass]
public class ArcadeProductionCrawlerTests
{
    [TestMethod]
    [DataRow(403)]
    [DataRow(429)]
    [DataRow(500)]
    public async Task HttpFailureStopsAllRegionsWithoutRetry(int status)
    {
        int requests = 0;
        var clock = new Clock();
        var gate = new Sc2ArcadeRequestGate(clock);
        var handler = new Handler(_ => { requests++; return new((HttpStatusCode)status) { Content = new StringContent("denied") }; });
        using var services = Services();
        var crawler = Crawler(services, handler, gate);
        await crawler.GetLobbyHistory(DateTime.UtcNow.AddDays(-5), CancellationToken.None);
        Assert.AreEqual(1, requests);
    }

    [TestMethod]
    public async Task SuccessfulCutoffPageWaitsBeforeEu_RespectsRetryAfter()
    {
        int requests = 0;
        var clock = new Clock();
        var gate = new Sc2ArcadeRequestGate(clock);
        var start = clock.GetUtcNow();
        DateTimeOffset secondAt = default;
        string? secondQuery = null;
        var handler = new Handler(request =>
        {
            requests++;
            if (requests == 2)
            {
                secondAt = clock.GetUtcNow();
                secondQuery = request.RequestUri!.Query;
                return new(HttpStatusCode.Forbidden) { Content = new StringContent("denied") };
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"page":{"next":"older"},"results":[{"regionId":1,"createdAt":"2026-05-01T00:00:00Z","slotsHumansTaken":1,"slots":[]}]}
                    """)
            };
            response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", "29");
            response.Headers.TryAddWithoutValidation("Retry-After", "17");
            return response;
        });
        using var services = Services();
        await Crawler(services, handler, gate).GetLobbyHistory(new DateTime(2026, 6, 1), CancellationToken.None);
        Assert.AreEqual(2, requests);
        Assert.IsTrue(secondAt >= start.AddSeconds(17));
        StringAssert.Contains(secondQuery!, "regionId=2");
    }

    [TestMethod]
    public async Task NormalPagesAreSpacedThreeSecondsAndCursorIsEncoded()
    {
        int requests = 0;
        var clock = new Clock();
        var start = clock.GetUtcNow();
        DateTimeOffset secondAt = default;
        string? secondQuery = null;
        var handler = new Handler(request =>
        {
            if (++requests == 2)
            {
                secondAt = clock.GetUtcNow();
                secondQuery = request.RequestUri!.Query;
                return new(HttpStatusCode.Forbidden) { Content = new StringContent("stop") };
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""
                {"page":{"next":"a+/="},"results":[{"regionId":1,"createdAt":"2026-10-07T00:00:00Z","slotsHumansTaken":1,"slots":[]}]}
                """) };
            response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", "29");
            return response;
        });
        using var services = Services();
        await Crawler(services, handler, new(clock)).GetLobbyHistory(new DateTime(2026, 10, 1), CancellationToken.None);
        Assert.AreEqual(2, requests);
        Assert.IsTrue(secondAt >= start.AddSeconds(3));
        StringAssert.Contains(secondQuery!, "after=a%2B%2F%3D");
    }

    private static ServiceProvider Services() => new ServiceCollection().AddSingleton(Mock.Of<IImportService>())
        .AddSingleton(Mock.Of<IRatingService>()).BuildServiceProvider();

    private static CrawlerService Crawler(ServiceProvider services, HttpMessageHandler handler, Sc2ArcadeRequestGate gate)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() =>
            new HttpClient(handler, false) { BaseAddress = new Uri("https://sc2arcade.com/api/") });
        return new CrawlerService(services, factory.Object, NullLogger<CrawlerService>.Instance, gate);
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(response(request));
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-10-07T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            now += dueTime;
            return new Timer(callback, state, 1, Timeout.Infinite);
        }
    }
}
