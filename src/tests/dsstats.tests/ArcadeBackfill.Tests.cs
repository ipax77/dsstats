using System.Net;
using System.Text.Json;
using dsstats.api.Services;
using dsstats.dbServices;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sc2arcade.backfill;
using sc2arcade.crawler;

namespace dsstats.tests;

[TestClass]
public sealed class ArcadeBackfillTests
{
    private string directory = null!;
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-07T12:00:00Z");
    private static readonly DateTimeOffset Cutoff = DateTimeOffset.Parse("2026-05-31T03:31:59Z");

    [TestInitialize]
    public void Setup() => directory = Path.Combine(Path.GetTempPath(), "dsstats-backfill-tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    public async Task First403StopsGlobally_ResumeHonorsWaitAndPreservesCursor()
    {
        var clock = new AdvancingClock();
        var denied = Response(HttpStatusCode.Forbidden, new string('x', 5000));
        denied.Headers.TryAddWithoutValidation("Retry-After", "7200");
        denied.Headers.TryAddWithoutValidation("cf-ray", "diagnostic-ray");
        denied.Headers.TryAddWithoutValidation("Set-Cookie", "secret");
        var handler = new QueueHandler(_ => Response(HttpStatusCode.OK, Page(1, "a+/=")), _ => denied);
        DateTimeOffset deadline;
        using (var archive = Create())
        {
            var state = await new BackfillRunner(new HttpClient(handler), clock).RunAsync(archive);
            Assert.AreEqual("Http403", state.StopReason);
            Assert.AreEqual(2, handler.Uris.Count);
            Assert.AreEqual(0, state.Regions[1].Pages);
            Assert.AreEqual("a+/=", state.Active!.Cursor);
            Assert.AreEqual(200, state.PreviousSuccessfulResponse!.StatusCode);
            Assert.AreEqual(4096, state.LastResponse!.Body!.Length);
            Assert.IsTrue(state.LastResponse.BodyTruncated);
            Assert.IsFalse(state.LastResponse.Headers.ContainsKey("Set-Cookie"));
            deadline = state.NotBefore;
        }
        using (var readOnly = Archive.Open(directory))
            Assert.AreEqual("Http403", BackfillState.Read(readOnly).StopReason);
        var resumed = new QueueHandler(uri =>
        {
            Assert.IsTrue(clock.GetUtcNow() >= deadline);
            StringAssert.Contains(uri, "after=a%2B%2F%3D");
            return Response(HttpStatusCode.Forbidden, "denied again");
        });
        using var reopened = Archive.Open(directory, write: true);
        var again = await new BackfillRunner(new HttpClient(resumed), clock).RunAsync(reopened);
        Assert.AreEqual("Http403", again.StopReason);
        Assert.HasCount(1, resumed.Uris);
        Assert.AreEqual("a+/=", again.Active!.Cursor);
    }

    [TestMethod]
    public async Task CommittedPageIsCheckpoint_InterruptedWaitDoesNotRefetchIt()
    {
        var clock = new AdvancingClock();
        using var cancellation = new CancellationTokenSource();
        clock.OnDelay = cancellation.Cancel;
        var handler = new QueueHandler(_ => Response(HttpStatusCode.OK, Page(1, "saved")));
        using (var archive = Create())
        {
            var state = await new BackfillRunner(new HttpClient(handler), clock).RunAsync(archive, cancellation.Token);
            Assert.AreEqual("Cancelled", state.StopReason);
            Assert.AreEqual(1, state.Active!.Pages);
        }
        clock.OnDelay = null;
        var resumed = new QueueHandler(uri =>
        {
            StringAssert.Contains(uri, "after=saved");
            return Response(HttpStatusCode.Forbidden, "stop");
        });
        using var reopen = Archive.Open(directory, true);
        await new BackfillRunner(new HttpClient(resumed), clock).RunAsync(reopen);
        Assert.HasCount(1, resumed.Uris);
    }

    [TestMethod]
    public async Task InterruptedBeforePageCommit_ReplaysSameRequestAndIgnoresTemporaryFile()
    {
        using (var archive = Create())
        {
            archive.Append(new() { Kind = "Started", At = Start, RegionId = 1, MapId = 208271, NotBefore = Start.AddSeconds(60) });
        }
        File.WriteAllText(Path.Combine(directory, "0000000002.json.tmp"), "interrupted write");
        var clock = new AdvancingClock();
        var handler = new QueueHandler(uri =>
        {
            Assert.IsFalse(uri.Contains("after="));
            Assert.IsTrue(clock.GetUtcNow() >= Start.AddSeconds(60));
            return Response(HttpStatusCode.Forbidden, "stop");
        });
        using var reopen = Archive.Open(directory, true);
        var state = await new BackfillRunner(new HttpClient(handler), clock).RunAsync(reopen);
        Assert.AreEqual(0, state.Active!.Pages);
    }

    [TestMethod]
    public async Task HeadersSurviveInterruptedBody_ResumeRespectsServerDeadline()
    {
        using (var archive = Create())
        {
            archive.Append(new() { Kind = "Started", At = Start, RegionId = 1, MapId = 208271 });
            archive.Append(new()
            {
                Kind = "Headers", At = Start, RequestStartedAt = Start, RegionId = 1, MapId = 208271,
                StatusCode = 403, Outcome = "Http403", NotBefore = Start.AddHours(4),
                Headers = new() { ["Retry-After"] = "14400" }
            });
        }
        var clock = new AdvancingClock();
        var handler = new QueueHandler(_ =>
        {
            Assert.IsTrue(clock.GetUtcNow() >= Start.AddHours(4));
            return Response(HttpStatusCode.Forbidden, "stop");
        });
        using var reopened = Archive.Open(directory, true);
        await new BackfillRunner(new HttpClient(handler), clock).RunAsync(reopened);
    }

    [TestMethod]
    public async Task CutoffPageRetained_RegionTransitionIsResumable()
    {
        var clock = new AdvancingClock();
        using var cancellation = new CancellationTokenSource();
        clock.OnDelay = cancellation.Cancel;
        using (var archive = Create())
        {
            var state = await new BackfillRunner(new HttpClient(new QueueHandler(_ =>
                Response(HttpStatusCode.OK, Page(1, "older", "2026-05-30T00:00:00Z")))), clock).RunAsync(archive, cancellation.Token);
            Assert.AreEqual("CutoffReached", state.Regions[0].Completion);
            Assert.AreEqual(1, state.Regions[0].Lobbies);
            Assert.AreEqual(2, state.Active!.RegionId);
        }
        clock.OnDelay = null;
        using var reopened = Archive.Open(directory, true);
        var handler = new QueueHandler(uri =>
        {
            StringAssert.Contains(uri, "regionId=2");
            return Response(HttpStatusCode.OK, "{\"page\":{\"next\":null},\"results\":[]}");
        });
        var done = await new BackfillRunner(new HttpClient(handler), clock).RunAsync(reopened);
        Assert.AreEqual("Complete", done.StopReason);
        Assert.AreEqual("HistoryExhaustedBeforeCutoff", done.Regions[1].Completion);
    }

    [TestMethod]
    public async Task DiskFailureDoesNotAdvanceCursor()
    {
        using var archive = Create();
        var handler = new QueueHandler(_ =>
        {
            // Permit the header checkpoint, but block the successful page commit.
            Directory.CreateDirectory(Path.Combine(directory, "0000000003.json"));
            return Response(HttpStatusCode.OK, Page(1, "must-not-advance"));
        });
        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await new BackfillRunner(new HttpClient(handler), new AdvancingClock()).RunAsync(archive));
        Assert.IsNull(BackfillState.Read(archive).Active!.Cursor);
    }

