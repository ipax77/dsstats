using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;
using dsstats.ratings.api.Services;
using dsstats.ratings.api;

namespace dsstats.ratings.tests;

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


[TestCaseOrderer("dsstats.ratings.api.tests.AlphabeticalOrderer", "dsstats.ratings.api.tests")]
public class RatingTests
{
    private readonly ServiceProvider serviceProvider;

    public RatingTests()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("TestConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportTestConnectionString").GetString() ?? "";
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 41));

        var services = new ServiceCollection();

        services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

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
        services.AddSingleton<RatingsService>();

        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void A1BasicRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        // context.Database.EnsureDeleted();
        // context.Database.Migrate();

        Assert.True(context.Replays.Any());

        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        ratingsService.ProduceRatings().Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.ReplayRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());
    }
}