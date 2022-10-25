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
public class DuplicateUploaderTests : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public DuplicateUploaderTests(IMapper mapper)
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
            .EnableDetailedErrors()
            .EnableDetailedErrors();
        });

        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task SetUploaderTest()
    {
        var uploaderDto1 = GetUploaderDto(1);
        var uploaderDto2 = GetUploaderDto(2);
        var uploaderDto3 = GetUploaderDto(3);
        var uploaderDto4 = GetUploaderDto(4);
        var uploaderDto5 = GetUploaderDto(5);
        var uploaderDto6 = GetUploaderDto(6);

        await uploadService.CreateOrUpdateUploader(uploaderDto1);
        await uploadService.CreateOrUpdateUploader(uploaderDto2);
        await uploadService.CreateOrUpdateUploader(uploaderDto3);
        await uploadService.CreateOrUpdateUploader(uploaderDto4);
        await uploadService.CreateOrUpdateUploader(uploaderDto5);
        await uploadService.CreateOrUpdateUploader(uploaderDto6);

        var context = CreateContext();
        var countBefore = await context.Replays.CountAsync();

        string testFile = "/data/ds/uploadtest3.json";

        Assert.True(File.Exists(testFile));
        ReplayDto? replayDto = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(testFile))?.FirstOrDefault();
        Assert.NotNull(replayDto);
        if (replayDto == null)
        {
            return;
        }

        replayDto.Players.ToList().ForEach(f => f.IsUploader = false);

        replayDto.Players.ElementAt(0).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto1.AppGuid);
        replayDto.Players.ElementAt(0).IsUploader = false;
        replayDto.Players.ElementAt(1).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto2.AppGuid);
        replayDto.Players.ElementAt(1).IsUploader = false;
        replayDto.Players.ElementAt(2).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto3.AppGuid);
        replayDto.Players.ElementAt(2).IsUploader = false;
        replayDto.Players.ElementAt(3).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto4.AppGuid);
        replayDto.Players.ElementAt(3).IsUploader = false;
        replayDto.Players.ElementAt(4).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto5.AppGuid);
        replayDto.Players.ElementAt(4).IsUploader = false;
        replayDto.Players.ElementAt(5).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto6.AppGuid);

        await Task.Delay(5000);

        var countAfter = await context.Replays.CountAsync();
        Assert.Equal(countBefore, countAfter - 1);

        var dbReplay = await context.Replays
            .Include(i => i.Players)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.NotNull(dbReplay);
        Assert.Equal(6, dbReplay?.Players.Count(c => c.IsUploader));
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
                    BattleNetId = 123456 + num
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "Test" + num,
                    ToonId = 123456 + num
                },
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