    [TestMethod]
    public async Task RepeatedCursorStopsWithoutAdvancing()
    {
        using var archive = Create();
        var handler = new QueueHandler(_ => Response(HttpStatusCode.OK, Page(1, "loop")),
            _ => Response(HttpStatusCode.OK, Page(1, "loop")));
        var state = await new BackfillRunner(new HttpClient(handler), new AdvancingClock()).RunAsync(archive);
        Assert.AreEqual("RepeatedCursor", state.StopReason);
        Assert.AreEqual(1, state.Active!.Pages);
    }

    [TestMethod]
    [DataRow("<html>challenge</html>", "HtmlChallenge")]
    [DataRow("{}", "InvalidResponse")]
    [DataRow("not json", "InvalidResponse")]
    [DataRow("null", "InvalidResponse")]
    [DataRow("[]", "InvalidResponse")]
    [DataRow("\"unexpected string\"", "InvalidResponse")]
    [DataRow("{\"page\":{},\"results\":null}", "InvalidResponse")]
    [DataRow("{\"page\":{},\"results\":[]}", "InvalidResponse")]
    public async Task InvalidResponseDoesNotBecomeEndOfHistory(string body, string reason)
    {
        using var archive = Create();
        var handler = new QueueHandler(_ => Response(HttpStatusCode.OK, body));
        var state = await new BackfillRunner(new HttpClient(handler), new AdvancingClock()).RunAsync(archive);
        Assert.AreEqual(reason, state.StopReason);
        Assert.AreEqual(0, state.Active!.Pages);
        Assert.HasCount(1, handler.Uris);
    }

