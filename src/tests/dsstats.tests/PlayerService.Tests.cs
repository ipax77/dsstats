using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace dsstats.tests;

[TestClass]
public class PlayerServiceTests
{
    [TestMethod]
    public async Task Profile_ReusesContextAndMapsRatings()
    {
        using var fixture = new Fixture();
        var response = await fixture.Service.GetPlayerStats(new()
        {
            ToonId = new() { Id = 1, Region = 1, Realm = 1 }, RatingType = RatingType.Commanders
        });
        Assert.AreEqual("Alpha", response.Name);
        Assert.AreEqual(1, response.RegionId);
        Assert.AreEqual(1, response.ToonId.Id);
        var rating = response.Ratings.Single();
        Assert.AreEqual(RatingType.Commanders, rating.RatingType);
        Assert.AreEqual(0.7, rating.Cons);
        Assert.AreEqual(0.8, rating.Conf);
        Assert.AreEqual(1500d, rating.Rating);
        Assert.AreEqual(RatingType.Commanders, response.RatingDetails.Single().RatingType);
        fixture.Factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
        fixture.Import.Verify(i => i.GetPlayerId(It.IsAny<ToonIdDto>()), Times.Once);
    }

    [TestMethod]
    public async Task MissingPlayer_ReturnsRequestedIdentityWithoutDetails()
    {
        using var fixture = new Fixture();
        var response = await fixture.Service.GetPlayerStats(new() { ToonId = new() { Id = 99 } });
        Assert.AreEqual(99, response.ToonId.Id);
        Assert.IsEmpty(response.Ratings);
        Assert.IsEmpty(response.RatingDetails);
    }

    [TestMethod]
    public async Task Details_FilterRatingTypeAndExcludeUnrelatedReplays()
    {
        using var fixture = new Fixture();
        using (var context = fixture.CreateContext())
        {
            AddReplay(context, 1, 1, RatingType.Commanders);
            AddReplay(context, 2, 1, RatingType.Standard);
            AddReplay(context, 3, 2, RatingType.Commanders);
            // Equal timestamps exercise the replay-ID tie breaker.
            AddReplay(context, 4, 1, RatingType.Commanders);
            context.SaveChanges();
        }
        var result = await fixture.Service.GetRatingDetails(new()
        {
            ToonId = new() { Id = 1 }, RatingType = RatingType.Commanders
        });
        CollectionAssert.AreEqual(new[] { "4", "1" }, result.Replays.Select(r => r.ReplayHash).ToArray());
        Assert.AreEqual(2, result.Commanders.Single().Count);
        Assert.AreEqual(1004f, result.Ratings.Single().Rating);
        Assert.AreEqual(2, result.PercentileMaxRank);
        var standard = await fixture.Service.GetRatingDetails(new()
        {
            ToonId = new() { Id = 1 }, RatingType = RatingType.Standard
        });
        Assert.AreEqual("2", standard.Replays.Single().ReplayHash);
    }

    [TestMethod]
    public async Task Leaderboard_MapsFieldsAndFiltersCount()
    {
        using var fixture = new Fixture();
        var items = await fixture.Service.GetRatings(Request());
        Assert.HasCount(3, items);
        Assert.AreEqual(RatingType.Commanders, items[0].RatingType);
        Assert.AreEqual(0.7, items[0].Cons);
        Assert.AreEqual(0.8, items[0].Conf);
        Assert.AreEqual(3, items[0].DsstatsGames);
        Assert.AreEqual(1, await fixture.Service.GetRatingsCount(new()
        {
            RatingType = RatingType.Commanders, RegionId = 2, IsActive = true
        }));
    }

