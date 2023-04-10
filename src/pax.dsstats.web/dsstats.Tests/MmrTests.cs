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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace dsstats.Tests;

public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
    {
        var result = testCases.ToList();
        result.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
        return result;
    }
}

[TestCaseOrderer("dsstats.Tests.AlphabeticalOrderer", "dsstats.Tests")]
public class MmrTests
{
    private readonly WebApplication app;

    public MmrTests()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Host.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
                });

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 40));
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

        builder.Services.AddScoped<IRatingRepository, RatingRepository>();
        builder.Services.AddScoped<RatingsService>();
        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();

        builder.Services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

        app = builder.Build();
    }

    [Fact]
    public void A1ImportTest()
    {
        // prepare data
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var testDir = "/data/temp";
        var testFile = Startup.GetTestFilePath("uploadtest.base64");
        Guid testGuid = Guid.NewGuid();

        var testPath = Path.Combine(testDir, testGuid.ToString());
        var testFileName = $"{DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss")}.base64";
        var testFilePath = Path.Combine(testPath, testFileName);

        if (!Directory.Exists(testPath))
        {
            Directory.CreateDirectory(testPath);
        }
        File.Copy(testFile, testFilePath);

        // prepare services
        var serviceProvider = scope.ServiceProvider;
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ImportService>>();
        var importService = new ImportService(serviceProvider, mapper, logger, testDir);

        // execute
        var result = importService.ImportReplayBlobs().GetAwaiter().GetResult();

        // assert
        Assert.True(result.ContinueReplays.Any());

        // cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public void A2RecalculateTest()
    {
        // prepare services
        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RatingsService>>();
        var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var ratingsService = new RatingsService(serviceProvider, mapper, dbImportOptions, logger);

        // execute
        ratingsService.ProduceRatings(true).Wait();

        // assert
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());
    }

    [Fact]
    public void A3ContinuecalculateTest()
    {
        var testDir = "/data/temp";

        // prepare services
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RatingsService>>();
        var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var ratingsService = new RatingsService(scope.ServiceProvider, mapper, dbImportOptions, logger);
        var importLogger = scope.ServiceProvider.GetRequiredService<ILogger<ImportService>>();
        var importService = new ImportService(scope.ServiceProvider, mapper, importLogger, testDir);


        // prepare data
        var testFile = Startup.GetTestFilePath("uploadtest.base64");
        Guid testGuid = Guid.NewGuid();

        var testPath = Path.Combine(testDir, testGuid.ToString());
        var testFileName = $"{DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss")}.base64";
        var testFilePath = Path.Combine(testPath, testFileName);

        if (!Directory.Exists(testPath))
        {
            Directory.CreateDirectory(testPath);
        }
        File.Copy(testFile, testFilePath);

        var replayPlayerRatingsCountBefore = context.RepPlayerRatings.Count();

        var mmrBefore = context.PlayerRatings.Sum(s => s.Rating);
        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .OrderByDescending(o => o.GameTime)
            .Take(5)
            .ToList();

        var replayPlayerIds = replays
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.ReplayPlayerId)
            .Distinct()
            .ToList();

        var replayPlayerRatings = context.RepPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .ToList();

        context.Replays.RemoveRange(replays);
        context.RepPlayerRatings.RemoveRange(replayPlayerRatings);
        context.SaveChanges();

        ratingsService.ProduceRatings(true).Wait();

        var replayCountBefore = context.Replays.Count();


        // execute
        var result = importService.ImportReplayBlobs().GetAwaiter().GetResult();
        ratingsService.ProduceRatings(true).Wait();

        // assert

        var replayCountAfter = context.Replays.Count();
        var replayPlayerRatingsCountAfter = context.RepPlayerRatings.Count();
        Assert.True(replayCountAfter > replayCountBefore);
        Assert.Equal(replayPlayerRatingsCountBefore, replayPlayerRatingsCountAfter);
        // Assert.Equal(mmrBefore, context.PlayerRatings.Sum(s => s.Rating));
        Assert.Equal(Math.Round(mmrBefore, 4), Math.Round(context.PlayerRatings.Sum(s => s.Rating), 4));

        // cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public void A4PlayerRatingPosTest()
    {
        // prepare services
        var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }

            var ratings = context.PlayerRatings
            .Where(x => x.RatingType == ratingType)
            .OrderByDescending(o => o.Rating).ThenBy(o => o.PlayerId);

            if (ratings.Count() < 2)
            {
                continue;
            }

            var firstRating = ratings.FirstOrDefault();
            var secondRating = ratings.Skip(1).FirstOrDefault();

            Assert.Equal(1, firstRating?.Pos);
            Assert.Equal(2, secondRating?.Pos);
        }

        // PlayerRating.Pos is created with the database procedure SetPlayerRatingPos
        // introduced in migration 20230105132613_PlayerRatingsRowNumber.cs
    }

    [Fact]
    public void A5PlayerRatingChangesTest()
    {
        var testDir = "/data/temp";

        // prepare services
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RatingsService>>();
        var dbImportOptions = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();
        var ratingsService = new RatingsService(scope.ServiceProvider, mapper, dbImportOptions, logger);
        var importLogger = scope.ServiceProvider.GetRequiredService<ILogger<ImportService>>();
        var importService = new ImportService(scope.ServiceProvider, mapper, importLogger, testDir);

        // prepare data
        var testFile = Startup.GetTestFilePath("replayDto3.json");
        var replayDto = System.Text.Json.JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(testFile));

        Assert.NotNull(replayDto);

        if (replayDto == null)
        {
            return;
        }

        replayDto = replayDto with { GameTime = DateTime.UtcNow };
        var replay = mapper.Map<Replay>(replayDto);

        Assert.NotNull(replay);

        if (replay == null)
        {
            return;
        }

        var toonId = replayDto.ReplayPlayers.FirstOrDefault(f => f.Name == "PAX")?.Player.ToonId;
        var player = context.Players.FirstOrDefault(f => f.ToonId == toonId);

        Assert.NotNull(player);

        if (player == null)
        {
            return;
        }

        var uploader = new Uploader() { AppGuid = Guid.NewGuid() };
        uploader.Players.Add(player);

        context.Uploaders.Add(uploader);
        context.SaveChanges();
        replay.UploaderId = uploader.UploaderId;

        var result = importService.ImportReplays(new() { replay }, new()).GetAwaiter().GetResult();

        Assert.Equal(1, result.SavedReplays);

        ratingsService.ProduceRatings(true).Wait();

        var dbReplay = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
                    .ThenInclude(i => i.PlayerRatings)
                        .ThenInclude(i => i.PlayerRatingChange)
            .Include(i => i.ReplayRatingInfo)
            .Where(x => x.ReplayHash == replayDto.ReplayHash)
            .FirstOrDefault();

        Assert.NotNull(dbReplay);

        if (dbReplay == null)
        {
            return;
        }

        // assert
        
        foreach (var replayPlayer in dbReplay.ReplayPlayers.Where(x => x.IsUploader))
        {
            var rating = replayPlayer.Player.PlayerRatings
                .FirstOrDefault(f => f.RatingType == dbReplay.ReplayRatingInfo?.RatingType);

            Assert.NotNull(rating);
            Assert.NotNull(rating.PlayerRatingChange);

            Assert.True(rating?.PlayerRatingChange?.Change24h != 0);
            // Assert.True(rating?.PlayerRatingChange?.Change10d != 0);
            // Assert.True(rating?.PlayerRatingChange?.Change30d != 0);
        }

        // PlayerRatingChanges is created with the database procedure SetRatingChange
        // introduced in migration 20230114191807_PlayerRatingChanges and 20230116063107_FixSetRatingChangeProcedure
    }
}