    [TestMethod]
    [DataRow(429)]
    [DataRow(500)]
    [DataRow(302)]
    public async Task OtherHttpFailuresStopWithoutRetry(int status)
    {
        using var archive = Create();
        var handler = new QueueHandler(_ => Response((HttpStatusCode)status, "stop"));
        var state = await new BackfillRunner(new HttpClient(handler), new AdvancingClock()).RunAsync(archive);
        Assert.AreEqual($"Http{status}", state.StopReason);
        Assert.HasCount(1, handler.Uris);
    }

    [TestMethod]
    public async Task NetworkFailureStopsWithoutAdvancing()
    {
        using var archive = Create();
        var state = await new BackfillRunner(new HttpClient(new QueueHandler(_ => throw new HttpRequestException("offline"))),
            new AdvancingClock()).RunAsync(archive);
        Assert.AreEqual("NetworkFailure", state.StopReason);
        Assert.IsNull(state.Active!.Cursor);
    }

    [TestMethod]
    public void ArchiveDetectsCorruptionAndLocksWriters()
    {
        using (var archive = Create())
        {
            Assert.ThrowsExactly<IOException>(() => Archive.Open(directory, true));
            using var status = Archive.Open(directory);
            Assert.AreEqual(Cutoff, status.Configuration.Cutoff);
        }
        var path = Path.Combine(directory, "0000000000.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("2026-05-31", "2026-05-30"));
        Assert.ThrowsExactly<InvalidDataException>(() => Archive.Open(directory));
    }

    [TestMethod]
    public void HeaderWaitParsingAndPrivacy()
    {
        Assert.AreEqual(Start.AddSeconds(120), BackfillRunner.GetNotBefore(new() { ["Retry-After"] = "120" }, Start, false));
        Assert.AreEqual(Start.AddHours(1), BackfillRunner.GetNotBefore(new() { ["Retry-After"] = Start.AddHours(1).ToString("R") }, Start, false));
        Assert.AreEqual(Start.AddSeconds(7), BackfillRunner.GetNotBefore(new()
            { ["x-ratelimit-remaining"] = "1", ["x-ratelimit-reset"] = "7" }, Start, true));
        Assert.AreEqual(Start.AddSeconds(60), BackfillRunner.GetNotBefore(new() { ["Retry-After"] = "invalid" }, Start, false));
        Assert.AreEqual(Start.AddSeconds(3), BackfillRunner.GetNotBefore(new()
            { ["x-ratelimit-remaining"] = "29", ["x-ratelimit-reset"] = "7" }, Start, true));
        Assert.AreEqual(DateTimeOffset.MaxValue, BackfillRunner.GetNotBefore(new() { ["Retry-After"] = long.MaxValue.ToString() }, Start, false));
    }

    [TestMethod]
    public async Task DisabledProductionCrawlStillRunsMaintenance()
    {
        var import = new Mock<IImportService>();
        var ratings = new Mock<IRatingService>();
        var crawler = new Mock<ICrawlerService>();
        using var services = new ServiceCollection().AddSingleton(crawler.Object).BuildServiceProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["SC2Arcade:CrawlEnabled"] = "false" }).Build();
        var job = new ArcadeJobService(services.GetRequiredService<IServiceScopeFactory>(), import.Object,
            ratings.Object, NullLogger<ArcadeJobService>.Instance, config);
        Assert.IsTrue(await job.RunAsync());
        crawler.Verify(c => c.GetLobbyHistory(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        import.Verify(i => i.CheckDuplicateCandidates(), Times.Once);
        import.Verify(i => i.CheckRealmDuplicateCandidates(), Times.Once);
        import.Verify(i => i.FixPlayerNames(), Times.Once);
        ratings.Verify(r => r.CreateRatings(), Times.Once);
    }

    [TestMethod]
    public async Task ProductionCrawlIsEnabledByDefault()
    {
        var crawler = new Mock<ICrawlerService>();
        using var services = new ServiceCollection().AddSingleton(crawler.Object).BuildServiceProvider();
        var job = new ArcadeJobService(services.GetRequiredService<IServiceScopeFactory>(), Mock.Of<IImportService>(),
            Mock.Of<IRatingService>(), NullLogger<ArcadeJobService>.Instance, new ConfigurationBuilder().Build());
        Assert.IsTrue(await job.RunAsync());
        crawler.Verify(c => c.GetLobbyHistory(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SampleInspectionUsesSharedEligibilityAndCountsDuplicates()
    {
        string body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testdata", "sc2arcade-eu-sample.json"));
        var sample = BackfillState.ParsePage(body, 2);
        Assert.IsTrue(sample.Results.Count > 0);
        int eligible = sample.Results.Count(r => ArcadeReplayConverter.Convert(r).Replay != null);
        Assert.IsTrue(eligible > 0);
        // First finish NA, then archive the EU fixture twice with distinct pagination cursors.
        var second = JsonSerializer.Serialize(new { page = new { next = "second" }, results = sample.Results },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new QueueHandler(_ => Response(HttpStatusCode.OK, Page(1, null, "2026-05-30T00:00:00Z")),
            _ => Response(HttpStatusCode.OK, body), _ => Response(HttpStatusCode.OK, second),
            _ => Response(HttpStatusCode.Forbidden, "stop"));
        using (var archive = Create())
        {
            var state = await new BackfillRunner(new HttpClient(handler), new AdvancingClock()).RunAsync(archive);
            var eu = state.Regions[1];
            Assert.AreEqual(sample.Results.Count * 2, eu.Lobbies);
            Assert.AreEqual(eligible * 2, eu.EligibleReplays);
            Assert.AreEqual(eligible, eu.DuplicateKeys);
            Assert.AreEqual(eu.Lobbies - eu.EligibleReplays, eu.Rejections.Values.Sum());
            Assert.AreEqual(eu.Lobbies, eu.DailyLobbies.Values.Sum());
        }
        using var reopened = Archive.Open(directory);
        Assert.AreEqual(eligible, BackfillState.Read(reopened).Regions[1].DuplicateKeys);
    }

    [TestMethod]
    public void IncompatibleConfigurationIsRejected()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => Archive.Create(directory,
            ArchiveConfiguration.Create(Start, Cutoff) with { MinimumDelaySeconds = 0 }));
    }

    private Archive Create() => Archive.Create(directory, ArchiveConfiguration.Create(Start, Cutoff));

    private static HttpResponseMessage Response(HttpStatusCode status, string body)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", "29");
        response.Headers.TryAddWithoutValidation("x-ratelimit-reset", "7");
        return response;
    }

    private static string Page(int region, string? next, string created = "2026-10-06T00:00:00Z") => JsonSerializer.Serialize(new
    {
        page = new { next },
        results = new[] { new { regionId = region, createdAt = created, slotsHumansTaken = 1, slots = Array.Empty<object>() } }
    });

    private sealed class QueueHandler(params Func<string, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public List<string> Uris { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string uri = request.RequestUri!.AbsoluteUri;
            Uris.Add(uri);
            Assert.IsTrue(Uris.Count <= responses.Length, "Unexpected additional API request.");
            return Task.FromResult(responses[Uris.Count - 1](uri));
        }
    }

    private sealed class AdvancingClock : TimeProvider
    {
        private DateTimeOffset now = Start;
        public Action? OnDelay { get; set; }
        public override DateTimeOffset GetUtcNow() => now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            now += dueTime;
            OnDelay?.Invoke();
            return new Timer(callback, state, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
        }
    }
}