    [TestMethod]
    [DataRow(nameof(PlayerRatingListItem.Wins))]
    [DataRow(nameof(PlayerRatingListItem.MainCount))]
    [DataRow(nameof(PlayerRatingListItem.Mvps))]
    public async Task Leaderboard_SortsFractionalPercentagesAndZeroGames(string column)
    {
        using var fixture = new Fixture();
        var request = Request();
        request.Orders = [new() { Column = column, Ascending = false }];
        var result = await fixture.Service.GetRatings(request);
        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, result.Select(r => r.PlayerId).ToArray());
        request.Orders[0].Ascending = true;
        result = await fixture.Service.GetRatings(request);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, result.Select(r => r.PlayerId).ToArray());
    }

    [TestMethod]
    public async Task Leaderboard_CacheSeparatesCountsTypesFiltersAndOrdering()
    {
        using var fixture = new Fixture();
        var request = Request(1);
        Assert.HasCount(1, await fixture.Service.GetRatings(request));
        request.Take = 2;
        Assert.HasCount(2, await fixture.Service.GetRatings(request));
        request.Take = 1;
        request.Name = "Beta";
        Assert.AreEqual(2, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.Name = "";
        request.RegionId = 2;
        Assert.AreEqual(2, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.RegionId = 0;
        request.IsActive = true;
        Assert.AreEqual(2, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.IsActive = false;
        request.Orders = [new() { Column = nameof(PlayerRatingListItem.Rating), Ascending = true }];
        Assert.AreEqual(3, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.Orders = [];
        request.RatingType = RatingType.Standard;
        Assert.IsEmpty(await fixture.Service.GetRatings(request));
        request.RatingType = RatingType.Commanders;
        Assert.AreEqual(1, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        fixture.Factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Exactly(7));
    }

    [TestMethod]
    public async Task Leaderboard_SmallPagesRespectPageSkipAndInvalidRanges()
    {
        using var fixture = new Fixture();
        var request = Request(1);
        request.PageSize = 1;
        Assert.AreEqual(1, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.Page = 2;
        Assert.AreEqual(2, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.Skip = 1;
        Assert.AreEqual(3, (await fixture.Service.GetRatings(request)).Single().PlayerId);
        request.Skip = -2;
        Assert.IsEmpty(await fixture.Service.GetRatings(request));
        request.Page = int.MaxValue;
        request.PageSize = int.MaxValue;
        Assert.IsEmpty(await fixture.Service.GetRatings(request));
        request = Request(0);
        Assert.IsEmpty(await fixture.Service.GetRatings(request));
    }

    [TestMethod]
    public async Task Details_PropagatesCancellation()
    {
        using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.Service.GetRatingDetails(new() { ToonId = new() { Id = 1 } }, cancellation.Token));
    }

    private static PlayerRatingsRequest Request(int take = 3) => new()
    {
        RatingType = RatingType.Commanders, PageSize = 3, Take = take
    };

    internal static void AddReplay(DsstatsContext context, int id, int playerId, RatingType type)
    {
        var replay = new Replay
        {
            ReplayId = id, ReplayHash = id.ToString(), Gametime = new(2026, 9, 17), WinnerTeam = 1
        };
        var player = new ReplayPlayer
        {
            PlayerId = playerId, Replay = replay, Race = Commander.Abathur, TeamId = 1, GamePos = 1
        };
        var rating = new ReplayRating { Replay = replay, RatingType = type };
        context.ReplayPlayerRatings.Add(new()
        {
            PlayerId = playerId, ReplayPlayer = player, ReplayRating = rating, RatingType = type,
            RatingBefore = 1000, RatingDelta = id, Games = id
        });
    }

    internal sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Filename=:memory:");
        private readonly MemoryCache cache = new(new MemoryCacheOptions());
        private readonly DbContextOptions<DsstatsContext> options;
        public Mock<IDbContextFactory<DsstatsContext>> Factory { get; } = new();
        public Mock<IImportService> Import { get; } = new();
        public PlayerService Service { get; }

        public Fixture(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor? interceptor = null)
        {
            connection.Open();
            var builder = new DbContextOptionsBuilder<DsstatsContext>().UseSqlite(connection);
            if (interceptor is not null) builder.AddInterceptors(interceptor);
            options = builder.Options;
            Factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken _) => Task.FromResult(CreateContext()));
            Import.Setup(i => i.GetPlayerId(It.IsAny<ToonIdDto>())).Returns((ToonIdDto id) => id.Id);
            Import.Setup(i => i.GetPlayerName(It.IsAny<ToonIdDto>())).Returns((ToonIdDto id) => $"Player{id.Id}");
            Service = new(Factory.Object, Import.Object, cache, NullLogger<PlayerService>.Instance);
            using var context = CreateContext();
            context.Database.EnsureCreated();
            for (int id = 1; id <= 3; id++)
            {
                context.PlayerRatings.Add(new()
                {
                    Player = new()
                    {
                        PlayerId = id, Name = id == 1 ? "Alpha" : id == 2 ? "Beta" : "Zero",
                        ToonId = new() { Id = id, Region = id == 2 ? 2 : 1, Realm = 1 }
                    },
                    RatingType = RatingType.Commanders, Rating = 1600 - id * 100,
                    Games = id == 3 ? 0 : 10, Wins = id * 2, MainCount = id * 2, Mvps = id * 2,
                    DsstatsGames = 3, Consistency = 0.7, Confidence = 0.8,
                    Position = id == 3 ? 0 : id, Change = id == 2 ? 10 : 0
                });
            }
            context.SaveChanges();
        }

        public DsstatsContext CreateContext() => new(options);

        public void Dispose()
        {
            cache.Dispose();
            connection.Dispose();
        }
    }
}
