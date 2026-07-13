using dsstats.db;
using dsstats.dbServices.Stats;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class WinrateComparisonTests
{
    [TestMethod]
    public void Resolve_CreatesEqualMirroredInclusivePeriods()
    {
        var windows = new WinrateComparisonRequest
        {
            ChangeDate = new DateTime(2026, 6, 23),
            AfterToDate = new DateTime(2026, 7, 13)
        }.Resolve(new DateTime(2026, 7, 13));

        Assert.AreEqual(new DateTime(2026, 6, 2), windows.BeforeFrom);
        Assert.AreEqual(new DateTime(2026, 6, 22), windows.BeforeToDate);
        Assert.AreEqual(new DateTime(2026, 6, 23), windows.AfterFrom);
        Assert.AreEqual(new DateTime(2026, 7, 13), windows.AfterToDate);
    }

    [TestMethod]
    public void Resolve_RejectsReversedAndFutureRanges()
    {
        Assert.Throws<ArgumentException>(() => new WinrateComparisonRequest
        {
            ChangeDate = new DateTime(2026, 7, 14),
            AfterToDate = new DateTime(2026, 7, 13)
        }.Resolve(new DateTime(2026, 7, 13)));

        Assert.Throws<ArgumentException>(() => new WinrateComparisonRequest
        {
            ChangeDate = new DateTime(2026, 7, 13),
            AfterToDate = new DateTime(2026, 7, 14)
        }.Resolve(new DateTime(2026, 7, 13)));
    }

    [TestMethod]
    public void CalculateWelch95_UsesReplayMomentsAndDetectsHigherAfter()
    {
        var result = WinrateComparisonStatistics.CalculateWelch95(
            new AggregateMoments(3, 6, 14),
            new AggregateMoments(3, 15, 77));

        Assert.AreEqual(3, result.Difference, 0.0001);
        Assert.AreEqual(0.733, result.Low!.Value, 0.01);
        Assert.AreEqual(5.267, result.High!.Value, 0.01);
        Assert.AreEqual(ComparisonConfidenceStatus.HigherAfter, result.Status);
    }

    [TestMethod]
    public void CalculateWelch95_HandlesInsufficientAndZeroVarianceSamples()
    {
        var insufficient = WinrateComparisonStatistics.CalculateWelch95(
            new AggregateMoments(1, 2, 4),
            new AggregateMoments(2, 8, 32));
        Assert.AreEqual(ComparisonConfidenceStatus.InsufficientData, insufficient.Status);
        Assert.IsNull(insufficient.Low);

        var zeroVariance = WinrateComparisonStatistics.CalculateWelch95(
            new AggregateMoments(2, 4, 8),
            new AggregateMoments(2, 8, 32));
        Assert.AreEqual(ComparisonConfidenceStatus.HigherAfter, zeroVariance.Status);
        Assert.AreEqual(2, zeroVariance.Low);
        Assert.AreEqual(2, zeroVariance.High);
    }

    [TestMethod]
    public void CalculateWelch95_ReportsInconclusiveWhenIntervalCrossesZero()
    {
        var result = WinrateComparisonStatistics.CalculateWelch95(
            new AggregateMoments(3, 6, 14),
            new AggregateMoments(3, 6.3, 14.63));

        Assert.AreEqual(ComparisonConfidenceStatus.Inconclusive, result.Status);
        Assert.IsTrue(result.Low < 0);
        Assert.IsTrue(result.High > 0);
    }

    [TestMethod]
    public async Task Provider_AggregatesByReplayAndHonorsComparisonBoundaries()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplay(fixture.Context, 1, new DateTime(2026, 6, 20), [-100], [false]); // excluded
        await SeedReplay(fixture.Context, 2, new DateTime(2026, 6, 21), [-2], [false]);
        await SeedReplay(fixture.Context, 3, new DateTime(2026, 6, 22), [2, 6], [true, false]);
        await SeedReplay(fixture.Context, 4, new DateTime(2026, 6, 23), [8, 12], [true, true]);
        await SeedReplay(fixture.Context, 5, new DateTime(2026, 6, 24), [14], [false]);
        await SeedReplay(fixture.Context, 6, new DateTime(2026, 6, 25), [100], [true]); // end-exclusive

        var response = await fixture.Provider.GetStatsAsync(CreateRequest());

        var row = response.ComparisonEnts.Single(x => x.Commander == Commander.Abathur);
        Assert.AreEqual(3, row.Before.Appearances);
        Assert.AreEqual(2, row.Before.Replays, "Duplicate commander appearances must be one confidence sample.");
        Assert.AreEqual(1, row.Before.Wins);
        Assert.AreEqual(1, row.Before.AverageRatingGain, 0.001);
        Assert.AreEqual(3, row.After.Appearances);
        Assert.AreEqual(2, row.After.Replays);
        Assert.AreEqual(2, row.After.Wins);
        Assert.AreEqual(12, row.After.AverageRatingGain, 0.001);
        Assert.AreEqual(11, row.AverageGainDifference, 0.001);
        Assert.AreEqual(1.0 / 3.0, row.RawWinrateDifference, 0.0001);
    }

    [TestMethod]
    public async Task Provider_PreservesInterestOpponentGrouping()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplay(fixture.Context, 1, new DateTime(2026, 6, 21), [1], [true]);
        await SeedReplay(fixture.Context, 2, new DateTime(2026, 6, 23), [2], [true]);
        var request = CreateRequest();
        request.Interest = Commander.Abathur;

        var response = await fixture.Provider.GetStatsAsync(request);

        Assert.HasCount(1, response.ComparisonEnts);
        Assert.AreEqual(Commander.Alarak, response.ComparisonEnts[0].Commander);
    }

    [TestMethod]
    public void CacheKey_SeparatesComparisonDatesButNotChartMetric()
    {
        var first = CreateRequest();
        var second = CreateRequest();
        second.Comparison = second.Comparison! with { AfterToDate = new DateTime(2026, 6, 25) };
        var metricOnly = CreateRequest();
        metricOnly.Comparison = metricOnly.Comparison! with { Metric = WinrateComparisonMetric.UnadjustedWinrate };

        Assert.AreNotEqual(first.GetMemKey(StatsType.Winrate), second.GetMemKey(StatsType.Winrate));
        Assert.AreEqual(first.GetMemKey(StatsType.Winrate), metricOnly.GetMemKey(StatsType.Winrate));
    }

    private static StatsRequest CreateRequest() => new()
    {
        Type = StatsType.Winrate,
        TimePeriod = TimePeriod.Last90Days,
        RatingType = RatingType.Commanders,
        Comparison = new WinrateComparisonRequest
        {
            ChangeDate = new DateTime(2026, 6, 23),
            AfterToDate = new DateTime(2026, 6, 24)
        }
    };

    private static async Task SeedReplay(
        DsstatsContext context,
        int replayId,
        DateTime gameTime,
        double[] gains,
        bool[] wins)
    {
        var replayRatingId = replayId * 10;
        context.Replays.Add(new Replay
        {
            ReplayId = replayId,
            FileName = $"Replay-{replayId}",
            Title = $"Replay {replayId}",
            Version = "1",
            GameMode = GameMode.Commanders,
            RegionId = 1,
            PlayerCount = gains.Length,
            Gametime = gameTime,
            BaseBuild = 1,
            Duration = 900,
            WinnerTeam = 1,
            ReplayHash = $"hash-{replayId}",
            CompatHash = $"compat-{replayId}",
            Imported = gameTime,
            Uploaded = true
        });
        context.ReplayRatings.Add(new ReplayRating
        {
            ReplayRatingId = replayRatingId,
            ReplayId = replayId,
            RatingType = RatingType.Commanders,
            LeaverType = LeaverType.None,
            ExpectedWinProbability = 0.5,
            AvgRating = 2000
        });

        for (var i = 0; i < gains.Length; i++)
        {
            var playerId = replayId * 100 + i + 1;
            var replayPlayerId = replayId * 1000 + i + 1;
            context.Players.Add(new Player
            {
                PlayerId = playerId,
                Name = $"Player-{playerId}",
                ToonId = new ToonId { Region = 1, Realm = 1, Id = playerId }
            });
            context.ReplayPlayers.Add(new ReplayPlayer
            {
                ReplayPlayerId = replayPlayerId,
                ReplayId = replayId,
                PlayerId = playerId,
                Name = $"Player-{playerId}",
                Race = Commander.Abathur,
                SelectedRace = Commander.Abathur,
                OppRace = Commander.Alarak,
                TeamId = wins[i] ? 1 : 2,
                GamePos = i + 1,
                Duration = 900,
                Result = wins[i] ? PlayerResult.Win : PlayerResult.Los,
                TierUpgrades = [],
                Refineries = []
            });
            context.ReplayPlayerRatings.Add(new ReplayPlayerRating
            {
                ReplayPlayerRatingId = replayId * 10000 + i + 1,
                ReplayRatingId = replayRatingId,
                ReplayPlayerId = replayPlayerId,
                PlayerId = playerId,
                RatingType = RatingType.Commanders,
                RatingBefore = 2000,
                RatingDelta = gains[i]
            });
        }

        await context.SaveChangesAsync();
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly IMemoryCache cache;

        private TestFixture(
            SqliteConnection connection,
            DsstatsContext context,
            DbContextOptions<DsstatsContext> options,
            IMemoryCache cache)
        {
            this.connection = connection;
            this.cache = cache;
            Context = context;
            Provider = new WinrateStatsProvider(new TestDbContextFactory<DsstatsContext>(options), cache);
        }

        public DsstatsContext Context { get; }
        public WinrateStatsProvider Provider { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<DsstatsContext>().UseSqlite(connection).Options;
            var context = new DsstatsContext(options);
            await context.Database.EnsureCreatedAsync();
            var cache = new MemoryCache(new MemoryCacheOptions());
            return new TestFixture(connection, context, options, cache);
        }

        public async ValueTask DisposeAsync()
        {
            cache.Dispose();
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
