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
using pax.dsstats.web.Server.Services.Import;
using System.Collections.Generic;
using System.Text.Json;
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

        builder.Services.AddSingleton<ImportService>();
        builder.Services.AddSingleton<RatingsService>();

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
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        // execute

        ImportRequest importRequest = new()
        {
            Replayblobs = new() { Path.Combine(testPath, testFileName) }
        };

        var are = new AutoResetEvent(false);
        importService.OnBlobsHandled += (s, e) => { are.Set(); };
        importService.ImportTask(importRequest).GetAwaiter().GetResult();

        var importFinished = are.WaitOne(TimeSpan.FromSeconds(60));

        // assert
        Assert.True(importFinished);
        Assert.True(context.Replays.Any());

        // cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public void A2RecalculateTest()
    {
        // prepare services
        using var scope = app.Services.CreateScope();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

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
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

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

        ImportRequest importRequest = new()
        {
            Replayblobs = new() { Path.Combine(testPath, testFileName) }
        };

        var are = new AutoResetEvent(false);
        importService.OnBlobsHandled += (s, e) => { are.Set(); };
        importService.ImportTask(importRequest).GetAwaiter().GetResult();

        var importFinished = are.WaitOne(TimeSpan.FromSeconds(60));

        // assert
        Assert.True(importFinished);
        Assert.True(context.Replays.Any());

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
        // prepare services
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // prepare data
        var testFile = Startup.GetTestFilePath("replayDto3.json");
        var replayDto = System.Text.Json.JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(testFile));

        Assert.NotNull(replayDto);

        if (replayDto == null)
        {
            return;
        }

        replayDto = replayDto with { GameTime = DateTime.UtcNow };

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

        var units = context.Units.ToList().ToHashSet();
        var upgrades = context.Upgrades.ToList().ToHashSet();
        (_, _, var dbReplay) = replayRepository.SaveReplay(replayDto, units, upgrades, null).GetAwaiter().GetResult();

        Assert.NotNull(dbReplay);

        ratingsService.ProduceRatings(true).Wait();

        dbReplay = context.Replays
            .Include(i => i.Uploaders)
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

        dbReplay.Uploaders.Add(uploader);
        context.SaveChanges();

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

    [Fact]
    public void A6PreRatingsTest()
    {
        // prepare data
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        var testDir = "/data/temp";
        var testFile = Startup.GetTestFilePath("uploadtest4.base64");
        Guid testGuid = Guid.NewGuid();

        var memStream = ImportService.UnzipAsync(File.ReadAllText(testFile)).GetAwaiter().GetResult();

        var testReplays = JsonSerializer.Deserialize<List<ReplayDto>>(memStream);

        Assert.NotNull(testReplays);
        Assert.NotEmpty(testReplays);

        if (testReplays == null || testReplays.Count == 0)
        {
            return;
        }

        var replays = testReplays.Select(s => s with { GameTime = DateTime.UtcNow }).ToList();

        var testPath = Path.Combine(testDir, testGuid.ToString());
        var testFileName = $"{DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss")}.base64";
        var testFilePath = Path.Combine(testPath, testFileName);

        if (!Directory.Exists(testPath))
        {
            Directory.CreateDirectory(testPath);
        }
        var json = JsonSerializer.Serialize(replays);
        File.WriteAllText(testFilePath, ImportService.Zip(json));

        // execute

        ImportRequest importRequest = new()
        {
            Replayblobs = new() { Path.Combine(testPath, testFileName) }
        };

        var are = new AutoResetEvent(false);
        importService.OnBlobsHandled += (s, e) => { are.Set(); };
        importService.ImportTask(importRequest).GetAwaiter().GetResult();

        var importFinished = are.WaitOne(TimeSpan.FromSeconds(60));

        // assert
        Assert.True(importFinished);
        Assert.True(context.ReplayRatings.Where(x => x.IsPreRating).Any());

        // cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public void A7RecalculateWithPreRatingsTest()
    {
        // prepare services
        using var scope = app.Services.CreateScope();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        // execute
        ratingsService.ProduceRatings(true).Wait();

        // assert
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());

        Assert.False(context.ReplayRatings.Where(x => x.IsPreRating).Any());
    }

    [Fact]
    public void A8PreRatingsWhileCalculatingTest()
    {
        // prepare data
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

       

        var testDir = "/data/temp";
        var testFile = Startup.GetTestFilePath("uploadtest5.base64");
        Guid testGuid = Guid.NewGuid();

        var memStream = ImportService.UnzipAsync(File.ReadAllText(testFile)).GetAwaiter().GetResult();

        var testReplays = JsonSerializer.Deserialize<List<ReplayDto>>(memStream);

        Assert.NotNull(testReplays);
        Assert.NotEmpty(testReplays);

        if (testReplays == null || testReplays.Count == 0)
        {
            return;
        }

        var replays = testReplays.Select(s => s with { GameTime = DateTime.UtcNow }).ToList();

        var testPath = Path.Combine(testDir, testGuid.ToString());
        var testFileName = $"{DateTime.UtcNow.ToString(@"yyyyMMdd-HHmmss")}.base64";
        var testFilePath = Path.Combine(testPath, testFileName);

        if (!Directory.Exists(testPath))
        {
            Directory.CreateDirectory(testPath);
        }
        var json = JsonSerializer.Serialize(replays);
        File.WriteAllText(testFilePath, ImportService.Zip(json));

        // cleanup
        var replay = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .Include(i => i.ReplayRatingInfo!)
                .ThenInclude(i => i.RepPlayerRatings!)
            .FirstOrDefault(f => f.ReplayHash == "1271fcd4a8a5b0156f4e255ea80132c3");

        if (replay != null)
        {
            context.Replays.Remove(replay);
            context.SaveChanges();
        }

        // execute

        ImportRequest importRequest = new()
        {
            Replayblobs = new() { Path.Combine(testPath, testFileName) }
        };

        var are = new AutoResetEvent(false);
        importService.OnBlobsHandled += (s, e) => { are.Set(); };

        var importTask = importService.ImportTask(importRequest);
        var ratingsTask = ratingsService.ProduceRatings(true);
        Task.WaitAll(new Task[2] { ratingsTask, importTask });

        var importFinished = are.WaitOne(TimeSpan.FromSeconds(60));

        // assert
        Assert.True(importFinished);
        Assert.True(context.ReplayRatings.Where(x => x.IsPreRating).Any());

        // cleanup
        Directory.Delete(testPath, true);
    }
}
