using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System;
using System.Data.Common;

namespace dsstats.Tests;

// [Collection("Sequential")]
public class ImportTests : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public ImportTests(IMapper mapper)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<ReplayContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ReplayContext(_contextOptions);

        context.Database.EnsureCreated();

        //if (context.Database.EnsureCreated())
        //{
        //    using var viewCommand = context.Database.GetDbConnection().CreateCommand();
        //    viewCommand.CommandText = @"
        //        CREATE VIEW AllResources AS
        //        SELECT Url
        //        FROM Blogs;";
        //    viewCommand.ExecuteNonQuery();
        //}
        //context.AddRange(
        //    new Blog { Name = "Blog1", Url = "http://blog1.com" },
        //    new Blog { Name = "Blog2", Url = "http://blog2.com" });
        //context.SaveChanges();

        var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        //        ServiceLifetime.Transient);

        //serviceCollection.AddDbContext<ReplayContext>(options =>
        //{
        //    options.UseSqlite(_connection, sqlOptions =>
        //    {
        //        sqlOptions.MigrationsAssembly("SqliteMigrations");
        //        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        //    })
        //    .EnableDetailedErrors()
        //    .EnableSensitiveDataLogging();
        //});

        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();
        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task UploadTest()
    {
        var appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
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

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == appGuid);
        Assert.True(dbHasUploader);

        string testFile = Startup.GetTestFilePath("uploadtest.base64");

        Assert.True(File.Exists(testFile));
        var base64String = File.ReadAllText(testFile);

        await uploadService.ImportReplays(base64String, appGuid);

        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.NotNull(uploader);
        Assert.True(uploader?.LatestUpload > DateTime.MinValue);

        await Task.Delay(5000);

        Assert.True(context.Replays.Any());

    }

    [Fact]
    public async Task UploadReplaysTest()
    {
        var appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
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

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == appGuid);
        Assert.True(dbHasUploader);

        string testFile = Startup.GetTestFilePath("uploadtest.base64");

        Assert.True(File.Exists(testFile));
        var base64String = File.ReadAllText(testFile);

        await uploadService.ImportReplays(base64String, appGuid);

        var uploader = await context.Uploaders.FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.NotNull(uploader);
        Assert.True(uploader?.LatestUpload > DateTime.MinValue);

        await Task.Delay(5000);

        Assert.True(context.Replays.Any());

        var dbUploader = await context.Uploaders
            .Include(i => i.Players)
            .Include(i => i.Replays)
            .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.True(dbUploader?.Replays.Count > 0);
    }
}


