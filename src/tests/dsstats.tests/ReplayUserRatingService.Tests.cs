using System.Net;
using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayUserRatingServiceTests
{
    [TestMethod]
    public async Task GetIpHash_UsesNormalizedIpAddress()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var ipv4Hash = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.1"));
        var mappedIpv4Hash = fixture.Service.GetIpHash(IPAddress.Parse("::ffff:127.0.0.1"));
        var otherIpv4Hash = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.2"));

        Assert.AreEqual(ipv4Hash, mappedIpv4Hash);
        Assert.AreNotEqual(ipv4Hash, otherIpv4Hash);
    }

    [TestMethod]
    public async Task SubmitRating_StoresDurableCollectRow_AndShowsPendingAverage()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        var ipHash = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.1"));

        var result = await fixture.Service.SubmitRatingAsync("rating-hash", ipHash, 5);

        Assert.AreEqual(ReplayUserRatingSubmitStatus.Accepted, result.Status);
        Assert.IsNotNull(result.Rating);
        Assert.AreEqual(5.0, result.Rating.Average);
        Assert.AreEqual(1, result.Rating.VoteCount);
        Assert.AreEqual(5, result.Rating.CurrentVote);
        Assert.AreEqual(1, await fixture.Context.ReplayUserRatingCollects.CountAsync());
        Assert.AreEqual(0, await fixture.Context.ReplayUserRatingSummaries.CountAsync());
    }

    [TestMethod]
    public async Task SubmitRating_RejectsInvalidScores_AndMissingReplay()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        var ipHash = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.1"));

        var invalid = await fixture.Service.SubmitRatingAsync("rating-hash", ipHash, 6);
        var missing = await fixture.Service.SubmitRatingAsync("missing", ipHash, 3);

        Assert.AreEqual(ReplayUserRatingSubmitStatus.InvalidScore, invalid.Status);
        Assert.AreEqual(ReplayUserRatingSubmitStatus.ReplayNotFound, missing.Status);
        Assert.AreEqual(0, await fixture.Context.ReplayUserRatingCollects.CountAsync());
    }

    [TestMethod]
    public async Task SubmitRating_EnforcesCooldown_ButAllowsAfterCooldown()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        var ipHash = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.1"));

        var first = await fixture.Service.SubmitRatingAsync("rating-hash", ipHash, 4);
        var second = await fixture.Service.SubmitRatingAsync("rating-hash", ipHash, 2);

        Assert.AreEqual(ReplayUserRatingSubmitStatus.Accepted, first.Status);
        Assert.AreEqual(ReplayUserRatingSubmitStatus.CooldownActive, second.Status);
        Assert.IsNotNull(second.NextAllowedVoteAt);
        Assert.AreEqual(1, await fixture.Context.ReplayUserRatingCollects.CountAsync());

        var vote = await fixture.Context.ReplayUserRatingCollects.SingleAsync();
        vote.CreatedAt = DateTime.UtcNow.AddHours(-25);
        await fixture.Context.SaveChangesAsync();

        var afterCooldown = await fixture.Service.SubmitRatingAsync("rating-hash", ipHash, 2);

        Assert.AreEqual(ReplayUserRatingSubmitStatus.Accepted, afterCooldown.Status);
        Assert.AreEqual(2, await fixture.Context.ReplayUserRatingCollects.CountAsync());
    }

    [TestMethod]
    public async Task CollectPendingVotes_UpdatesSummary_AndKeepsReadsStable()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        var firstIp = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.1"));
        var secondIp = fixture.Service.GetIpHash(IPAddress.Parse("127.0.0.2"));
        await fixture.Service.SubmitRatingAsync("rating-hash", firstIp, 5);
        await fixture.Service.SubmitRatingAsync("rating-hash", secondIp, 3);

        var collected = await fixture.Service.CollectPendingVotesAsync();
        var rating = await fixture.Service.GetRatingAsync("rating-hash", firstIp);
        var summary = await fixture.Context.ReplayUserRatingSummaries.SingleAsync();

        Assert.AreEqual(2, collected);
        Assert.AreEqual(2, summary.VoteCount);
        Assert.AreEqual(8, summary.ScoreSum);
        Assert.AreEqual(4.0, rating!.Average);
        Assert.AreEqual(2, rating.VoteCount);
        Assert.AreEqual(2, await fixture.Context.ReplayUserRatingCollects.CountAsync(x => x.ProcessedAt != null));
    }

    [TestMethod]
    public async Task RebuildPendingOverlay_LoadsDurableUnprocessedRowsAfterRestart()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        fixture.Context.ReplayUserRatingCollects.Add(new()
        {
            ReplayId = 1,
            IpHash = "restarted-ip",
            Score = 4,
            CreatedAt = DateTime.UtcNow
        });
        await fixture.Context.SaveChangesAsync();

        var restarted = fixture.CreateService();
        var rating = await restarted.GetRatingAsync("rating-hash", "restarted-ip");

        Assert.AreEqual(4.0, rating!.Average);
        Assert.AreEqual(1, rating.VoteCount);
        Assert.AreEqual(4, rating.CurrentVote);
    }

    [TestMethod]
    public async Task CollectPendingVotes_DeletesProcessedRowsAfterRetention()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context);
        fixture.Context.ReplayUserRatingCollects.Add(new()
        {
            ReplayId = 1,
            IpHash = "old-ip",
            Score = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            ProcessedAt = DateTime.UtcNow.AddDays(-8)
        });
        await fixture.Context.SaveChangesAsync();

        var collected = await fixture.Service.CollectPendingVotesAsync();

        Assert.AreEqual(0, collected);
        Assert.AreEqual(0, await fixture.Context.ReplayUserRatingCollects.CountAsync());
    }

    private static async Task SeedReplayAsync(DsstatsContext context)
    {
        context.Replays.Add(new()
        {
            ReplayId = 1,
            FileName = "Replay.SC2Replay",
            Title = "Replay",
            Version = "1.0",
            GameMode = GameMode.Commanders,
            RegionId = 1,
            PlayerCount = 6,
            Gametime = new DateTime(2026, 5, 24),
            BaseBuild = 90000,
            Duration = 900,
            WinnerTeam = 1,
            ReplayHash = "rating-hash",
            CompatHash = "rating-compat",
            Imported = new DateTime(2026, 5, 24),
            Uploaded = true
        });
        await context.SaveChangesAsync();
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, ServiceProvider serviceProvider, DsstatsContext context)
        {
            Connection = connection;
            ServiceProvider = serviceProvider;
            Context = context;
            Service = CreateService();
        }

        public SqliteConnection Connection { get; }
        public ServiceProvider ServiceProvider { get; }
        public DsstatsContext Context { get; }
        public ReplayUserRatingService Service { get; }

        public ReplayUserRatingService CreateService()
        {
            return new(
                ServiceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>(),
                Options.Create(new ReplayUserRatingOptions
                {
                    IpHashSalt = "test-salt",
                    Cooldown = TimeSpan.FromHours(24),
                    ProcessedRetention = TimeSpan.FromDays(7)
                }),
                NullLogger<ReplayUserRatingService>.Instance);
        }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddDbContextFactory<DsstatsContext>(options => options.UseSqlite(connection, sqlite =>
                sqlite.MigrationsAssembly("dsstats.migrations.sqlite")));

            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>().CreateDbContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            return new TestFixture(connection, serviceProvider, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await ServiceProvider.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
