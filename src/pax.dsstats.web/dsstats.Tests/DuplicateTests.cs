using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System.Data.Common;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace dsstats.Tests;

// [Collection("Sequential")]
public class DuplicateTest : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public DuplicateTest(IMapper mapper)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<ReplayContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ReplayContext(_contextOptions);

        context.Database.EnsureCreated();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddDbContext<ReplayContext>(options =>
        {
            options.UseSqlite(_connection, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("SqliteMigrations");
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            //.EnableDetailedErrors()
            //.EnableDetailedErrors()
            ;
        });

        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddHttpClient();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        uploadService = new UploadService(serviceProvider, mapper, httpClientFactory, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task BasicDuplicateTest()
    {
        var uploaderDto1 = GetUploaderDto(1);
        var uploaderDto2 = GetUploaderDto(2);
        var uploaderDto3 = GetUploaderDto(3);
        var uploaderDto4 = GetUploaderDto(4);
        var uploaderDto5 = GetUploaderDto(5);
        var uploaderDto6 = GetUploaderDto(6);

        var context = CreateContext();
        var countBefore = await context.Uploaders.CountAsync();

        await uploadService.CreateOrUpdateUploader(uploaderDto1);
        await uploadService.CreateOrUpdateUploader(uploaderDto2);
        await uploadService.CreateOrUpdateUploader(uploaderDto3);
        await uploadService.CreateOrUpdateUploader(uploaderDto4);
        await uploadService.CreateOrUpdateUploader(uploaderDto5);
        await uploadService.CreateOrUpdateUploader(uploaderDto6);


        var countAfter = await context.Uploaders.CountAsync();


        Assert.Equal(countBefore + 6, countAfter);
    }

    private static UploaderDto GetUploaderDto(int num)
    {
        return new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 123456 + num,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "Test" + num,
                            ToonId = 123456 + num,
                            RegionId = 1
                        },
                    }
                }
            }
        };
    }

    private static string ZipReplay(ReplayDto replayDto)
    {
        return Zip(JsonSerializer.Serialize(new List<ReplayDto>() { replayDto }));
    }

    private static string Zip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            msi.CopyTo(gs);
        }
        return Convert.ToBase64String(mso.ToArray());
    }
}


