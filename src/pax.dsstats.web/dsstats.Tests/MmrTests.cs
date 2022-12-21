using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using pax.dsstats.dbng.Services;
using AutoMapper;
using Microsoft.Extensions.Logging;

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
        builder.Services.AddScoped<MmrProduceService>();
        builder.Services.AddTransient<IReplayRepository, ReplayRepository>();

        app = builder.Build();

        Data.MysqlConnectionString = importConnectionString;
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
        var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MmrProduceService>>();
        var mmrProduceService = new MmrProduceService(serviceProvider, mapper, logger);

        // execute
        mmrProduceService.ProduceRatings(new(reCalc: true)).GetAwaiter().GetResult();

        // assert
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.ReplayPlayerRatings.Any());
    }

    [Fact]
    public void A3ContinuecalculateTest()
    {
        var testDir = "/data/temp";

        // prepare services
        var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MmrProduceService>>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var serviceProvider = scope.ServiceProvider;
        var mmrProduceService = new MmrProduceService(serviceProvider, mapper, logger);
        var importLogger = scope.ServiceProvider.GetRequiredService<ILogger<ImportService>>();
        var importService = new ImportService(serviceProvider, mapper, importLogger, testDir);


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
        
        var replayPlayerRatingsCountBefore = context.ReplayPlayerRatings.Count();
        
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

        var replayPlayerRatings = context.ReplayPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .ToList();

        context.Replays.RemoveRange(replays);
        context.ReplayPlayerRatings.RemoveRange(replayPlayerRatings);
        context.SaveChanges();

        mmrProduceService.ProduceRatings(new(reCalc: true)).GetAwaiter().GetResult();

        var replayCountBefore = context.Replays.Count();
        
        
        // execute
        var result = importService.ImportReplayBlobs().GetAwaiter().GetResult();
        mmrProduceService.ProduceRatings(new(reCalc: false), result.LatestReplay, result.ContinueReplays).GetAwaiter().GetResult();

        // assert

        var replayCountAfter = context.Replays.Count();
        var replayPlayerRatingsCountAfter = context.ReplayPlayerRatings.Count();
        Assert.True(replayCountAfter > replayCountBefore);
        Assert.Equal(replayPlayerRatingsCountBefore, replayPlayerRatingsCountAfter);
        // Assert.Equal(mmrBefore, context.PlayerRatings.Sum(s => s.Rating));
        Assert.Equal(Math.Round(mmrBefore, 4), Math.Round(context.PlayerRatings.Sum(s => s.Rating), 4));

        // cleanup
        Directory.Delete(testPath, true);
    }
}
