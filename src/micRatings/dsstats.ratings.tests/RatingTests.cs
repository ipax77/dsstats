using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;
using dsstats.ratings.api.Services;
using dsstats.ratings.api;
using pax.dsstats.shared;
using AutoMapper;

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


[TestCaseOrderer("dsstats.ratings.tests.AlphabeticalOrderer", "dsstats.ratings.tests")]
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
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        List<ReplayDto> replays = new();
        for (int i = 0; i < 10; i++)
        {
            replays.Add(CreateRandomTestReplay());
        }
        var dbreplays = replays.Select(mapper.Map<Replay>).ToList();
        dbreplays.ForEach(f => f.Imported = DateTime.UtcNow);
        context.Replays.AddRange(dbreplays);
        context.SaveChanges();

        Assert.True(context.Replays.Any());

        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        ratingsService.ProduceRatings().Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.ReplayRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());
    }

    [Fact]
    public void A2ContinueRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        Assert.True(context.Replays.Any());

        List<ReplayDto> replays = new();
        for (int i = 0; i < 10; i++)
        {
            replays.Add(CreateRandomTestReplay());
        }
        var dbreplays = replays.Select(mapper.Map<Replay>).ToList();
        dbreplays.ForEach(f => f.Imported = DateTime.UtcNow);
        context.Replays.AddRange(dbreplays);
        context.SaveChanges();

        var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();

        ratingsService.ProduceRatings().Wait();

        Assert.True(context.PlayerRatings.Any());
        Assert.True(context.ReplayRatings.Any());
        Assert.True(context.RepPlayerRatings.Any());
    }

    private ReplayDto CreateRandomTestReplay()
    {
        Random random = new Random();
        int winnerTeam = random.Next(1, 3);
        List<GameMode> gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic, GameMode.Standard };
        int gameModeIndex = random.Next(gameModes.Count);
        var gameMode = gameModes[gameModeIndex];
        var duration = random.Next(301, 5001);

        ReplayDto replayDto = new()
        {
            GameTime = DateTime.UtcNow,
            Duration = duration,
            WinnerTeam = winnerTeam,
            GameMode = gameMode,
            Minkillsum = random.Next(2, 22) * duration,
            Maxkillsum = random.Next(6, 44) * duration,
            Minarmy = random.Next(1, 22) * duration,
            Minincome = random.Next(1, 22) * duration * 50,
            Maxleaver = random.Next(0, 99),
            Playercount = 6,
            FileName = string.Empty,
            CommandersTeam1 = string.Empty,
            CommandersTeam2 = string.Empty,
            Middle = string.Empty,
            ReplayPlayers = new List<ReplayPlayerDto>()
            {
                new()
                {
                    Name = string.Empty,
                    GamePos = 1,
                    Team = 1,
                    PlayerResult = winnerTeam == 1 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                },
                new()
                {
                    Name = string.Empty,
                    GamePos = 2,
                    Team = 1,
                    PlayerResult = winnerTeam == 1 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                },
                new()
                {
                    Name = string.Empty,
                    GamePos = 3,
                    Team = 1,
                    PlayerResult = winnerTeam == 1 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                },
                new()
                {
                    Name = string.Empty,
                    GamePos = 4,
                    Team = 2,
                    PlayerResult = winnerTeam == 2 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                },
                new()
                {
                    Name = string.Empty,
                    GamePos = 5,
                    Team = 2,
                    PlayerResult = winnerTeam == 2 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                },
                new()
                {
                    Name = string.Empty,
                    GamePos = 6,
                    Team = 2,
                    PlayerResult = winnerTeam == 2 ? PlayerResult.Win : PlayerResult.Los,
                    Duration = duration,
                    Race = GetRandomCommander(gameMode, random),
                    Income = random.Next(1, 22) * duration * 50,
                    Army = random.Next(1, 23) * duration,
                    Kills = random.Next(6, 44) * duration,
                    UpgradesSpent = random.Next(1, 11) * duration,
                    Refineries = string.Empty,
                    TierUpgrades = string.Empty,
                    Player = new()
                    {
                        Name = RandomString(random.Next(3, 11), random),
                        ToonId = random.Next(1000, 10000000),
                        RegionId = random.Next(1, 4)
                    }
                }
            }
        };

        replayDto.ReplayHash = Data.GenHash(replayDto);
        return replayDto;
    }

    private static Commander GetRandomCommander(GameMode gameMode, Random random)
    {
        if (gameMode == GameMode.Standard)
        {
            return (Commander)random.Next(1, 4);
        }
        else
        {
            Commander[] cmdrs = Data.GetCommanders(Data.CmdrGet.NoStd).ToArray();
            return (Commander)(cmdrs.GetValue(random.Next(cmdrs.Length)) ?? Commander.Abathur);
        }
    }

    public static string RandomString(int length, Random random)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}