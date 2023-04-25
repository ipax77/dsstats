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

    //[Fact]
    //public void ArcadeA4PlayerRatingPosTest()
    //{
    //    // prepare services
    //    var scope = app.Services.CreateScope();
    //    var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

    //    foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
    //    {
    //        if (ratingType == RatingType.None)
    //        {
    //            continue;
    //        }

    //        var ratings = context.PlayerRatings
    //        .Where(x => x.RatingType == ratingType)
    //        .OrderByDescending(o => o.Rating).ThenBy(o => o.PlayerId);

    //        if (ratings.Count() < 2)
    //        {
    //            continue;
    //        }

    //        var firstRating = ratings.FirstOrDefault();
    //        var secondRating = ratings.Skip(1).FirstOrDefault();

    //        Assert.Equal(1, firstRating?.Pos);
    //        Assert.Equal(2, secondRating?.Pos);
    //    }

    //    // PlayerRating.Pos is created with the database procedure SetPlayerRatingPos
    //    // introduced in migration 20230105132613_PlayerRatingsRowNumber.cs
    //}

    //[Fact]
    //public void ArcadeA5PlayerRatingChangesTest()
    //{
    //    var testDir = "/data/temp";

    //    // prepare services
    //    using var scope = app.Services.CreateScope();
    //    var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
    //    var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    //    var logger = scope.ServiceProvider.GetRequiredService<ILogger<RatingsService>>();
    //    var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
    //    var ratingsService = new RatingsService(scope.ServiceProvider, mapper, dbImportOptions, logger);
    //    var importLogger = scope.ServiceProvider.GetRequiredService<ILogger<ImportService>>();
    //    var importService = new ImportService(scope.ServiceProvider, mapper, importLogger, testDir);

    //    // prepare data
    //    var testFile = Startup.GetTestFilePath("replayDto3.json");
    //    var replayDto = System.Text.Json.JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(testFile));

    //    Assert.NotNull(replayDto);

    //    if (replayDto == null)
    //    {
    //        return;
    //    }

    //    replayDto = replayDto with { GameTime = DateTime.UtcNow };
    //    var replay = mapper.Map<Replay>(replayDto);

    //    Assert.NotNull(replay);

    //    if (replay == null)
    //    {
    //        return;
    //    }

    //    var toonId = replayDto.ReplayPlayers.FirstOrDefault(f => f.Name == "PAX")?.Player.ToonId;
    //    var player = context.Players.FirstOrDefault(f => f.ToonId == toonId);

    //    Assert.NotNull(player);

    //    if (player == null)
    //    {
    //        return;
    //    }

    //    var uploader = new Uploader() { AppGuid = Guid.NewGuid() };
    //    uploader.Players.Add(player);

    //    context.Uploaders.Add(uploader);
    //    context.SaveChanges();
    //    replay.UploaderId = uploader.UploaderId;

    //    var result = importService.ImportReplays(new() { replay }, new()).GetAwaiter().GetResult();

    //    Assert.Equal(1, result.SavedReplays);

    //    ratingsService.ProduceRatings(true).Wait();

    //    var dbReplay = context.Replays
    //        .Include(i => i.ReplayPlayers)
    //            .ThenInclude(i => i.Player)
    //                .ThenInclude(i => i.PlayerRatings)
    //                    .ThenInclude(i => i.PlayerRatingChange)
    //        .Include(i => i.ReplayRatingInfo)
    //        .Where(x => x.ReplayHash == replayDto.ReplayHash)
    //        .FirstOrDefault();

    //    Assert.NotNull(dbReplay);

    //    if (dbReplay == null)
    //    {
    //        return;
    //    }

    //    // assert

    //    foreach (var replayPlayer in dbReplay.ReplayPlayers.Where(x => x.IsUploader))
    //    {
    //        var rating = replayPlayer.Player.PlayerRatings
    //            .FirstOrDefault(f => f.RatingType == dbReplay.ReplayRatingInfo?.RatingType);

    //        Assert.NotNull(rating);
    //        Assert.NotNull(rating.PlayerRatingChange);

    //        Assert.True(rating?.PlayerRatingChange?.Change24h != 0);
    //        // Assert.True(rating?.PlayerRatingChange?.Change10d != 0);
    //        // Assert.True(rating?.PlayerRatingChange?.Change30d != 0);
    //    }

    //    // PlayerRatingChanges is created with the database procedure SetRatingChange
    //    // introduced in migration 20230114191807_PlayerRatingChanges and 20230116063107_FixSetRatingChangeProcedure
    //}
}
