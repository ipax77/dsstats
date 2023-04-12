using dsstats.import.api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace dsstats.import.api.tests;

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

[TestCaseOrderer("dsstats.import.api.tests.AlphabeticalOrderer", "dsstats.import.api.tests")]
public class ImportTests
{
    private ServiceProvider serviceProvider;

    public ImportTests()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("TestConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 41));

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddSingleton<ImportService>();

        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void A1BlobImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile1 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-182938.base64";
        string testFile2 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-185756.base64";

        Assert.True(File.Exists(testFile1));
        Assert.True(File.Exists(testFile2));

        ImportRequest request = new()
        {
            Replayblobs = new()
            {
                testFile1,
                testFile2
            }
        };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate(object? sender, EventArgs e) 
            { 
                jobDoneEvent.Set();
            };

        importService.Import(request).Wait();

        var waitResult = jobDoneEvent.WaitOne(30000);

        Assert.True(waitResult);

        Assert.True(context.Replays.Any());

        Assert.True(File.Exists(testFile1 + ".done"));
        Assert.True(File.Exists(testFile2 + ".done"));

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);
        File.Move(testFile2 + ".done", testFile2);
    }

    [Fact]
    public void A2DuplicateTest()
    {
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile1 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-182938.base64";
        string testFile2 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-185756.base64";

        Assert.True(File.Exists(testFile1));
        Assert.True(File.Exists(testFile2));

        ImportRequest request = new()
        {
            Replayblobs = new()
            {
                testFile1,
                testFile2
            }
        };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate (object? sender, EventArgs e)
        {
            jobDoneEvent.Set();
        };

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        int countBefore = context.Replays.Count();
        int repCountBefore = context.ReplayPlayers.Count();

        importService.Import(request).Wait();

        var waitResult = jobDoneEvent.WaitOne(30000);

        Assert.True(waitResult);


        Assert.True(context.Replays.Any());

        Assert.True(File.Exists(testFile1 + ".done"));
        Assert.True(File.Exists(testFile2 + ".done"));

        int countAfter = context.Replays.Count();
        int repCountAfter = context.ReplayPlayers.Count();

        Assert.Equal(countBefore, countAfter);
        Assert.Equal(repCountBefore, repCountAfter);

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);
        File.Move(testFile2 + ".done", testFile2);
    }

    [Fact]
    public void A3DuplicateTest2()
    {
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile1 = "/data/ds/replayblobs/b1e8116d-f6fb-4504-9c02-bff5a0157d48/20230324-233557.base64";
        string testFile2 = "/data/ds/replayblobs/f111abcc-bd49-44b1-9237-bd516e04c467/20230324-233610.base64";
        string replayHash = "7ba74eed813f8d5dc4a4ca3b9290a175";

        Assert.True(File.Exists(testFile1));
        Assert.True(File.Exists(testFile2));

        ImportRequest request = new()
        {
            Replayblobs = new()
            {
                testFile1,
                testFile2
            }
        };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate (object? sender, EventArgs e)
        {
            jobDoneEvent.Set();
        };

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        importService.Import(request).Wait();

        var waitResult = jobDoneEvent.WaitOne(30000);

        Assert.True(waitResult);

        Assert.True(File.Exists(testFile1 + ".done"));
        Assert.True(File.Exists(testFile2 + ".done"));

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);
        File.Move(testFile2 + ".done", testFile2);

        var replay = context.Replays
            .Include(i => i.ReplayPlayers)
            .FirstOrDefault(f => f.ReplayHash == replayHash);

        Assert.Equal(2, replay?.ReplayPlayers.Count(c => c.IsUploader));
    }

    [Fact]
    public void A4DuplicateLastSpawnHashTest()
    {
        using var scope = serviceProvider.CreateScope();

        // DEBUG
        //var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        //context.Database.EnsureDeleted();
        //context.Database.Migrate();

        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile2 = "/data/ds/replayblobs/07d4bd5a-5507-498c-b79e-80a0ecc68e56/20230313-180342.base64";
        string testFile1 = "/data/ds/replayblobs/473e3232-b92f-4001-80ed-44addec32e63/20230313-172443.base64";
        string replayHash = "07e8dcf9939911fab712361aede0098a";

        Assert.True(File.Exists(testFile1));
        Assert.True(File.Exists(testFile2));

        ImportRequest request = new()
        {
            Replayblobs = new()
            {
                testFile1,
                testFile2
            }
        };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate (object? sender, EventArgs e)
        {
            jobDoneEvent.Set();
        };

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        importService.Import(request).Wait();

        var waitResult = jobDoneEvent.WaitOne(30000);

        Assert.True(waitResult);

        Assert.True(File.Exists(testFile1 + ".done"));
        Assert.True(File.Exists(testFile2 + ".done"));

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);
        File.Move(testFile2 + ".done", testFile2);

        var replay = context.Replays
            .Include(i => i.ReplayPlayers)
            .FirstOrDefault(f => f.ReplayHash == replayHash);

        Assert.Equal(2, replay?.ReplayPlayers.Count(c => c.IsUploader));
        Assert.Equal(599, replay?.Duration);
    }

    [Fact]
    public void A5DuplicateParallelTest()
    {
        using var scope = serviceProvider.CreateScope();

        // DEBUG
        //var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        //context.Database.EnsureDeleted();
        //context.Database.Migrate();

        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile1 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221205-201113.base64";
        string testFile2 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221225-191918.base64";
        string testFile3 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221225-211225.base64";
        string testFile4 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221226-053632.base64";
        string testFile5 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221226-234555.base64";
        string replayHash = "c594b7383e237d2d1442392cd04624d0";

        List<string> testFiles = new() { testFile1, testFile2, testFile3, testFile4, testFile5 };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate (object? sender, EventArgs e)
        {
            jobDoneEvent.Set();
        };

        foreach (var testFile in testFiles)
        {
            Assert.True(File.Exists(testFile));

            importService.Import(new() { Replayblobs = new() { testFile } }).Wait();
        }

        var waitResult = jobDoneEvent.WaitOne(120000);

        Assert.True(waitResult);

        testFiles.ForEach(f => Assert.True(File.Exists(f + ".done")));

        // Cleanup
        foreach (var testFile in testFiles)
        {
            File.Move(testFile + ".done", testFile);
        }

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replay = context.Replays
            .Include(i => i.ReplayPlayers)
            .FirstOrDefault(f => f.ReplayHash == replayHash);

        Assert.Equal(3, replay?.ReplayPlayers.Count(c => c.IsUploader));
        Assert.Equal(918, replay?.Duration);
    }

    [Fact(Skip = "special performance test only")]
    public void A6SpeedTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        DEBUGSeedUnitsUpgradesFromJson();

        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        // string testFile1 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221205-033218.base64";

        string testFile1 = "D:\\backup\\sc2dsstats\\replayblobs\\c182f07b-9263-402f-b8e9-bbc0bcb75b4d\\20221125-045426.base64";


        Assert.True(File.Exists(testFile1));

        ImportRequest request = new()
        {
            Replayblobs = new()
            {
                testFile1
            }
        };

        ManualResetEvent jobDoneEvent = new ManualResetEvent(false);

        importService.OnBlobsHandled += delegate (object? sender, EventArgs e)
        {
            jobDoneEvent.Set();
        };

        Stopwatch sw = Stopwatch.StartNew();

        importService.Import(request).Wait();

        var waitResult = jobDoneEvent.WaitOne(2400000);

        Assert.True(waitResult);

        sw.Stop();

        Assert.True(context.Replays.Any());

        Assert.True(File.Exists(testFile1 + ".done"));

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);

        Assert.Equal(0, sw.ElapsedMilliseconds);
    }

    private void DEBUGSeedUnitsUpgradesFromJson()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Unit> units = JsonSerializer.Deserialize<List<Unit>>(File.ReadAllText("/data/ds/units.json")) ?? new();
        context.Units.AddRange(units);
        context.SaveChanges();

        List<Upgrade> upgrades = JsonSerializer.Deserialize<List<Upgrade>>(File.ReadAllText("/data/ds/upgrades.json")) ?? new();
        context.Upgrades.AddRange(upgrades);
        context.SaveChanges();
    }
}