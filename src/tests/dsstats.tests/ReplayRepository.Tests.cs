using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayRepositoryTests
{
    [TestMethod]
    public async Task GetReplays_SortsByLeaverType_WithAndWithoutRatings()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 5, 1), leaverType: LeaverType.None);
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 5, 2), leaverType: LeaverType.OneLeaver);
        await SeedReplayAsync(fixture.Context, replayId: 3, gametime: new DateTime(2026, 4, 30), seedRating: false);
        await SeedReplayAsync(fixture.Context, replayId: 4, gametime: new DateTime(2026, 5, 4), leaverType: LeaverType.TwoSameTeam);

        var ascending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 10,
            TableOrders =
            [
                new() { Column = "LeaverType", Ascending = true },
                new() { Column = "GameTime", Ascending = true }
            ]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-3", "hash-1", "hash-2", "hash-4" },
            ascending.Select(s => s.ReplayHash).ToArray());
        Assert.AreEqual(LeaverType.None, ascending[0].LeaverType);
        Assert.IsNull(ascending[0].AvgRating);
        Assert.IsNull(ascending[0].Exp2Win);

        var descending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 10,
            TableOrders = [new() { Column = "LeaverType", Ascending = false }]
        });

        Assert.AreEqual("hash-4", descending[0].ReplayHash);
        Assert.AreEqual(LeaverType.TwoSameTeam, descending[0].LeaverType);
    }

    [TestMethod]
    public async Task GetReplays_SortsByAverageRatingAndExpectedWinProbability()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 5, 1), avgRating: 1500, exp2Win: 0.40);
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 5, 2), avgRating: 2000, exp2Win: 0.80);
        await SeedReplayAsync(fixture.Context, replayId: 3, gametime: new DateTime(2026, 5, 3), seedRating: false);
        await SeedReplayAsync(fixture.Context, replayId: 4, gametime: new DateTime(2026, 5, 4), avgRating: 1000, exp2Win: 0.20);

        var byAverageRating = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 10,
            TableOrders = [new() { Column = "AvgRating", Ascending = false }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-2", "hash-1", "hash-4", "hash-3" },
            byAverageRating.Select(s => s.ReplayHash).ToArray());

        var byExpectedWin = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 10,
            TableOrders = [new() { Column = "Exp2Win", Ascending = true }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-3", "hash-4", "hash-1", "hash-2" },
            byExpectedWin.Select(s => s.ReplayHash).ToArray());
    }

    [TestMethod]
    public async Task GetReplays_DefaultGameTimeSort_LoadsRatingsForPage()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 5, 1), avgRating: 1500, exp2Win: 0.40);
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 5, 2), avgRating: 2000, exp2Win: 0.80, leaverType: LeaverType.OneLeaver);
        await SeedReplayAsync(fixture.Context, replayId: 3, gametime: new DateTime(2026, 5, 3), seedRating: false);
        await SeedReplayAsync(fixture.Context, replayId: 4, gametime: new DateTime(2026, 5, 4), avgRating: 1000, exp2Win: 0.20, leaverType: LeaverType.TwoSameTeam);

        var replays = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 4
        });

        CollectionAssert.AreEqual(
            new[] { "hash-4", "hash-3", "hash-2", "hash-1" },
            replays.Select(s => s.ReplayHash).ToArray());
        Assert.AreEqual(1000, replays[0].AvgRating);
        Assert.AreEqual(0.20, replays[0].Exp2Win);
        Assert.AreEqual(LeaverType.TwoSameTeam, replays[0].LeaverType);
        Assert.IsNull(replays[1].AvgRating);
        Assert.IsNull(replays[1].Exp2Win);
        Assert.AreEqual(LeaverType.None, replays[1].LeaverType);
    }

    [TestMethod]
    public async Task GetReplays_DefaultGameTimeSort_LoadsReplayUserRatingsOnlyWhenRequested()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            replayUserVoteCount: 2,
            replayUserScoreSum: 8);

        var defaultReplays = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            Take = 10
        });

        Assert.IsNull(defaultReplays[0].ReplayUserVoteCount);
        Assert.IsNull(defaultReplays[0].ReplayUserScore);

        var requestedReplays = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            RatingType = RatingType.Commanders,
            IncludeReplayUserRatings = true,
            Take = 10
        });

        Assert.AreEqual(2, requestedReplays[0].ReplayUserVoteCount);
        Assert.AreEqual(4.0, requestedReplays[0].ReplayUserScore);
    }

    [TestMethod]
    public async Task GetReplays_RatedOnly_ExcludesUnratedAndZeroVoteReplays()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            replayUserVoteCount: 2,
            replayUserScoreSum: 8);
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 5, 2));
        await SeedReplayAsync(
            fixture.Context,
            replayId: 3,
            gametime: new DateTime(2026, 5, 3),
            replayUserVoteCount: 0,
            replayUserScoreSum: 0);

        var request = new ReplaysRequest
        {
            IncludeReplayUserRatings = true,
            Filter = new() { RatedOnly = true },
            Take = 10
        };

        var count = await fixture.Repository.GetReplaysCount(request);
        var replays = await fixture.Repository.GetReplays(request);

        Assert.AreEqual(1, count);
        Assert.AreEqual(1, replays.Count);
        Assert.AreEqual("hash-1", replays[0].ReplayHash);
        Assert.AreEqual(2, replays[0].ReplayUserVoteCount);
        Assert.AreEqual(4.0, replays[0].ReplayUserScore);
    }

    [TestMethod]
    public async Task GetReplays_WithSpawnPlayback_ExcludesReplaysWithoutSpawnPlayback()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            seedSpawnPlayback: true);
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 5, 2));
        await SeedReplayAsync(
            fixture.Context,
            replayId: 3,
            gametime: new DateTime(2026, 5, 3),
            seedSpawnPlayback: true);

        var request = new ReplaysRequest
        {
            Filter = new() { WithSpawnPlayback = true },
            Take = 10
        };

        var count = await fixture.Repository.GetReplaysCount(request);
        var replays = await fixture.Repository.GetReplays(request);

        Assert.AreEqual(2, count);
        CollectionAssert.AreEqual(
            new[] { "hash-3", "hash-1" },
            replays.Select(s => s.ReplayHash).ToArray());
    }

    [TestMethod]
    public async Task GetReplays_WithSpawnPlayback_ComposesWithRatedOnly()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            replayUserVoteCount: 2,
            replayUserScoreSum: 8,
            seedSpawnPlayback: true);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 2,
            gametime: new DateTime(2026, 5, 2),
            replayUserVoteCount: 2,
            replayUserScoreSum: 10);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 3,
            gametime: new DateTime(2026, 5, 3),
            seedSpawnPlayback: true);

        var request = new ReplaysRequest
        {
            IncludeReplayUserRatings = true,
            Filter = new()
            {
                RatedOnly = true,
                WithSpawnPlayback = true
            },
            Take = 10
        };

        var count = await fixture.Repository.GetReplaysCount(request);
        var replays = await fixture.Repository.GetReplays(request);

        Assert.AreEqual(1, count);
        Assert.AreEqual(1, replays.Count);
        Assert.AreEqual("hash-1", replays[0].ReplayHash);
        Assert.AreEqual(2, replays[0].ReplayUserVoteCount);
    }

    [TestMethod]
    public async Task GetReplays_SortsByReplayUserVoteCount()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            replayUserVoteCount: 1,
            replayUserScoreSum: 5);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 2,
            gametime: new DateTime(2026, 5, 2),
            replayUserVoteCount: 5,
            replayUserScoreSum: 20);

        var ascending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            Filter = new() { RatedOnly = true },
            Take = 10,
            TableOrders = [new() { Column = nameof(ReplayListDto.ReplayUserVoteCount), Ascending = true }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-1", "hash-2" },
            ascending.Select(s => s.ReplayHash).ToArray());

        var descending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            Filter = new() { RatedOnly = true },
            Take = 10,
            TableOrders = [new() { Column = nameof(ReplayListDto.ReplayUserVoteCount), Ascending = false }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-2", "hash-1" },
            descending.Select(s => s.ReplayHash).ToArray());
    }

    [TestMethod]
    public async Task GetReplays_SortsByReplayUserScoreThenVoteCountAndGameTime()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            replayUserVoteCount: 1,
            replayUserScoreSum: 5);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 2,
            gametime: new DateTime(2026, 5, 2),
            replayUserVoteCount: 3,
            replayUserScoreSum: 15);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 3,
            gametime: new DateTime(2026, 5, 3),
            replayUserVoteCount: 10,
            replayUserScoreSum: 45);

        var descending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            Filter = new() { RatedOnly = true },
            Take = 10,
            TableOrders = [new() { Column = nameof(ReplayListDto.ReplayUserScore), Ascending = false }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-2", "hash-1", "hash-3" },
            descending.Select(s => s.ReplayHash).ToArray());

        var ascending = await fixture.Repository.GetReplays(new ReplaysRequest
        {
            Filter = new() { RatedOnly = true },
            Take = 10,
            TableOrders = [new() { Column = nameof(ReplayListDto.ReplayUserScore), Ascending = true }]
        });

        CollectionAssert.AreEqual(
            new[] { "hash-3", "hash-1", "hash-2" },
            ascending.Select(s => s.ReplayHash).ToArray());
    }

    [TestMethod]
    public async Task GetReplays_NameFilter_DoesNotDuplicateReplayRows()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(
            fixture.Context,
            replayId: 1,
            gametime: new DateTime(2026, 5, 1),
            playerNames: ["Duplicate", "Duplicate", "OtherA", "OtherB", "OtherC", "OtherD"]);
        await SeedReplayAsync(
            fixture.Context,
            replayId: 2,
            gametime: new DateTime(2026, 5, 2),
            playerNames: ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot"]);

        var request = new ReplaysRequest
        {
            Name = "Duplicate",
            RatingType = RatingType.Commanders,
            Take = 10
        };

        var count = await fixture.Repository.GetReplaysCount(request);
        var replays = await fixture.Repository.GetReplays(request);

        Assert.AreEqual(1, count);
        Assert.AreEqual(1, replays.Count);
        Assert.AreEqual("hash-1", replays[0].ReplayHash);
    }

    private static async Task SeedReplayAsync(
        DsstatsContext context,
        int replayId,
        DateTime gametime,
        LeaverType leaverType = LeaverType.None,
        int avgRating = 1500,
        double exp2Win = 0.50,
        bool seedRating = true,
        int? replayUserVoteCount = null,
        int replayUserScoreSum = 0,
        bool seedSpawnPlayback = false,
        string[]? playerNames = null)
    {
        playerNames ??= ["PlayerA", "PlayerB", "PlayerC", "PlayerD", "PlayerE", "PlayerF"];
        Commander[] commanders =
        [
            Commander.Abathur,
            Commander.Alarak,
            Commander.Artanis,
            Commander.Dehaka,
            Commander.Fenix,
            Commander.Horner
        ];

        var replay = new Replay
        {
            ReplayId = replayId,
            FileName = $"Replay-{replayId}.SC2Replay",
            Title = $"Replay {replayId}",
            Version = "1.0",
            GameMode = GameMode.Commanders,
            RegionId = 1,
            TE = false,
            PlayerCount = 6,
            Gametime = gametime,
            BaseBuild = 90000,
            Duration = 900,
            WinnerTeam = 1,
            ReplayHash = $"hash-{replayId}",
            CompatHash = $"compat-{replayId}",
            Imported = gametime.AddMinutes(1),
            Uploaded = true
        };

        var players = new List<Player>(6);
        var replayPlayers = new List<ReplayPlayer>(6);

        for (int i = 0; i < 6; i++)
        {
            var playerId = replayId * 100 + i + 1;
            var replayPlayerId = replayId * 1000 + i + 1;
            var teamId = i < 3 ? 1 : 2;

            players.Add(new Player
            {
                PlayerId = playerId,
                Name = playerNames[i],
                ToonId = new ToonId
                {
                    Region = 1,
                    Realm = 1,
                    Id = playerId
                }
            });

            replayPlayers.Add(new ReplayPlayer
            {
                ReplayPlayerId = replayPlayerId,
                Name = playerNames[i],
                Race = commanders[i],
                SelectedRace = commanders[i],
                OppRace = Commander.None,
                TeamId = teamId,
                GamePos = i + 1,
                Duration = replay.Duration,
                Result = teamId == 1 ? PlayerResult.Win : PlayerResult.Los,
                TierUpgrades = [],
                Refineries = [],
                ReplayId = replayId,
                PlayerId = playerId
            });
        }

        context.Players.AddRange(players);
        context.Replays.Add(replay);
        context.ReplayPlayers.AddRange(replayPlayers);

        if (seedRating)
        {
            context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayId * 10,
                RatingType = RatingType.Commanders,
                LeaverType = leaverType,
                ExpectedWinProbability = exp2Win,
                AvgRating = avgRating,
                ReplayId = replayId
            });
        }

        if (replayUserVoteCount.HasValue)
        {
            context.ReplayUserRatingSummaries.Add(new ReplayUserRatingSummary
            {
                ReplayUserRatingSummaryId = replayId * 100,
                ReplayId = replayId,
                VoteCount = replayUserVoteCount.Value,
                ScoreSum = replayUserScoreSum,
                UpdatedAt = gametime
            });
        }

        if (seedSpawnPlayback)
        {
            context.ReplaySpawnPlaybacks.Add(new ReplaySpawnPlayback
            {
                ReplayId = replayId,
                FormatVersion = SpawnPlaybackSidecarCodec.FormatVersion,
                Compression = SpawnPlaybackCompression.Brotli,
                CompressedLength = 3,
                UncompressedLength = 10,
                UnitCount = 1,
                Payload = [1, 2, 3],
                CreatedAt = gametime
            });
        }

        await context.SaveChangesAsync();
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, ServiceProvider serviceProvider, DsstatsContext context)
        {
            Connection = connection;
            ServiceProvider = serviceProvider;
            Context = context;
            Repository = new ReplayRepository(
                serviceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>(),
                NullLogger<ReplayRepository>.Instance);
        }

        public SqliteConnection Connection { get; }
        public ServiceProvider ServiceProvider { get; }
        public DsstatsContext Context { get; }
        public ReplayRepository Repository { get; }

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
