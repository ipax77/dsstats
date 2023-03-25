using System.Diagnostics;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace dsstats.import.tests;

public class ImportSpeedTests
{
    private ServiceProvider serviceProvider;

    public ImportSpeedTests()
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
        services.AddScoped<ImportService>();

        serviceProvider = services.BuildServiceProvider();
    }

    // [Fact]
    // public void SpeedTest()
    // {
    //     using var scope = serviceProvider.CreateScope();
    //     var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
    //     context.Database.EnsureDeleted();
    //     context.Database.Migrate();

    //     var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    //     var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

    //     string testFile = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221205-033218.base64";
    //     Assert.True(File.Exists(testFile));

    //     Stopwatch sw = Stopwatch.StartNew();
    //     var base64String = File.ReadAllText(testFile);

    //     var replayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(ImportService.UnzipAsync(base64String).GetAwaiter().GetResult());
    //     Assert.True(replayDtos?.Any());
    //     if (replayDtos == null)
    //     {
    //         return;
    //     }

    //     List<Replay> replays = replayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
    //     replays.ForEach(f => f.UploaderId = 1);

    //     var report = importService.ImportReplays(replays, new()).GetAwaiter().GetResult();

    //     sw.Stop();
    //     Assert.True(context.Replays.Any());

    //     Assert.Equal(0, sw.ElapsedMilliseconds);
    // }
}