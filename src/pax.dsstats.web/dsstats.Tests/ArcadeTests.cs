using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services.Arcade;
using System.Net.Mime;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace dsstats.Tests;

[TestCaseOrderer("dsstats.Tests.AlphabeticalOrderer", "dsstats.Tests")]
public class ArcadeTests
{
    private readonly WebApplication app;

    public ArcadeTests()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
        });

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 42));
        var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
        var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"];

        builder.Services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

        builder.Services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(120);
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            ;
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        builder.Services.AddLogging();

        builder.Services.AddHttpClient("sc2arcardeClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://api.sc2arcade.com");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
            });

        builder.Services.AddScoped<CrawlerService>();

        app = builder.Build();
    }

    [Fact]
    public void ArcadeA1ImportTest()
    {
        // prepare data
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        string testDataFile = Startup.GetTestFilePath("arcadetestdata.json");
        CrawlInfo crawlInfo = JsonSerializer.Deserialize<CrawlInfo>(File.ReadAllText(testDataFile)) ?? new();

        Assert.True(crawlInfo.Results.Any());

        var crawlService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
        crawlService.TestImportArcadeReplays(crawlInfo).Wait();

        // assert
        Assert.True(context.ArcadeReplays.Any());
    }

    [Fact]
    public void ArcadeA2RecalculateTest()
    {
        // prepare services
        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ArcadeRatingsService>>();
        var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var ratingsService = new ArcadeRatingsService(serviceProvider, mapper, dbImportOptions, logger);

        // execute
        ratingsService.ProduceRatings(true).Wait();

        // assert
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        Assert.True(context.ArcadePlayerRatings.Any());
        Assert.True(context.ArcadeReplayPlayerRatings.Any());
    }

    [Fact]
    public void ArcadeA3ContinuecalculateTest()
    {
        // prepare services
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ArcadeRatingsService>>();
        var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var ratingsService = new ArcadeRatingsService(scope.ServiceProvider, mapper, dbImportOptions, logger);


        // prepare data
        var replays = context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .ToList();
        context.ArcadeReplays.RemoveRange(replays);
        context.SaveChanges();

        var playerIds = replays
            .SelectMany(s => s.ArcadeReplayPlayers)
            .Select(s => s.ArcadePlayerId)
            .Distinct()
            .OrderBy(o => o)
            .ToList();

        var ratingsBefore = (
                                from p in context.ArcadePlayers
                                from r in p.ArcadePlayerRatings
                                orderby p.ArcadePlayerId, r.ArcadePlayerRatingId
                                where playerIds.Contains(p.ArcadePlayerId)
                                select r.Rating
                            )
                            .ToList();


        ratingsService.ProduceRatings(true).Wait();

        context.ArcadeReplays.AddRange(replays);
        context.SaveChanges();

        ratingsService.ProduceRatings(false).Wait();

        var ratingsAfter = (
                                from p in context.ArcadePlayers
                                from r in p.ArcadePlayerRatings
                                orderby p.ArcadePlayerId, r.ArcadePlayerRatingId
                                where playerIds.Contains(p.ArcadePlayerId)
                                select r.Rating
                            )
                            .ToList();

        Assert.True(ratingsBefore.SequenceEqual(ratingsAfter));
    }
}
