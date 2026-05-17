using dsstats.db;
using dsstats.dbServices.BuildDetails;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace dsstats.tests;

[TestClass]
public sealed class BuildDetailGenerationServiceTests
{
    [TestMethod]
    public async Task ProcessPendingBatchAsync_DetectsAndStoresNormalizedFacts()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedDetectableReplayAsync();

        var result = await fixture.Service.ProcessPendingBatchAsync();

        Assert.AreEqual(1, result.Candidates);
        Assert.AreEqual(1, result.Detected);
        Assert.AreEqual(0, result.NotDetectable);

        var detail = await fixture.Context.ReplayBuildDetails
            .Include(x => x.PlayerBuilds)
            .Include(x => x.TeamBuilds)
            .SingleAsync();

        Assert.AreEqual(ReplayBuildDetailStatus.Detected, detail.Status);
        Assert.AreEqual(BuildDetailGenerationService.CurrentDetectionVersion, detail.DetectionVersion);
        Assert.AreEqual(6, detail.PlayerBuilds.Count);
        Assert.AreEqual(1, detail.TeamBuilds.Count);

        var protoss = detail.PlayerBuilds.Single(x => x.GamePos == 1);
        Assert.AreEqual(Commander.Protoss, protoss.Commander);
        Assert.AreEqual((int)ProtossBuild.Stalker, protoss.Build);
        Assert.AreEqual(Commander.Terran, protoss.OppCommander);
        Assert.AreEqual((int)TerranBuild.Bio, protoss.OppBuild);
        Assert.AreEqual(1, protoss.Lane);
        Assert.IsTrue(protoss.Won);
        Assert.IsFalse(protoss.GasFirst);

