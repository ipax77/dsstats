using dsstats.db;
using dsstats.dbServices.BuildDetails;
using dsstats.shared;
using dsstats.shared.DetailBuild;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class BuildDetailsServiceTests
{
    [TestMethod]
    public async Task GetOverview_AggregatesRatingGainWinrateAndGasFirstStats()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);
        await fixture.SeedReplayAsync(4, ProtossBuild.Zealots, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 6);

        var rows = await fixture.Service.GetOverview(CreateRequest());
        var stalker = rows.Single(x => x.Commander == Commander.Protoss && x.Build == (int)ProtossBuild.Stalker);

        Assert.AreEqual(3, stalker.Games);
        Assert.AreEqual(2, stalker.Wins);
        Assert.AreEqual(2.67, stalker.AverageRatingGain, 0.001);
        Assert.AreEqual(66.67, stalker.Winrate, 0.001);
        Assert.AreEqual(2, stalker.GasFirstGames);
        Assert.AreEqual(66.67, stalker.GasFirstRate, 0.001);
    }

    [TestMethod]
    public async Task GetMatchups_AggregatesSelectedBuildVersusOpponentBuild()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);
        await fixture.SeedReplayAsync(4, ProtossBuild.Stalker, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 8);

        var rows = await fixture.Service.GetMatchups(CreateMatchupRequest());
        var bio = rows.Single(x => x.OpponentCommander == Commander.Terran && x.OpponentBuild == (int)TerranBuild.Bio);

        Assert.AreEqual(3, bio.Games);
        Assert.AreEqual(2, bio.Wins);
        Assert.AreEqual(2.67, bio.AverageRatingGain, 0.001);
        Assert.AreEqual(2, bio.SelectedGasFirstGames);
        Assert.AreEqual(2, bio.OpponentGasFirstGames);
    }

    [TestMethod]
    public async Task GetMatchups_AppliesGasFirstFilters()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);

        var selectedSide = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.SelectedSide));
        var opponentSide = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.OpponentSide));
        var eitherSide = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.EitherSide));
        var bothSides = await fixture.Service.GetMatchups(CreateMatchupRequest(BuildDetailsGasFilter.BothSides));

        Assert.AreEqual(2, selectedSide.Single().Games);
        Assert.AreEqual(-1.0, selectedSide.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(2, opponentSide.Single().Games);
        Assert.AreEqual(6.0, opponentSide.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(3, eitherSide.Single().Games);
        Assert.AreEqual(1, bothSides.Single().Games);
        Assert.AreEqual(2.0, bothSides.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetSampleReplays_ReturnsBoundedClickableReplayHashesForSelectedMatchup()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2);

        var rows = await fixture.Service.GetSampleReplays(new BuildDetailsSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
            OpponentCommander = Commander.Terran,
            OpponentBuild = (int)TerranBuild.Bio,
            Count = 2,
        });

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("hash-3", rows[0].Replay.ReplayHash);
        Assert.AreEqual("hash-2", rows[1].Replay.ReplayHash);
        Assert.AreEqual(1, rows[0].Replay.PlayerPos);
        Assert.AreEqual(2.0, rows[0].Replay.PlayerGain, 0.001);
        Assert.IsTrue(rows[0].Replay.CommandersTeam1.Contains(Commander.Protoss));
        Assert.IsTrue(rows[0].Replay.CommandersTeam2.Contains(Commander.Terran));
    }

    [TestMethod]
    public async Task GetOverview_ExcludesLeaversUnlessRequested()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 30, leaverType: LeaverType.OneLeaver);

        var withoutLeavers = await fixture.Service.GetOverview(CreateRequest());
        var withLeaversRequest = CreateRequest();
        withLeaversRequest.WithLeavers = true;
        var withLeavers = await fixture.Service.GetOverview(withLeaversRequest);

        Assert.AreEqual(1, withoutLeavers.Single().Games);
        Assert.AreEqual(2, withLeavers.Single().Games);
        Assert.AreEqual(20.0, withLeavers.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetOverview_FiltersTeAndNonTeUsingMatchingRatingTypes()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, te: true);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, te: false);

        var all = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.All));
        var te = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.TE));
        var nonTe = await fixture.Service.GetOverview(CreateRequest(BuildDetailsTeFilter.NonTE));

        Assert.AreEqual(2, all.Single().Games);
        Assert.AreEqual(3.0, all.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(1, te.Single().Games);
        Assert.AreEqual(10.0, te.Single().AverageRatingGain, 0.001);
        Assert.AreEqual(1, nonTe.Single().Games);
        Assert.AreEqual(-4.0, nonTe.Single().AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetOverview_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(3, ProtossBuild.Zealots, TerranBuild.Mech, selectedGasFirst: true, opponentGasFirst: false, won: true, ratingDelta: 6, selectedPlayerId: 42);

        var request = CreateRequest();
        request.Player = CreatePlayer(42);
        var rows = await fixture.Service.GetOverview(request);

        Assert.AreEqual(2, rows.Count);
        var stalker = rows.Single(x => x.Build == (int)ProtossBuild.Stalker);
        var zealots = rows.Single(x => x.Build == (int)ProtossBuild.Zealots);
        Assert.AreEqual(1, stalker.Games);
        Assert.AreEqual(10.0, stalker.AverageRatingGain, 0.001);
        Assert.AreEqual(1, zealots.Games);
        Assert.AreEqual(6.0, zealots.AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetMatchups_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 20, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(4, ProtossBuild.Stalker, TerranBuild.Mech, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 8, selectedPlayerId: 42);

        var request = CreateMatchupRequest();
        request.Player = CreatePlayer(42);
        var rows = await fixture.Service.GetMatchups(request);

        Assert.AreEqual(2, rows.Count);
        var bio = rows.Single(x => x.OpponentBuild == (int)TerranBuild.Bio);
        var mech = rows.Single(x => x.OpponentBuild == (int)TerranBuild.Mech);
        Assert.AreEqual(2, bio.Games);
        Assert.AreEqual(3.0, bio.AverageRatingGain, 0.001);
        Assert.AreEqual(1, mech.Games);
        Assert.AreEqual(8.0, mech.AverageRatingGain, 0.001);
    }

    [TestMethod]
    public async Task GetSampleReplays_FiltersToSelectedPlayerAsBuildOwner()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: true, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);
        await fixture.SeedReplayAsync(3, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: true, opponentGasFirst: true, won: true, ratingDelta: 2, selectedPlayerId: 42);

        var rows = await fixture.Service.GetSampleReplays(new BuildDetailsSamplesRequest
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            Player = CreatePlayer(42),
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
            OpponentCommander = Commander.Terran,
            OpponentBuild = (int)TerranBuild.Bio,
            Count = 10,
        });

        CollectionAssert.AreEqual(new[] { "hash-3", "hash-1" }, rows.Select(x => x.Replay.ReplayHash).ToArray());
        Assert.IsTrue(rows.All(x => x.Replay.PlayerPos is 1));
    }

    [TestMethod]
    public async Task GetOverview_UsesSeparateCacheEntriesForPlayerFilteredRequests()
    {
        await using var fixture = await TestFixture.CreateAsync();
        await fixture.SeedReplayAsync(1, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: true, ratingDelta: 10, selectedPlayerId: 42);
        await fixture.SeedReplayAsync(2, ProtossBuild.Stalker, TerranBuild.Bio, selectedGasFirst: false, opponentGasFirst: false, won: false, ratingDelta: -4, selectedPlayerId: 99);

        var globalRows = await fixture.Service.GetOverview(CreateRequest());
        var request = CreateRequest();
        request.Player = CreatePlayer(42);
        var playerRows = await fixture.Service.GetOverview(request);

        Assert.AreEqual(2, globalRows.Single().Games);
        Assert.AreEqual(1, playerRows.Single().Games);
        Assert.AreNotEqual(CreateRequest().GetMemKey(), request.GetMemKey());
    }

    private static BuildDetailsRequest CreateRequest(BuildDetailsTeFilter teFilter = BuildDetailsTeFilter.All)
    {
        return new()
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            TeFilter = teFilter,
        };
    }

    private static BuildDetailsMatchupRequest CreateMatchupRequest(BuildDetailsGasFilter gasFilter = BuildDetailsGasFilter.Any)
    {
        return new()
        {
            RatingType = RatingType.All,
            TimePeriod = TimePeriod.AllTime,
            Commander = Commander.Protoss,
            FromRating = Data.MinBuildRating,
            ToRating = Data.MaxBuildRating,
            GasFilter = gasFilter,
            SelectedCommander = Commander.Protoss,
            SelectedBuild = (int)ProtossBuild.Stalker,
        };
    }

    private static PlayerDto CreatePlayer(int playerId)
    {
        return new()
        {
            PlayerId = playerId,
            Name = $"Player{playerId}",
            ToonId = new ToonIdDto { Region = 1, Realm = 1, Id = playerId }
        };
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(SqliteConnection connection, DsstatsContext context, BuildDetailsService service, IMemoryCache memoryCache)
        {
            Connection = connection;
            Context = context;
            Service = service;
            MemoryCache = memoryCache;
        }

        public SqliteConnection Connection { get; }
        public DsstatsContext Context { get; }
        public BuildDetailsService Service { get; }
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
            var service = new BuildDetailsService(contextFactory, cache);
            return new TestFixture(connection, context, service, cache);
        }

        public async Task SeedReplayAsync(
            int replayId,
            ProtossBuild selectedBuild,
            TerranBuild opponentBuild,
            bool selectedGasFirst,
            bool opponentGasFirst,
            bool won,
            double ratingDelta,
            LeaverType leaverType = LeaverType.None,
            bool te = true,
            int? selectedPlayerId = null,
            int? opponentPlayerId = null)
        {
            var ratingType = te ? RatingType.StandardTE : RatingType.Standard;
            var gametime = new DateTime(2026, 1, 1).AddDays(replayId);
            var selectedReplayPlayerId = replayId * 10 + 1;
            var opponentReplayPlayerId = replayId * 10 + 4;
            var selectedPersistentPlayerId = selectedPlayerId ?? selectedReplayPlayerId;
            var opponentPersistentPlayerId = opponentPlayerId ?? opponentReplayPlayerId;
            var replayRatingId = replayId * 100;
            var detailId = replayId * 1000;

            await AddPlayerIfMissing(selectedPersistentPlayerId, $"Protoss{selectedPersistentPlayerId}");
            await AddPlayerIfMissing(opponentPersistentPlayerId, $"Terran{opponentPersistentPlayerId}");

            Context.Replays.Add(new Replay
            {
                ReplayId = replayId,
                FileName = $"Replay-{replayId}.SC2Replay",
                Title = $"Replay {replayId}",
                Version = "1.0",
                GameMode = GameMode.Standard,
                RegionId = 1,
                TE = te,
                PlayerCount = 6,
                Gametime = gametime,
                Duration = 900,
                WinnerTeam = won ? 1 : 2,
                ReplayHash = $"hash-{replayId}",
                CompatHash = $"compat-{replayId}",
                Imported = gametime.AddMinutes(1),
                Uploaded = true
            });

            Context.ReplayPlayers.AddRange(
                new ReplayPlayer
                {
                    ReplayPlayerId = selectedReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = selectedPersistentPlayerId,
                    Name = $"Protoss{replayId}",
                    Race = Commander.Protoss,
                    SelectedRace = Commander.Protoss,
                    OppRace = Commander.Terran,
                    TeamId = 1,
                    GamePos = 1,
                    Duration = 900,
                    Result = won ? PlayerResult.Win : PlayerResult.Los
                },
                new ReplayPlayer
                {
                    ReplayPlayerId = opponentReplayPlayerId,
                    ReplayId = replayId,
                    PlayerId = opponentPersistentPlayerId,
                    Name = $"Terran{replayId}",
                    Race = Commander.Terran,
                    SelectedRace = Commander.Terran,
                    OppRace = Commander.Protoss,
                    TeamId = 2,
                    GamePos = 4,
                    Duration = 900,
                    Result = won ? PlayerResult.Los : PlayerResult.Win
                });

            Context.ReplayRatings.Add(new ReplayRating
            {
                ReplayRatingId = replayRatingId,
                RatingType = ratingType,
                LeaverType = leaverType,
                ExpectedWinProbability = 0.5,
                AvgRating = 1800 + replayId,
                ReplayId = replayId
            });

            Context.ReplayPlayerRatings.AddRange(
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 10000 + 1,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = ratingDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = selectedReplayPlayerId,
                    PlayerId = selectedPersistentPlayerId
                },
                new ReplayPlayerRating
                {
                    ReplayPlayerRatingId = replayId * 10000 + 4,
                    RatingType = ratingType,
                    RatingBefore = 1800,
                    RatingDelta = -ratingDelta,
                    ExpectedDelta = 0,
                    Games = 10,
                    ReplayRatingId = replayRatingId,
                    ReplayPlayerId = opponentReplayPlayerId,
                    PlayerId = opponentPersistentPlayerId
                });

            Context.ReplayBuildDetails.Add(new ReplayBuildDetail
            {
                ReplayBuildDetailId = detailId,
                ReplayId = replayId,
                DetectionVersion = 1,
                Status = ReplayBuildDetailStatus.Detected,
                CreatedAt = gametime,
                UpdatedAt = gametime,
                PlayerBuilds =
                [
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 1,
                        TeamId = 1,
                        Commander = Commander.Protoss,
                        Build = (int)selectedBuild,
                        GasFirst = selectedGasFirst,
                        Lane = 1,
                        OppGamePos = 4,
                        OppCommander = Commander.Terran,
                        OppBuild = (int)opponentBuild,
                        OppGasFirst = opponentGasFirst,
                        Won = won,
                        ReplayPlayerId = selectedReplayPlayerId,
                        OppReplayPlayerId = opponentReplayPlayerId
                    },
                    new ReplayPlayerBuildDetail
                    {
                        GamePos = 4,
                        TeamId = 2,
                        Commander = Commander.Terran,
                        Build = (int)opponentBuild,
                        GasFirst = opponentGasFirst,
                        Lane = 1,
                        OppGamePos = 1,
                        OppCommander = Commander.Protoss,
                        OppBuild = (int)selectedBuild,
                        OppGasFirst = selectedGasFirst,
                        Won = !won,
                        ReplayPlayerId = opponentReplayPlayerId,
                        OppReplayPlayerId = selectedReplayPlayerId
                    }
                ]
            });

            await Context.SaveChangesAsync();
        }

        private async Task AddPlayerIfMissing(int playerId, string name)
        {
            if (Context.Players.Local.Any(x => x.PlayerId == playerId)
                || await Context.Players.AnyAsync(x => x.PlayerId == playerId))
            {
                return;
            }

            Context.Players.Add(new Player
            {
                PlayerId = playerId,
                Name = name,
                ToonId = new ToonId { Region = 1, Realm = 1, Id = playerId }
            });
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
            (MemoryCache as MemoryCache)?.Dispose();
        }
    }
}
