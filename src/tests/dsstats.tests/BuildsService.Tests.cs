using dsstats.db;
using dsstats.dbServices.Builds;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class BuildsServiceTests
{
    [TestMethod]
    public async Task GetBuildResponse_AndUpgradeTimings_ReturnUpgradeSpendAndAverageTiming()
    {
        await using var fixture = await TestFixture.CreateAsync();
        for (var i = 0; i < 10; i++)
        {
            await fixture.SeedReplayPlayerAsync(
                replayId: i + 1,
                playerId: 101 + i,
                commander: Commander.Abathur,
                oppCommander: Commander.Alarak,
                min5UpgradeSpent: i < 5 ? 100 : 300,
                allUpgradeSpent: i < 5 ? 400 : 500,
                min5GasCount: i < 5 ? 1 : 2,
                allGasCount: 3,
                refineries: i < 9
                    ? [i < 5 ? 100 : 200, i < 5 ? 200 : 280, 290]
                    : [200, 280, 400],
                upgrades:
                [
                    ("BlinkTech", i < 5 ? 100 : 200),
                    ("Charge", i < 5 ? 200 : 280),
                    ("VanadiumPlatingController", 150),
                    ("PlayerStateVictory", 250),
                    ("LateUpgrade", 400)
                ]);
        }

        for (var i = 0; i < 9; i++)
        {
            await fixture.AddUpgradeAsync(replayPlayerId: (i + 1) * 1000 + 1, upgradeName: "RareUpgrade", time: 120);
        }

        await fixture.SeedReplayPlayerAsync(
            replayId: 11,
            playerId: 111,
            commander: Commander.Fenix,
            oppCommander: Commander.Alarak,
            min5UpgradeSpent: 900,
            allUpgradeSpent: 900,
            upgrades: [("IgnoredCommanderUpgrade", 120)]);

        var request = CreateRequest(Breakpoint.Min5);
        var response = await fixture.Service.GetBuildResponse(request);
        var timings = await fixture.Service.GetUpgradeTimings(request);
        var anecdotalTimings = await fixture.Service.GetUpgradeTimings(request, includeAnecdotal: true);
        var gasTimings = await fixture.Service.GetGasTimings(request);
        var anecdotalGasTimings = await fixture.Service.GetGasTimings(request, includeAnecdotal: true);

        Assert.AreEqual(1.5, response.Stats.Gas, 0.0001);
        Assert.AreEqual(200.0, response.Stats.Upgrades, 0.0001);
        Assert.AreEqual(2, timings.Count);

        var blink = timings.Single(x => x.Upgrade == "BlinkTech");
        Assert.AreEqual(150.0, blink.AverageTimeSeconds, 0.0001);
        Assert.AreEqual(10, blink.Count);
        Assert.AreEqual(100.0, blink.UsagePercent, 0.0001);

        var charge = timings.Single(x => x.Upgrade == "Charge");
        Assert.AreEqual(240.0, charge.AverageTimeSeconds, 0.0001);
        Assert.AreEqual(10, charge.Count);
        Assert.IsFalse(timings.Any(x => x.Upgrade.StartsWith("PlayerState")));
        Assert.IsFalse(timings.Any(x => x.Upgrade == "VanadiumPlatingController"));
        Assert.IsFalse(timings.Any(x => x.Upgrade == "LateUpgrade"));
        Assert.IsFalse(timings.Any(x => x.Upgrade == "RareUpgrade"));
        Assert.AreEqual(3, anecdotalTimings.Count);
        Assert.AreEqual(9, anecdotalTimings.Single(x => x.Upgrade == "RareUpgrade").Count);
        Assert.IsFalse(anecdotalTimings.Any(x => x.Upgrade.StartsWith("PlayerState")));
        Assert.IsFalse(anecdotalTimings.Any(x => x.Upgrade == "VanadiumPlatingController"));
        Assert.IsFalse(anecdotalTimings.Any(x => x.Upgrade == "LateUpgrade"));

        var allTimings = await fixture.Service.GetUpgradeTimings(CreateRequest(Breakpoint.All));
        Assert.IsTrue(allTimings.Any(x => x.Upgrade == "LateUpgrade"));

        Assert.AreEqual(2, gasTimings.Count);
        Assert.AreEqual(150.0, gasTimings.Single(x => x.Gas == 1).AverageTimeSeconds, 0.0001);
        Assert.AreEqual(240.0, gasTimings.Single(x => x.Gas == 2).AverageTimeSeconds, 0.0001);
        Assert.AreEqual(10, gasTimings.Single(x => x.Gas == 1).Count);
        Assert.IsFalse(gasTimings.Any(x => x.Gas == 3));
        Assert.AreEqual(3, anecdotalGasTimings.Count);
        Assert.AreEqual(9, anecdotalGasTimings.Single(x => x.Gas == 3).Count);
        Assert.AreEqual(290.0, anecdotalGasTimings.Single(x => x.Gas == 3).AverageTimeSeconds, 0.0001);

        var allGasTimings = await fixture.Service.GetGasTimings(CreateRequest(Breakpoint.All));
        Assert.IsTrue(allGasTimings.Any(x => x.Gas == 3));
    }

    [TestMethod]
    public async Task GetUpgradeTimings_AppliesCommanderVersusPlayerRatingAndLeaverFilters()
    {
        await using var fixture = await TestFixture.CreateAsync();
        for (var i = 0; i < 10; i++)
        {
            await fixture.SeedReplayPlayerAsync(
                replayId: 100 + i,
                playerId: 201,
                commander: Commander.Abathur,
                oppCommander: Commander.Alarak,
                ratingBefore: 1800,
                refineries: [120],
                upgrades: [("IncludedUpgrade", 120)]);
            await fixture.SeedReplayPlayerAsync(
                replayId: 200 + i,
                playerId: 202 + i,
                commander: Commander.Abathur,
                oppCommander: Commander.Alarak,
                ratingBefore: 900,
                refineries: [120],
                upgrades: [("LowRatingUpgrade", 120)]);
            await fixture.SeedReplayPlayerAsync(
                replayId: 300 + i,
                playerId: 302 + i,
                commander: Commander.Abathur,
                oppCommander: Commander.Dehaka,
                ratingBefore: 1800,
                refineries: [120],
                upgrades: [("WrongVersusUpgrade", 120)]);
            await fixture.SeedReplayPlayerAsync(
                replayId: 400 + i,
                playerId: 402 + i,
                commander: Commander.Abathur,
                oppCommander: Commander.Alarak,
                leaverType: LeaverType.OneLeaver,
                ratingBefore: 1800,
                refineries: [120],
                upgrades: [("LeaverUpgrade", 120)]);
            await fixture.SeedReplayPlayerAsync(
                replayId: 500 + i,
                playerId: 502 + i,
                commander: Commander.Fenix,
                oppCommander: Commander.Alarak,
                ratingBefore: 1800,
                refineries: [120],
                upgrades: [("WrongCommanderUpgrade", 120)]);
        }

        var request = CreateRequest(Breakpoint.Min5);
        request.Players = [CreatePlayerDto(201)];

        var timings = await fixture.Service.GetUpgradeTimings(request);
        var anecdotalTimings = await fixture.Service.GetUpgradeTimings(request, includeAnecdotal: true);
        var gasTimings = await fixture.Service.GetGasTimings(request);
        var anecdotalGasTimings = await fixture.Service.GetGasTimings(request, includeAnecdotal: true);

        Assert.AreEqual(1, timings.Count);
        Assert.AreEqual("IncludedUpgrade", timings[0].Upgrade);
        Assert.AreEqual(10, timings[0].Count);
        Assert.AreEqual(100.0, timings[0].UsagePercent, 0.0001);
        Assert.AreEqual(1, anecdotalTimings.Count);
        Assert.AreEqual("IncludedUpgrade", anecdotalTimings[0].Upgrade);
        Assert.AreEqual(1, gasTimings.Count);
        Assert.AreEqual(1, gasTimings[0].Gas);
        Assert.AreEqual(10, gasTimings[0].Count);
        Assert.AreEqual(1, anecdotalGasTimings.Count);
        Assert.AreEqual(1, anecdotalGasTimings[0].Gas);

        var ratingRequest = CreateRequest(Breakpoint.Min5);
        ratingRequest.FromRating = 1500;
        ratingRequest.ToRating = 2000;

        var ratingFilteredTimings = await fixture.Service.GetUpgradeTimings(ratingRequest);
        var anecdotalRatingFilteredTimings = await fixture.Service.GetUpgradeTimings(ratingRequest, includeAnecdotal: true);
        var ratingFilteredGasTimings = await fixture.Service.GetGasTimings(ratingRequest);
        var anecdotalRatingFilteredGasTimings = await fixture.Service.GetGasTimings(ratingRequest, includeAnecdotal: true);

        Assert.AreEqual(1, ratingFilteredTimings.Count);
        Assert.AreEqual("IncludedUpgrade", ratingFilteredTimings[0].Upgrade);
        Assert.AreEqual(1, anecdotalRatingFilteredTimings.Count);
        Assert.AreEqual("IncludedUpgrade", anecdotalRatingFilteredTimings[0].Upgrade);
        Assert.AreEqual(1, ratingFilteredGasTimings.Count);
        Assert.AreEqual(1, ratingFilteredGasTimings[0].Gas);
        Assert.AreEqual(1, anecdotalRatingFilteredGasTimings.Count);
        Assert.AreEqual(1, anecdotalRatingFilteredGasTimings[0].Gas);
    }

    private static BuildsRequest CreateRequest(Breakpoint breakpoint)
    {
        return new()
        {
            RatingType = RatingType.Commanders,
            TimePeriod = TimePeriod.AllTime,
            Interest = Commander.Abathur,
            Versus = Commander.Alarak,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            Breakpoint = breakpoint,
            WithSpawnInfo = false
        };
    }

    private static PlayerDto CreatePlayerDto(int playerId)
    {
        return new()
        {
            PlayerId = playerId,
            Name = $"Player{playerId}",
            ToonId = new() { Region = 1, Realm = 1, Id = playerId }
        };
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly Dictionary<string, Upgrade> upgrades = [];

        private TestFixture(SqliteConnection connection, DsstatsContext context, BuildsService service, IMemoryCache memoryCache)
        {
            Connection = connection;
            Context = context;
            Service = service;
            MemoryCache = memoryCache;
        }

        public SqliteConnection Connection { get; }
        public DsstatsContext Context { get; }
        public BuildsService Service { get; }
        public IMemoryCache MemoryCache { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite(connection, o => o.MigrationsAssembly("dsstats.migrations.sqlite"))
                .Options;

            var context = new DsstatsContext(options);
            var contextFactory = new TestDbContextFactory<DsstatsContext>(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new BuildsService(contextFactory, cache);
            return new TestFixture(connection, context, service, cache);
        }

        public async Task SeedReplayPlayerAsync(
            int replayId,
            int playerId,
            Commander commander,
            Commander oppCommander,
            int min5UpgradeSpent = 100,
            int allUpgradeSpent = 100,
            int min5GasCount = 0,
            int allGasCount = 0,
            double ratingBefore = 1800,
            LeaverType leaverType = LeaverType.None,
            RatingType ratingType = RatingType.Commanders,
            int[]? refineries = null,
            (string Upgrade, int Time)[]? upgrades = null)
        {
            upgrades ??= [];
            refineries ??= [];

            var replayPlayerId = replayId * 1000 + 1;
            var replayRatingId = replayId * 10;
            var player = Context.Players.Local.FirstOrDefault(x => x.PlayerId == playerId)
                ?? await Context.Players.FindAsync(playerId);
            var addPlayer = player is null;
            player ??= new Player
            {
                PlayerId = playerId,
                Name = $"Player{playerId}",
                ToonId = new ToonId { Region = 1, Realm = 1, Id = playerId }
            };
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
                Gametime = new DateTime(2026, 1, 1).AddDays(replayId),
                BaseBuild = 90000,
                Duration = 900,
                WinnerTeam = 1,
                ReplayHash = $"hash-{replayId}",
                CompatHash = $"compat-{replayId}",
                Imported = new DateTime(2026, 1, 1).AddDays(replayId).AddMinutes(1),
                Uploaded = true
            };
            var replayPlayer = new ReplayPlayer
            {
                ReplayPlayerId = replayPlayerId,
                Name = player.Name,
                Race = commander,
                SelectedRace = commander,
                OppRace = oppCommander,
                TeamId = 1,
                GamePos = 1,
                Duration = replay.Duration,
                Result = PlayerResult.Win,
                ReplayId = replayId,
                PlayerId = playerId,
                Refineries = refineries,
                Spawns =
                [
                    new() { Breakpoint = Breakpoint.Min5, GasCount = min5GasCount, UpgradeSpent = min5UpgradeSpent },
                    new() { Breakpoint = Breakpoint.All, GasCount = allGasCount, UpgradeSpent = allUpgradeSpent }
                ]
            };

            foreach (var upgrade in upgrades)
            {
                replayPlayer.Upgrades.Add(new PlayerUpgrade
                {
                    Gameloop = upgrade.Time,
                    Upgrade = GetUpgrade(upgrade.Upgrade)
                });
            }

            if (addPlayer)
            {
                Context.Players.Add(player);
            }
            Context.Replays.Add(replay);
            Context.ReplayPlayers.Add(replayPlayer);
            Context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayRatingId,
                RatingType = ratingType,
                LeaverType = leaverType,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800,
                ReplayId = replayId
            });
            Context.ReplayPlayerRatings.Add(new ReplayPlayerRating
            {
                ReplayPlayerRatingId = replayId * 10000 + 1,
                RatingType = ratingType,
                RatingBefore = ratingBefore,
                RatingDelta = 5,
                ExpectedDelta = 0,
                Games = 10,
                ReplayRatingId = replayRatingId,
                ReplayPlayerId = replayPlayerId,
                PlayerId = playerId
            });

            await Context.SaveChangesAsync();
        }

        public async Task AddUpgradeAsync(int replayPlayerId, string upgradeName, int time)
        {
            var replayPlayer = Context.ReplayPlayers.Local.First(x => x.ReplayPlayerId == replayPlayerId);
            replayPlayer.Upgrades.Add(new PlayerUpgrade
            {
                Gameloop = time,
                Upgrade = GetUpgrade(upgradeName)
            });

            await Context.SaveChangesAsync();
        }

        private Upgrade GetUpgrade(string name)
        {
            if (upgrades.TryGetValue(name, out var upgrade))
            {
                return upgrade;
            }

            upgrade = new Upgrade { Name = name };
            upgrades[name] = upgrade;
            return upgrade;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
            (MemoryCache as MemoryCache)?.Dispose();
        }
    }
}