        var teamBuild = detail.TeamBuilds.Single();
        Assert.AreEqual(1, teamBuild.TeamId);
        Assert.AreEqual(TeamBuild.PTStack, teamBuild.TeamBuild);
        Assert.AreEqual(101, teamBuild.LeaderReplayPlayerId);
        Assert.AreEqual(102, teamBuild.FollowerReplayPlayerId);
    }

    [TestMethod]
    public async Task ProcessPendingBatchAsync_IsIdempotent()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedDetectableReplayAsync();

        await fixture.Service.ProcessPendingBatchAsync();
        var secondResult = await fixture.Service.ProcessPendingBatchAsync();

        Assert.AreEqual(0, secondResult.Candidates);
        Assert.AreEqual(1, await fixture.Context.ReplayBuildDetails.CountAsync());
        Assert.AreEqual(6, await fixture.Context.ReplayPlayerBuildDetails.CountAsync());
        Assert.AreEqual(1, await fixture.Context.ReplayTeamBuildDetails.CountAsync());
    }

    [TestMethod]
    public async Task ProcessPendingBatchAsync_MarksStructurallyInvalidReplayAsNotDetectable()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedDetectableReplayAsync(withMin5Spawns: false);

        var result = await fixture.Service.ProcessPendingBatchAsync();
        var detail = await fixture.Context.ReplayBuildDetails.SingleAsync();

        Assert.AreEqual(1, result.Candidates);
        Assert.AreEqual(0, result.Detected);
        Assert.AreEqual(1, result.NotDetectable);
        Assert.AreEqual(ReplayBuildDetailStatus.NotDetectable, detail.Status);
        Assert.AreEqual(0, await fixture.Context.ReplayPlayerBuildDetails.CountAsync());

        var secondResult = await fixture.Service.ProcessPendingBatchAsync();
        Assert.AreEqual(0, secondResult.Candidates);
    }

    [TestMethod]
    public async Task StoredPlayerFacts_JoinToReplayPlayerRatingsForAverageGain()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedDetectableReplayAsync();
        await fixture.Service.ProcessPendingBatchAsync();

        var avgGain = await (
            from build in fixture.Context.ReplayPlayerBuildDetails
            join rating in fixture.Context.ReplayPlayerRatings
                on build.ReplayPlayerId equals rating.ReplayPlayerId
            where build.Commander == Commander.Protoss
                && build.Build == (int)ProtossBuild.Stalker
            select rating.RatingDelta)
            .AverageAsync();

        Assert.AreEqual(12.5, avgGain, 0.0001);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, DsstatsContext context, BuildDetailGenerationService service)
        {
            Connection = connection;
            Context = context;
            Service = service;
        }

        public SqliteConnection Connection { get; }
        public DsstatsContext Context { get; }
        public BuildDetailGenerationService Service { get; }

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

            var contextFactory = new TestDbContextFactory<DsstatsContext>(options);
            var service = new BuildDetailGenerationService(
                contextFactory,
                NullLogger<BuildDetailGenerationService>.Instance);

            return new TestFixture(connection, context, service);
        }

        public async Task SeedDetectableReplayAsync(bool withMin5Spawns = true)
        {
            var replay = new Replay
            {
                ReplayId = 1,
                FileName = "Replay-1.SC2Replay",
                Title = "Direct Strike TE",
                Version = "1.0",
                GameMode = GameMode.Standard,
                RegionId = 1,
                TE = true,
                PlayerCount = 6,
                Gametime = new DateTime(2026, 1, 1),
                BaseBuild = 90000,
                Duration = 900,
                WinnerTeam = 1,
                ReplayHash = "hash-1",
                CompatHash = "compat-1",
                Imported = new DateTime(2026, 1, 1, 0, 1, 0),
                Uploaded = true
            };

            Context.Replays.Add(replay);

            AddPlayer(1, 101, Commander.Protoss, PlayerResult.Win, withMin5Spawns, [("Stalker", 5)]);
            AddPlayer(2, 102, Commander.Terran, PlayerResult.Win, withMin5Spawns, [("Marine", 8), ("Marauder", 2)]);
            AddPlayer(3, 103, Commander.Zerg, PlayerResult.Win, withMin5Spawns, [("Roach", 4), ("Queen", 3)]);
            AddPlayer(4, 104, Commander.Terran, PlayerResult.Los, withMin5Spawns, [("Marine", 8)]);
            AddPlayer(5, 105, Commander.Zerg, PlayerResult.Los, withMin5Spawns, [("Hydralisk", 6)]);
            AddPlayer(6, 106, Commander.Protoss, PlayerResult.Los, withMin5Spawns, [("Zealot", 6)]);

            var replayRating = new ReplayRating
            {
                ReplayRatingId = 10,
                ReplayId = 1,
                RatingType = RatingType.StandardTE,
                LeaverType = LeaverType.None,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800
            };

            Context.ReplayRatings.Add(replayRating);
            foreach (var replayPlayerId in Enumerable.Range(101, 6))
            {
                Context.ReplayPlayerRatings.Add(new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayPlayerId * 10,
                    RatingType = RatingType.StandardTE,
                    RatingBefore = 1800,
                    RatingDelta = replayPlayerId == 101 ? 12.5 : 1,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRating.ReplayRatingId,
                    ReplayPlayerId = replayPlayerId,
                    PlayerId = replayPlayerId
                });
            }

            await Context.SaveChangesAsync();
        }

        private void AddPlayer(
            int gamePos,
            int replayPlayerId,
            Commander commander,
            PlayerResult result,
            bool withMin5Spawn,
            (string Unit, int Count)[] units)
        {
            var player = new Player
            {
                PlayerId = replayPlayerId,
                Name = $"Player{gamePos}",
                ToonId = new ToonId { Region = 1, Realm = 1, Id = replayPlayerId }
            };

            var replayPlayer = new ReplayPlayer
            {
                ReplayPlayerId = replayPlayerId,
                ReplayId = 1,
                PlayerId = replayPlayerId,
                Name = player.Name,
                Race = commander,
                SelectedRace = commander,
                TeamId = gamePos <= 3 ? 1 : 2,
                GamePos = gamePos,
                Duration = 900,
                Result = result,
                Player = player
            };

            if (withMin5Spawn)
            {
                replayPlayer.Spawns.Add(new Spawn
                {
                    Breakpoint = Breakpoint.Min5,
                    GasCount = gamePos == 2 ? 1 : 0,
                    Units = units.Select(x => new SpawnUnit
                    {
                        Unit = GetOrAddUnit(x.Unit),
                        Count = x.Count
                    }).ToList()
                });
            }

            Context.ReplayPlayers.Add(replayPlayer);
        }

        private Unit GetOrAddUnit(string name)
        {
            var unit = Context.Units.Local.FirstOrDefault(x => x.Name == name);
            if (unit is not null)
            {
                return unit;
            }

            unit = new Unit { Name = name };
            Context.Units.Add(unit);
            return unit;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
