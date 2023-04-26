using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;
using System.Reflection;
using System.Text.Json;

namespace dsstats.maui.tests;


public class RatingsTest
{
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public RatingsTest()
    {
        var serviceCollection = new ServiceCollection();

        var sqliteConnectionString = $"DataSource=/data/ds/testdata/rtest.db";

        serviceCollection.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = sqliteConnectionString);

        serviceCollection.AddDbContext<ReplayContext>(options => options
            .UseSqlite(sqliteConnectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("SqliteMigrations");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
        );

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();

        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
        serviceCollection.AddSingleton<RatingsService>();

        ServiceProvider = serviceCollection.BuildServiceProvider();

        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        Data.IsMaui = true;
    }

    public ServiceProvider ServiceProvider { get; private set; }

    [Fact]
    public void RecalulateTest()
    {
        using var scope = ServiceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // prepare data
        var testData = Path.Combine(assemblyPath, "testdata/testreplays1.json");
        var replays = JsonSerializer.Deserialize<List<Replay>>(File.ReadAllText(testData)) ?? new();
        var replayDtos = replays.Select(s => mapper.Map<ReplayDto>(s)).ToList();

        HashSet<Unit> units = new();
        HashSet<Upgrade> upgrades = new();
        foreach (var replay in replayDtos)
        {
            (units, upgrades, var dbReplay) = replayRepository
                .SaveReplay(replay, units, upgrades, null).GetAwaiter().GetResult();
        }

        Assert.True(context.Replays.Any());

        // execute
        ratingsService.ProduceRatings(recalc: true).Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());
    }

    [Fact]
    public void ReRecalulateTest()
    {
        using var scope = ServiceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // prepare data
        var testData = Path.Combine(assemblyPath, "testdata/testreplays1.json");
        var replays = JsonSerializer.Deserialize<List<Replay>>(File.ReadAllText(testData)) ?? new();
        var replayDtos = replays.Select(s => mapper.Map<ReplayDto>(s)).ToList();

        HashSet<Unit> units = new();
        HashSet<Upgrade> upgrades = new();
        foreach (var replay in replayDtos)
        {
            (units, upgrades, var dbReplay) = replayRepository
                .SaveReplay(replay, units, upgrades, null).GetAwaiter().GetResult();
        }

        Assert.True(context.Replays.Any());

        // execute
        ratingsService.ProduceRatings(recalc: true).Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());

        int playerRatingsBefore = context.PlayerRatings.Count();

        ratingsService.ProduceRatings(recalc: true).Wait();

        int playerRatingsAfter = context.PlayerRatings.Count();

        Assert.Equal(playerRatingsBefore, playerRatingsAfter);
    }

    [Fact]
    public void ContinueCalulateTest()
    {
        using var scope = ServiceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // prepare data
        var testData = Path.Combine(assemblyPath, "testdata/testreplays1.json");
        var replays = JsonSerializer.Deserialize<List<Replay>>(File.ReadAllText(testData)) ?? new();
        var replayDtos = replays.Select(s => mapper.Map<ReplayDto>(s)).ToList();

        HashSet<Unit> units = new();
        HashSet<Upgrade> upgrades = new();
        foreach (var replay in replayDtos)
        {
            (units, upgrades, var dbReplay) = replayRepository
                .SaveReplay(replay, units, upgrades, null).GetAwaiter().GetResult();
        }

        Assert.True(context.Replays.Any());

        // execute
        ratingsService.ProduceRatings(recalc: true).Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());

        var testreplays = context.Replays
            .Include(i => i.ReplayPlayers)
            .OrderByDescending(o => o.GameTime)
            .Take(20)
            .ToList();
        context.Replays.RemoveRange(testreplays);
        context.SaveChanges();

        var playerIds = replays
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.PlayerId)
            .Distinct()
            .OrderBy(o => o)
            .ToList();

        var ratingsBefore = (
                                from p in context.Players
                                from r in p.PlayerRatings
                                orderby p.PlayerId, r.PlayerRatingId
                                where playerIds.Contains(p.PlayerId)
                                select r.Rating
                            )
                            .ToList();

        ratingsService.ProduceRatings(true).Wait();

        context.Replays.AddRange(testreplays);
        context.SaveChanges();

        ratingsService.ProduceRatings(false).Wait();

        var ratingsAfter = (
                                from p in context.Players
                                from r in p.PlayerRatings
                                orderby p.PlayerId, r.PlayerRatingId
                                where playerIds.Contains(p.PlayerId)
                                select r.Rating
                            )
                            .ToList();

        Assert.True(ratingsBefore.SequenceEqual(ratingsAfter));
    }
}
