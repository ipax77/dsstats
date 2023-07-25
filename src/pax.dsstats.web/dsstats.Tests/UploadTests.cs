using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
    private readonly IMapper mapper;

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

        serviceCollection.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = "Filename=:memory:");
        serviceCollection.AddHttpClient();
        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddSingleton<pax.dsstats.web.Server.Services.Import.ImportService>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var dbImportOptions = serviceProvider.GetRequiredService<IOptions<DbImportOptions>>();

        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
        this.mapper = mapper;
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
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 1234,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 1234
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 1235
                        }
                    }
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
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 1234,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 1234
                        }
                    }
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
        Assert.True(dbHasUploader, "Uploader does not exist, yet");

        var uploaderDto2 = new UploaderDto()
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
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPaxX",
                            ToonId = 12347
                        }
                    }
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
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 72345,
                    PlayerUploadDtos = new List<PlayerUploadDto>()
                    {
                        new PlayerUploadDto()
                        {
                            Name = "PAX",
                            ToonId = 72345
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 72346
                        }
                    }
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

    [Fact]
    public async Task ChangeUploaderPlayers()
    {
        Guid appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 77123
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);

        var changeUploaderDto = new UploaderDto()
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
        latestReplay = await uploadService.CreateOrUpdateUploader(changeUploaderDto);
        bool uploaderHasPlayers = await context.Players
            .Include(i => i.Uploader)
            .Where(x => x.Uploader != null && x.Uploader.AppGuid == appGuid)
            .AnyAsync();

        Assert.True(uploaderHasPlayers);
    }

    [Fact]
    public async Task ChangeUploaderWithExistingPlayers()
    {
        Guid appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 77123
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);

        context.Players.Add(new Player()
        {
            Name = "PAX",
            ToonId = 771234
        });
        context.Players.Add(new Player()
        {
            Name = "xPax",
            ToonId = 771235
        });
        await context.SaveChangesAsync();

        var changeUploaderDto = new UploaderDto()
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
                            ToonId = 771234
                        },
                        new PlayerUploadDto()
                        {
                            Name = "xPax",
                            ToonId = 771235
                        }
                    }
                }
            }
        };
        latestReplay = await uploadService.CreateOrUpdateUploader(changeUploaderDto);
        int uploaderPlayersCount = await context.Players
            .Include(i => i.Uploader)
            .Where(x => x.Uploader != null && x.Uploader.AppGuid == appGuid)
            .CountAsync();

        Assert.Equal(changeUploaderDto.BattleNetInfos.SelectMany(s => s.PlayerUploadDtos).Count(), uploaderPlayersCount);
    }

    [Fact]
    public async Task DisableUploadTest()
    {
        Guid appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 77123
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);

        await uploadService.DisableUploader(appGuid);

        latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        Assert.Null(latestReplay);
    }

    [Fact]
    public async Task DeleteUploadTest()
    {
        Guid appGuid = Guid.NewGuid();

        var uploaderDto = new UploaderDto()
        {
            AppGuid = appGuid,
            AppVersion = "0.0.1",
            BattleNetInfos = new List<BattleNetInfoDto>()
            {
                new BattleNetInfoDto()
                {
                    BattleNetId = 77123
                }
            }
        };

        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        var context = CreateContext();
        bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
        Assert.True(dbHasUploader);

        await uploadService.DeleteUploader(appGuid);

        latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

        Assert.Null(latestReplay);
    }

    //[Fact]
    //public async Task DuplicateUploaderTest()
    //{
    //    Guid appGuid = Guid.NewGuid();

    //    var uploaderDto = new UploaderDto()
    //    {
    //        AppGuid = appGuid,
    //        AppVersion = "0.0.1",
    //        BattleNetInfos = new List<BattleNetInfoDto>()
    //        {
    //            new BattleNetInfoDto()
    //            {
    //                BattleNetId = 77123
    //            }
    //        }
    //    };

    //    var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto);

    //    var context = CreateContext();
    //    bool dbHasUploader = await context.Uploaders.AnyAsync(a => a.AppGuid == uploaderDto.AppGuid);
    //    Assert.True(dbHasUploader);

    //    var countBefore = await context.Uploaders.CountAsync();

    //    latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto with { AppGuid = Guid.NewGuid() });

    //    Assert.NotNull(latestReplay);

    //    var countAfter = await context.Uploaders.CountAsync();

    //    Assert.Equal(countBefore, countAfter);
    //}
}
