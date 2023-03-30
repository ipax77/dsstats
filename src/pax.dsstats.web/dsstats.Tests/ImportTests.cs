using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace dsstats.Tests;


[TestCaseOrderer("dsstats.Tests.AlphabeticalOrderer", "dsstats.Tests")]
public class ImportTests
{
    private ServiceProvider serviceProvider;

    // requires dsstats.import.api to be running (and restarted for each testrun)
    // requires dsstats.ratings.api to be running

    public ImportTests()
    {

        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("TestConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 41));

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = config.GetProperty("ImportTestConnectionString").GetString() ?? "");

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

        services.AddHttpClient("importClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("http://localhost:5259");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
                options.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(config.GetProperty("ImportAuthSecret").GetString() ?? "");
            });

        services.AddHttpClient("ratingsClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("http://localhost:5153");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
                options.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(config.GetProperty("ImportAuthSecret").GetString() ?? "");
            });

        services.AddSingleton<UploadService>();

        serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ImportA1BasicImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        // create uploader
        var uploaderDto = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 12345,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 12345
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 12346
                        }
                    }
                }
            }
        };

        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        var latestReplay = uploadService.CreateOrUpdateUploader(uploaderDto).GetAwaiter().GetResult();
        
        bool dbHasUploader = context.Uploaders.Any(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);

        var testFile = Startup.GetTestFilePath("uploadtest3.base64");
        var testDir = Path.Combine(Data.ReplayBlobDir, uploaderDto.AppGuid.ToString());
        //var importFile = Path.Combine(testDir, Path.GetFileName(testFile));

        //Directory.CreateDirectory(testDir);
        //File.Copy(testFile, importFile);

        var result = uploadService.ImportReplays(File.ReadAllText(testFile), uploaderDto.AppGuid, DateTime.UtcNow).GetAwaiter().GetResult();

        Assert.True(result);

        // cleanup
        Directory.Delete(testDir, true);
    }

    [Fact]
    public void ImportA2MultiImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        // create uploader
        var uploaderDto1 = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 123456,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 123456
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 123467
                        }
                    }
                }
            }
        };

        var uploaderDto2 = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 123457,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 123457
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 123468
                        }
                    }
                }
            }
        };

        var uploaderDto3 = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 123458,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 123458
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 123469
                        }
                    }
                }
            }
        };

        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        uploadService.CreateOrUpdateUploader(uploaderDto1).GetAwaiter().GetResult();
        uploadService.CreateOrUpdateUploader(uploaderDto2).GetAwaiter().GetResult();
        uploadService.CreateOrUpdateUploader(uploaderDto3).GetAwaiter().GetResult();

        var testFile1 = Startup.GetTestFilePath("uploadtest.base64");
        var testDir1 = Path.Combine(Data.ReplayBlobDir, uploaderDto1.AppGuid.ToString());

        var testFile2 = Startup.GetTestFilePath("uploadtest2.base64");
        var testDir2 = Path.Combine(Data.ReplayBlobDir, uploaderDto2.AppGuid.ToString());

        var testFile3 = Startup.GetTestFilePath("uploadtest3.base64");
        var testDir3 = Path.Combine(Data.ReplayBlobDir, uploaderDto3.AppGuid.ToString());

        var result1 = uploadService.ImportReplays(File.ReadAllText(testFile1), uploaderDto1.AppGuid, DateTime.UtcNow).GetAwaiter().GetResult();
        var result2 = uploadService.ImportReplays(File.ReadAllText(testFile2), uploaderDto2.AppGuid, DateTime.UtcNow).GetAwaiter().GetResult();
        var result3 = uploadService.ImportReplays(File.ReadAllText(testFile3), uploaderDto3.AppGuid, DateTime.UtcNow).GetAwaiter().GetResult();

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);

        // cleanup
        Directory.Delete(testDir1, true);
        Directory.Delete(testDir2, true);
        Directory.Delete(testDir3, true);
    }

    [Fact]
    public void ImportA3RatingsImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        // wait for import to finish
        Task.Delay(20000).Wait();

        var httpClient = httpClientFactory.CreateClient("ratingsClient");
        var result = httpClient.GetAsync("/api/v1/ratings").GetAwaiter().GetResult();

        Assert.True(result.IsSuccessStatusCode);
    }
}
