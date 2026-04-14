using dsstats.db;
using dsstats.dbServices.Stats;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class SynergyStatsProviderTests
{
    [TestMethod]
    public async Task GetStatsAsync_ShouldReturnGroupedCommanderTeammateStats()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 2, 15));
        var provider = new SynergyStatsProvider(fixture.Context, fixture.MemoryCache);

        var response = await provider.GetStatsAsync(CreateRequest());

        Assert.AreEqual(12, response.SynergyEnts.Count);

        var abathurAlarak = response.SynergyEnts.FirstOrDefault(x =>
            x.Commander == Commander.Abathur && x.Teammate == Commander.Alarak);
        Assert.IsNotNull(abathurAlarak);
        Assert.AreEqual(1, abathurAlarak.Games);
        Assert.AreEqual(1, abathurAlarak.Wins);
        Assert.AreEqual(1.0, abathurAlarak.Winrate, 0.0001);
        Assert.AreEqual(10.0, abathurAlarak.AvgGain, 0.0001);

        var dehakaFenix = response.SynergyEnts.FirstOrDefault(x =>
            x.Commander == Commander.Dehaka && x.Teammate == Commander.Fenix);
        Assert.IsNotNull(dehakaFenix);
        Assert.AreEqual(1, dehakaFenix.Games);
        Assert.AreEqual(0, dehakaFenix.Wins);
        Assert.AreEqual(0.0, dehakaFenix.Winrate, 0.0001);
        Assert.AreEqual(-5.0, dehakaFenix.AvgGain, 0.0001);
    }

    [TestMethod]
    public async Task GetStatsAsync_ShouldApplyAllRequestFilters()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var provider = new SynergyStatsProvider(fixture.Context, fixture.MemoryCache);

        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 2, 15));
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2025, 1, 15)); // date
        await SeedReplayAsync(fixture.Context, replayId: 3, gametime: new DateTime(2026, 2, 20), duration: 650); // duration
        await SeedReplayAsync(fixture.Context, replayId: 4, gametime: new DateTime(2026, 2, 20), ratingBefore: 1900); // rating
        await SeedReplayAsync(fixture.Context, replayId: 5, gametime: new DateTime(2026, 2, 20), expectedWinProbability: 0.20); // exp2win
        await SeedReplayAsync(fixture.Context, replayId: 6, gametime: new DateTime(2026, 2, 20), avgRating: 1800); // team rating
        await SeedReplayAsync(fixture.Context, replayId: 7, gametime: new DateTime(2026, 2, 20), leaverType: LeaverType.OneLeaver); // leaver
        await SeedReplayAsync(fixture.Context, replayId: 8, gametime: new DateTime(2026, 2, 20), ratingType: RatingType.Standard); // rating type

        var request = CreateRequest();
        request.Filter = new StatsFilter
        {
            DateRange = new() { From = new DateTime(2026, 2, 1), To = new DateTime(2026, 3, 31) },
            RatingRange = new() { From = 2000, To = 2400 },
            DurationRange = new() { From = 700, To = 1000 },
            Exp2WinRange = new() { From = 45, To = 65 },
            TeamRatingRange = new() { From = 2000, To = 2300 }
        };

        var response = await provider.GetStatsAsync(request);

        Assert.AreEqual(12, response.SynergyEnts.Sum(x => x.Games));
        Assert.IsTrue(response.SynergyEnts.All(x => x.Games == 1));
    }

    [TestMethod]
    public async Task GetStatsAsync_ShouldExcludeInvalidRows()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var provider = new SynergyStatsProvider(fixture.Context, fixture.MemoryCache);

        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 2, 15)); // valid
        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 2, 16), gameMode: GameMode.Standard); // invalid mode
        await SeedReplayAsync(fixture.Context, replayId: 3, gametime: new DateTime(2026, 2, 17), playerCount: 4); // invalid player count
        await SeedReplayAsync(
            fixture.Context,
            replayId: 4,
            gametime: new DateTime(2026, 2, 18),
            team1: [Commander.Protoss, Commander.Terran, Commander.Zerg],
            team2: [Commander.Protoss, Commander.Terran, Commander.Zerg]); // invalid races

        var response = await provider.GetStatsAsync(CreateRequest());

        Assert.AreEqual(12, response.SynergyEnts.Sum(x => x.Games));
        Assert.IsTrue(response.SynergyEnts.All(x => (int)x.Commander > 3 && (int)x.Teammate > 3));
        Assert.IsTrue(response.SynergyEnts.All(x => x.Commander != x.Teammate));
    }

    [TestMethod]
    public async Task GetStatsAsync_ShouldReuseCacheForSameRequest_AndRefreshOnDifferentKey()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var provider = new SynergyStatsProvider(fixture.Context, fixture.MemoryCache);
        var request = CreateRequest();

        await SeedReplayAsync(fixture.Context, replayId: 1, gametime: new DateTime(2026, 2, 15));
        var first = await provider.GetStatsAsync(request);
        Assert.AreEqual(12, first.SynergyEnts.Sum(x => x.Games));

        await SeedReplayAsync(fixture.Context, replayId: 2, gametime: new DateTime(2026, 2, 20));

        var cached = await provider.GetStatsAsync(request);
        Assert.AreEqual(12, cached.SynergyEnts.Sum(x => x.Games));

        var requestWithDifferentKey = CreateRequest();
        requestWithDifferentKey.Filter = new StatsFilter
        {
            DateRange = new() { From = new DateTime(2020, 1, 1), To = new DateTime(2030, 1, 1) },
            RatingRange = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating },
            DurationRange = new() { From = Data.MinDuration, To = Data.MaxDuration },
            Exp2WinRange = new() { From = 0, To = 100 },
            TeamRatingRange = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating }
        };

        var refreshed = await provider.GetStatsAsync(requestWithDifferentKey);
        Assert.AreEqual(24, refreshed.SynergyEnts.Sum(x => x.Games));
    }

    private static StatsRequest CreateRequest()
    {
        return new StatsRequest
        {
            Type = StatsType.Synergy,
            TimePeriod = TimePeriod.AllTime,
            RatingType = RatingType.Commanders,
            Interest = Commander.None,
            WithLeavers = false
        };
    }

    private static async Task SeedReplayAsync(
        DsstatsContext context,
        int replayId,
        DateTime gametime,
        GameMode gameMode = GameMode.Commanders,
        int playerCount = 6,
        RatingType ratingType = RatingType.Commanders,
        LeaverType leaverType = LeaverType.None,
        double expectedWinProbability = 0.55,
        int avgRating = 2100,
        int duration = 900,
        double ratingBefore = 2200,
        Commander[]? team1 = null,
        Commander[]? team2 = null,
        bool team1Wins = true,
        double[]? ratingDeltas = null)
    {
        team1 ??= [Commander.Abathur, Commander.Alarak, Commander.Artanis];
        team2 ??= [Commander.Dehaka, Commander.Fenix, Commander.Horner];
        ratingDeltas ??= [10, 20, 30, -5, -10, -15];

        var replay = new Replay
        {
            ReplayId = replayId,
            FileName = $"Replay-{replayId}.SC2Replay",
            Title = $"Replay {replayId}",
            Version = "1.0",
            GameMode = gameMode,
            RegionId = 1,
            TE = false,
            PlayerCount = playerCount,
            Gametime = gametime,
            BaseBuild = 90000,
            Duration = duration,
            Cannon = 0,
            Bunker = 0,
            WinnerTeam = team1Wins ? 1 : 2,
            ReplayHash = $"hash-{replayId}",
            CompatHash = $"compat-{replayId}",
            Imported = gametime.AddMinutes(5),
            Uploaded = true
        };

        var replayRatingId = replayId * 10;
        var replayRating = new ReplayRating
        {
            ReplayRatingId = replayRatingId,
            RatingType = ratingType,
            LeaverType = leaverType,
            ExpectedWinProbability = expectedWinProbability,
            IsPreRating = false,
            AvgRating = avgRating,
            ReplayId = replayId
        };

        var players = new List<Player>(6);
        var replayPlayers = new List<ReplayPlayer>(6);
        var replayPlayerRatings = new List<ReplayPlayerRating>(6);

        for (var i = 0; i < 6; i++)
        {
            var playerId = replayId * 100 + i + 1;
            var replayPlayerId = replayId * 1000 + i + 1;
            var teamId = i < 3 ? 1 : 2;
            var race = i < 3 ? team1[i] : team2[i - 3];
            var result = team1Wins
                ? (teamId == 1 ? PlayerResult.Win : PlayerResult.Los)
                : (teamId == 1 ? PlayerResult.Los : PlayerResult.Win);

            players.Add(new Player
            {
                PlayerId = playerId,
                Name = $"Player{playerId}",
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
                Name = $"ReplayPlayer{playerId}",
                Clan = null,
                Race = race,
                SelectedRace = race,
                OppRace = Commander.None,
                TeamId = teamId,
                GamePos = i + 1,
                Duration = duration,
                Result = result,
                Apm = 100,
                Messages = 0,
                Pings = 0,
                TierUpgrades = [],
                Refineries = [],
                IsMvp = false,
                IsUploader = i == 0,
                ReplayId = replayId,
                PlayerId = playerId
            });

            replayPlayerRatings.Add(new ReplayPlayerRating
            {
                ReplayPlayerRatingId = replayId * 10000 + i + 1,
                RatingType = ratingType,
                RatingBefore = ratingBefore,
                RatingDelta = ratingDeltas[i],
                ExpectedDelta = 0,
                Games = 50,
                ReplayRatingId = replayRatingId,
                ReplayPlayerId = replayPlayerId,
                PlayerId = playerId
            });
        }

        context.Players.AddRange(players);
        context.Replays.Add(replay);
        context.ReplayRatings.Add(replayRating);
        context.ReplayPlayers.AddRange(replayPlayers);
        context.ReplayPlayerRatings.AddRange(replayPlayerRatings);
        await context.SaveChangesAsync();
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, DsstatsContext context, IMemoryCache memoryCache)
        {
            Connection = connection;
            Context = context;
            MemoryCache = memoryCache;
        }

        public SqliteConnection Connection { get; }
        public DsstatsContext Context { get; }
        public IMemoryCache MemoryCache { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite(connection, o => o.MigrationsAssembly("dsstats.migrations.sqlite"))
                .Options;

            var context = new DsstatsContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            return new TestFixture(connection, context, cache);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
            (MemoryCache as MemoryCache)?.Dispose();
        }
    }
}
