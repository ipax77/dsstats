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
public class LeaverTests : IDisposable
{
    private readonly UploadService uploadService;
    private readonly DbConnection _connection;
    private readonly DbContextOptions<ReplayContext> _contextOptions;

    public LeaverTests(IMapper mapper)
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

        var serviceProvider = serviceCollection.BuildServiceProvider();
        uploadService = new UploadService(serviceProvider, mapper, NullLogger<UploadService>.Instance);
    }

    ReplayContext CreateContext() => new ReplayContext(_contextOptions);
    public void Dispose() => _connection.Dispose();


    [Fact]
    public async Task LeaverUploadTest()
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

        string testFile = Startup.GetTestFilePath("replayDto2.json");

        Assert.True(File.Exists(testFile));
        ReplayDto? replayDto = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(testFile));
        Assert.NotNull(replayDto);
        if (replayDto == null)
        {
            return;
        }

        replayDto.ReplayPlayers.ToList().ForEach(f => f.IsUploader = false);

        replayDto.ReplayPlayers.ElementAt(0).IsUploader = true;

        var leaverReplay = GetLeaverReplay(replayDto);
        await uploadService.ImportReplays(ZipReplay(leaverReplay), uploaderDto1.AppGuid);
        await Task.Delay(3000);

        replayDto.ReplayPlayers.ElementAt(0).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(1).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto2.AppGuid);
        replayDto.ReplayPlayers.ElementAt(1).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(2).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto3.AppGuid);
        replayDto.ReplayPlayers.ElementAt(2).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(3).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto4.AppGuid);
        replayDto.ReplayPlayers.ElementAt(3).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(4).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto5.AppGuid);
        replayDto.ReplayPlayers.ElementAt(4).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(5).IsUploader = true;
        await uploadService.ImportReplays(ZipReplay(replayDto), uploaderDto6.AppGuid);

        await Task.Delay(5000);

        var countAfter = await context.Replays.CountAsync();
        Assert.Equal(countBefore, countAfter - 1);

        var dbReplay = await context.Replays
            .Include(i => i.ReplayPlayers)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.NotNull(dbReplay);
        Assert.Equal(6, dbReplay?.ReplayPlayers.Count(c => c.IsUploader));
    }

    private static ReplayDto GetLeaverReplay(ReplayDto replayDto, int mod = 2)
    {
        DateTime newGameTime = replayDto.GameTime.AddSeconds(-replayDto.Duration / mod);
        int newDuration = replayDto.Duration / mod;

        List<ReplayPlayerDto> replayPlayers = new();
        for (int i = 0; i < replayDto.ReplayPlayers.Count; i++)
        {
            var player = replayDto.ReplayPlayers.ElementAt(i);
            replayPlayers.Add(player with
            {
                Income = player.Income / mod,
                Army = player.Army / mod,
                Kills = player.Kills / mod,
                UpgradesSpent = player.UpgradesSpent / mod,
                Upgrades = player.Upgrades.Take(player.Upgrades.Count / mod).ToList(),
                Spawns = player.Spawns.Take(player.Spawns.Count / mod).ToList(),
                Duration = newDuration,
                PlayerResult = PlayerResult.None
            });
        }

        var newMiddle = String.Join('|', replayDto.Middle.Split('|').Chunk(2).First());

        var leaverReplay = replayDto with
        {
            GameTime = newGameTime,
            Duration = newDuration,
            ReplayPlayers = replayPlayers,
            WinnerTeam = 0,
            Minkillsum = replayPlayers.Select(s => s.Kills).Min(),
            Maxkillsum = replayPlayers.Select(s => s.Kills).Max(),
            Minarmy = replayPlayers.Select(s => s.Army).Min(),
            Minincome = replayPlayers.Select(s => s.Income).Min(),
            Maxleaver = newDuration - replayPlayers.Select(s => s.Duration).Min(),
            Middle = newMiddle
        };

        leaverReplay.ReplayHash = Data.GenHash(leaverReplay);

        return leaverReplay;
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
                    BattleNetId = 223456 + num
                }
            },
            Players = new List<PlayerUploadDto>()
            {
                new PlayerUploadDto()
                {
                    Name = "Test" + num,
                    ToonId = 223456 + num
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


