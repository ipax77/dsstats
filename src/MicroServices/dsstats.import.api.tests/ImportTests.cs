using dsstats.import.api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
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
        var serverVersion = new MySqlServerVersion(new Version(5, 0, 41));

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

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        ArgumentNullException.ThrowIfNull(context);

        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }

    [Fact]
    public void A1BlobImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        Assert.NotNull(importService);
        ArgumentNullException.ThrowIfNull(importService, nameof(ImportService));

        string testFile1 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-182938.base64";
        string testFile2 = "/data/ds/replayblobs/3fdfdead-9d5c-461d-b119-406332b6d2f9/20230103-185756.base64";

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

        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.True(context.Replays.Any());

        Assert.True(File.Exists(testFile1 + ".done"));
        Assert.True(File.Exists(testFile2 + ".done"));

        // Cleanup
        File.Move(testFile1 + ".done", testFile1);
        File.Move(testFile2 + ".done", testFile2);
    }
}