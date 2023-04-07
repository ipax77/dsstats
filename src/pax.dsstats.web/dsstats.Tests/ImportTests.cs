using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
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

    // curl -X 'GET' -H 'Authorization: mySecretPw' -H 'Accept: application/json' http://localhost:5153/api/v1/ratings/reports
    // curl -X 'GET' -H 'Authorization: mySecretPw' -H 'Accept: application/json' http://localhost:5259/api/v1/import

    const string skip = "Class ImportTests disabled";

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

    [Fact(Skip = skip)]
    public void ImportA1BasicImportTest()
    {
        // start depending microservices

        ProcessStartInfo importStartInfo;
        ProcessStartInfo ratingsStartInfo;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            importStartInfo = new ProcessStartInfo();
            importStartInfo.FileName = Startup.GetCmdFilePath();
            importStartInfo.Arguments = "\"dotnet run\" \"../../../../../micImport/dsstats.import.api\"";
            importStartInfo.UseShellExecute = false;
            importStartInfo.CreateNoWindow = false;
            importStartInfo.WorkingDirectory = "../../../../../micImport/dsstats.import.api";

            ratingsStartInfo = new ProcessStartInfo();
            ratingsStartInfo.FileName = Startup.GetCmdFilePath();
            ratingsStartInfo.Arguments = "\"dotnet run\" \"../../../../../micRatings/dsstats.ratings.api\"";
            ratingsStartInfo.UseShellExecute = false;
            ratingsStartInfo.CreateNoWindow = false;
            ratingsStartInfo.WorkingDirectory = "../../../../../micRatings/dsstats.ratings.api";
        }
        else
        {
            throw new NotImplementedException();
        }
        using var importProcess = Process.Start(importStartInfo);
        importProcess?.WaitForExit();

        Task.Delay(4000).Wait();
        
        using var ratingsProcss = Process.Start(ratingsStartInfo);
        ratingsProcss?.WaitForExit();

        // test start

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

    [Fact(Skip=skip)]
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

    [Fact(Skip=skip)]
    public void ImportA3RatingsImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        // wait for import to finish
        Task.Delay(20000).Wait();

        var importClient = httpClientFactory.CreateClient("importClient");
        var importResult = importClient.GetFromJsonAsync<ImportResult>("/api/v1/import").GetAwaiter().GetResult();

        Assert.NotNull(importResult);
        if (importResult == null)
        {
            return;
        }

        Assert.True(importResult.LatestImports > 0);

        var httpClient = httpClientFactory.CreateClient("ratingsClient");
        var result = httpClient.GetAsync("/api/v1/ratings").GetAwaiter().GetResult();

        Assert.True(result.IsSuccessStatusCode);

        // wait for ratings being produced
        Task.Delay(5000).Wait();

        var reports = httpClient.GetFromJsonAsync<List<RatingsReport>>("/api/v1/ratings/reports").GetAwaiter().GetResult();
        Assert.NotNull(reports);
        if (reports == null)
        {
            return;
        }
        Assert.True(reports.Any());
    }

    [Fact(Skip=skip)]
    public void ImportA5RatingsWhileImportTest()
    {
        using var scope = serviceProvider.CreateScope();

        var testFile1 = "/data/ds/replayblobs/00000000-0000-0000-0000-000000000000/20221205-013659.base64.done";

        // create uploader
        var uploaderDto1 = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 223456,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 223456
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 223467
                        }
                    }
                }
            }
        };

        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();
        uploadService.CreateOrUpdateUploader(uploaderDto1).GetAwaiter().GetResult();

        // trigger import replays
        var result1 = uploadService.ImportReplays(File.ReadAllText(testFile1), uploaderDto1.AppGuid, DateTime.UtcNow).GetAwaiter().GetResult();

        Assert.True(result1);

        var testDir1 = Path.Combine(Data.ReplayBlobDir, uploaderDto1.AppGuid.ToString());

        Task.Delay(1000).Wait();

        // trigger ratings calculation
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var httpClient = httpClientFactory.CreateClient("ratingsClient");
        var result = httpClient.GetAsync("/api/v1/ratings").GetAwaiter().GetResult();

        Assert.True(result.IsSuccessStatusCode);

        // cleanup
        Directory.Delete(testDir1, true);
    }
}
