using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.ratings.lib;
using dsstats.db8services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using AutoMapper;

namespace dsstats.cli;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 42));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddSingleton<CalcService>();

        services.AddScoped<ICalcRepository, CalcRepository>();
        services.AddScoped<IWinrateService, WinrateService>();
        services.AddScoped<IReplaysService, ReplaysService>();
        // services.AddScoped<IDsstatsService, DsstatsService>();
        // services.AddScoped<IArcadeService, ArcadeService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IBuildService, BuildService>();
        services.AddScoped<ICmdrInfoService, CmdrInfoService>();

        var serviceProvider = services.BuildServiceProvider();

        Stopwatch sw = Stopwatch.StartNew();

        if (args.Length == 2)
        {
            if (args[0] == "ratings")
            {
                var bab = args[1] switch
                {
                    "dsstats" => CreateRatings(RatingCalcType.Dsstats, serviceProvider).GetAwaiter().GetResult(),
                    "arcade" => CreateRatings(RatingCalcType.Arcade, serviceProvider).GetAwaiter().GetResult(),
                    "combo" => CreateRatings(RatingCalcType.Combo, serviceProvider).GetAwaiter().GetResult(),
                    _ => Task.Run(() => { throw new NotImplementedException(); return true; }).GetAwaiter().GetResult()
                };
            }
            else if (args[0] == "testdata")
            {
                if (args.Length > 1 && args[1] == "adjust")
                {
                    CreateAdjustTestData(serviceProvider).GetAwaiter().GetResult();
                }
            }
        }

        sw.Stop();
        Console.WriteLine($"job done in {sw.ElapsedMilliseconds} ms.");

        Console.ReadLine();
    }

    private static async Task<bool> CreateRatings(RatingCalcType ratingCalcType, ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();

        logger.LogInformation("Producing {type} ratings", ratingCalcType.ToString());
        await calcService.GenerateRatings(ratingCalcType, recalc: true);

        return true;
    }

    public static async Task CreateAdjustTestData(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays1 = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Where(x =>
                x.GameTime > new DateTime(2023, 1, 1)
                && x.ResultCorrected
                && x.WinnerTeam == 1)
            .OrderByDescending(o => o.ReplayId)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        var replays2 = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Where(x =>
                x.GameTime > new DateTime(2023, 1, 1)
                && x.ResultCorrected
                && x.WinnerTeam == 2)
            .OrderByDescending(o => o.ReplayId)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        for (int i = 0; i < replays1.Count; i++)
        {
            var replay = replays1[i];
            ResetReplayWinner(replay);
            var replayDto = mapper.Map<ReplayDto>(replay);
            var json = JsonSerializer.Serialize(replayDto);
            File.WriteAllText($"/data/ds/adjustTestTeam1_{i}.json", json);
        }

        for (int i = 0; i < replays2.Count; i++)
        {
            var replay = replays2[i];
            ResetReplayWinner(replay);
            var replayDto = mapper.Map<ReplayDto>(replay);
            var json = JsonSerializer.Serialize(replayDto);
            File.WriteAllText($"/data/ds/adjustTestTeam2_{i}.json", json);
        }
    }

    private static void ResetReplayWinner(Replay replay)
    {
            replay.WinnerTeam = 0;
            foreach (var rp in replay.ReplayPlayers)
            {
                rp.PlayerResult = PlayerResult.None;
            }
    }
}
