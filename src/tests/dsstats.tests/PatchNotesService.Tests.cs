using dsstats.db;
using dsstats.db.UnitModels;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.PatchNotes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class PatchNotesServiceTests
{
    [TestMethod]
    public async Task GetPatchNotes_FiltersSortsAndPages()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var firstPage = await fixture.Service.GetPatchNotes(new PatchNotesRequest
        {
            Commander = Commander.Abathur,
            Unit = "Roach",
            Sort = PatchNotesSort.OldestFirst,
            PageSize = 1
        });

        Assert.HasCount(1, firstPage.Items);
        Assert.AreEqual("Roach life increased.", firstPage.Items[0].Content);
        Assert.IsTrue(firstPage.HasMore);

        var secondPage = await fixture.Service.GetPatchNotes(new PatchNotesRequest
        {
            Commander = Commander.Abathur,
            Unit = "Roach",
            Sort = PatchNotesSort.OldestFirst,
            Page = 2,
            PageSize = 1
        });

        Assert.HasCount(1, secondPage.Items);
        Assert.AreEqual("Roach cost reduced.", secondPage.Items[0].Content);
        Assert.IsFalse(secondPage.HasMore);
    }

    [TestMethod]
    public async Task GetPatchNotes_FiltersByInclusiveDateRange()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var page = await fixture.Service.GetPatchNotes(new PatchNotesRequest
        {
            PatchDate = new DateTime(2026, 1, 2),
            ToDate = new DateTime(2026, 1, 3)
        });

        Assert.HasCount(2, page.Items);
        CollectionAssert.AreEqual(
            new[] { "Brutalisk armor increased.", "Roach cost reduced." },
            page.Items.Select(item => item.Content).ToArray());
        Assert.IsFalse(page.HasMore);
    }

    [TestMethod]
    public async Task GetPatchNotes_PatchDateWithoutToDateFiltersOneDay()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var page = await fixture.Service.GetPatchNotes(new PatchNotesRequest
        {
            PatchDate = new DateTime(2026, 1, 2)
        });

        Assert.HasCount(1, page.Items);
        Assert.AreEqual("Roach cost reduced.", page.Items[0].Content);
        Assert.IsFalse(page.HasMore);
    }

    [TestMethod]
    public async Task GetUnitNames_FiltersCommanderAndRemovesDuplicates()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var abathur = await fixture.Service.GetUnitNames(Commander.Abathur);
        CollectionAssert.AreEqual(new[] { "Brutalisk", "Roach" }, abathur.ToArray());

        var all = await fixture.Service.GetUnitNames(Commander.None);
        CollectionAssert.AreEqual(new[] { "Brutalisk", "Marine", "Roach" }, all.ToArray());
    }

    [TestMethod]
    public async Task GetPatchDates_ReturnsDistinctNewestDatesAndCachesResults()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var dates = await fixture.Service.GetPatchDates(3);
        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 1, 4), new DateTime(2026, 1, 3), new DateTime(2026, 1, 2) },
            dates.ToArray());

        fixture.Context.PatchNotes.Add(TestFixture.CreateNote("new", Commander.Abathur, new DateTime(2026, 2, 1), "New patch."));
        await fixture.Context.SaveChangesAsync();
        var cached = await fixture.Service.GetPatchDates(1);
        Assert.AreEqual(new DateTime(2026, 1, 4), cached[0]);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DsstatsContext context;
        private readonly IMemoryCache cache;

        private TestFixture(
            SqliteConnection connection,
            DsstatsContext context,
            IMemoryCache cache,
            PatchNotesService service)
        {
            this.connection = connection;
            this.context = context;
            this.cache = cache;
            Service = service;
        }

        public PatchNotesService Service { get; }
        public DsstatsContext Context => context;

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<DsstatsContext>().UseSqlite(connection).Options;
            var context = new DsstatsContext(options);
            await context.Database.EnsureCreatedAsync();

            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            context.PatchNotes.AddRange(
                CreateNote("1", Commander.Abathur, start, "Roach life increased."),
                CreateNote("2", Commander.Abathur, start.AddDays(1), "Roach cost reduced."),
                CreateNote("3", Commander.Abathur, start.AddDays(2), "Brutalisk armor increased."),
                CreateNote("4", Commander.Raynor, start.AddDays(3), "Roach comparison removed."));
            context.DsUnits.AddRange(
                new DsUnit { Name = "Roach", Commander = Commander.Abathur },
                new DsUnit { Name = "Brutalisk", Commander = Commander.Abathur },
                new DsUnit { Name = "roach", Commander = Commander.Abathur },
                new DsUnit { Name = "Marine", Commander = Commander.Raynor });
            await context.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new PatchNotesService(new TestDbContextFactory<DsstatsContext>(options), cache);
            return new TestFixture(connection, context, cache, service);
        }

        public static PatchNote CreateNote(string key, Commander commander, DateTime date, string content) => new()
        {
            SourceKey = key,
            Source = PatchNoteSource.Manual,
            PublishedAtUtc = date,
            Commander = commander,
            Content = content
        };

        public async ValueTask DisposeAsync()
        {
            cache.Dispose();
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
