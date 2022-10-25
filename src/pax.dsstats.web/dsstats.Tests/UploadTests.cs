using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;
using System.Data.Common;

namespace dsstats.Tests;

// [Collection("Sequential")]
public class UploadTests : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public UploadTests(IMapper mapper)
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
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
                ServiceLifetime.Transient);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task CreateUploader()
    {
        var uploaderDto = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 12345
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 12345
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 12346
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);
    }

    [Fact]
    public async Task ModifyUploaderRemovePlayer()
    {
        Guid appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 1234
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 1234
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 1235
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == appGuid);
        Assert.True(dbHasUploader, "Uploader does not exist, yet");

        var hasOldPlayer = context.Players.Any(a => a.ToonId == 1235);
        Assert.True(hasOldPlayer, "Oldplayer wasn't even created.");


        var uploaderDto2 = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 1234
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 1234
                }
            }
        };

        var latestReplay2 = await uploadService.CreateOrUpdateUploader(uploaderDto2);

        hasOldPlayer = context.Players.Any(a => a.ToonId == 1235);
        Assert.True(hasOldPlayer, "Uploader removePlayer got deleted");

        var uploader = await context.Uploaders
                        .Include(i => i.Players)
                        .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.True(uploader?.Players.Count == 1, "Uploader Players Count not as expected");
    }

    [Fact]
    public async Task ModifyUploaderAddPlayer()
    {
        var appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 12345
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 12345
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 12346
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == appGuid);
        Assert.True(dbHasUploader, "Uploader does not exist, yet");

        var uploaderDto2 = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 12345
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 12345
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 12346
                },
                new PlayerUploadDto()
                {
                    Name = "xPaxX",
                    Toonid = 12347
                }
            }
        };

        var latestReplay2 = await uploadService.CreateOrUpdateUploader(uploaderDto2);

        var hasNewPlayer = context.Players.Any(a => a.ToonId == 12347);
        Assert.True(hasNewPlayer, "Uploader removePlayer got deleted");

        var uploader = await context.Uploaders
                        .Include(i => i.Players)
                        .FirstOrDefaultAsync(f => f.AppGuid == appGuid);

        Assert.True(uploader?.Players.Count == 3, "Uploader Players Count not as expected");
    }

    [Fact]
    public async Task ModifyUploaderNewApp()
    {
        var uploaderDto = new UploaderDto()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "0.0.1",
            BatteBattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 72345
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "PAX",
                    Toonid = 72345
                },
                new PlayerUploadDto()
                {
                    Name = "xPax",
                    Toonid = 72346
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader, "Uploader does not exist, yet");

        var uploaderDto2 = uploaderDto with { AppGuid = Guid.NewGuid() };

        var latestReplay2 = await uploadService.CreateOrUpdateUploader(uploaderDto2);

        var uploader = await context.Uploaders
                        .Include(i => i.Players)
                        .FirstOrDefaultAsync(f => f.AppGuid == uploaderDto2.AppGuid);

        Assert.NotNull(uploader);
    }
}
