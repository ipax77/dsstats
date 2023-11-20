using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace dsstats.db8cli;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 43));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information);
            options.AddConsole();
        });

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
        });

        //services.AddOptions<DbImportOptions>()
        //.Configure(x =>
        //{
        //        x.ImportConnectionString = "DataSource=/data/ds/dsstats.db";
        //        x.IsSqlite = true;
        //    });

        //services.AddDbContext<ReplayContext>(options =>
        //{
        //    options.UseSqlite("DataSource=/data/ds/dsstats.db", p =>
        //    {
        //        p.CommandTimeout(600);
        //        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
        //    })
        //    .EnableDetailedErrors()
        //    .EnableSensitiveDataLogging();
        //});

        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddScoped<IWinrateService, WinrateService>();
        services.AddScoped<IBuildService, BuildService>();
        services.AddScoped<IPlayerService, PlayerService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        //var result = playerService.GetPlayerPlayerIdSummary(new(226401, 1, 2), RatingType.Cmdr, RatingCalcType.Dsstats)
        //    .GetAwaiter().GetResult();

        //PlayerDetailRequest request = new()
        //{
        //    RequestNames = new("PAX", 226401, 2, 1),
        //    TimePeriod = TimePeriod.Patch2_71,
        //    RatingType = RatingType.Cmdr,
        //};

        //var details = playerService.GetPlayerIdPlayerDetails(request).GetAwaiter().GetResult();

        var r = playerService.GetPlayerIdPlayerRatingDetails(new(226401, 1, 2), RatingType.Cmdr, RatingCalcType.Dsstats)
            .GetAwaiter().GetResult();

        Console.ReadLine();
    }

    public static async Task GetWinrate(ReplayContext context)
    {
        DateTime fromDate = new DateTime(2023, 07, 01);
        var group = from r in context.Replays
                  from rp in r.ReplayPlayers
                  join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                  join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                  where r.GameTime > fromDate
                   && r.Duration > 300
                   && rr.RatingType == RatingType.Cmdr
                  group new { rp, rr, rpr, r } by rp.Race into g
                  select new AvgResult()
                  {
                      Commander = g.Key,
                      Count = g.Count(),
                      AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                      AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                      Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                      Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                  };

        var data = await group.ToListAsync();
        data = data.Where(x => (int)x.Commander > 3).ToList();

        if (data.Count > 0)
        {
            Console.WriteLine(data[0]);
        }
    }

    public static async Task GetRawWinrate(ReplayContext context)
    {
        DateTime fromDate = new DateTime(2023, 10, 1);
        DateTime endDate = DateTime.Today;
        RatingType ratingType = RatingType.Cmdr;

        FormattableString sql = $@"SELECT 
    rp.Race as Commander,
	count(*) as Count,
    round(avg(rpr.Rating), 2) as AvgRating,
    round(avg(rpr.RatingChange), 2) as AvgGain,
    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as Wins
FROM Replays as r
INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
WHERE rr.RatingType = {ratingType}
    AND r.GameTime >= {fromDate}
    AND rp.Duration > 300
GROUP BY rp.Race
";

        var data = await context.Database
            .SqlQuery<AvgResult>(sql)
            .ToListAsync();

        if (data.Count > 0)
        {
            Console.WriteLine(data[0]);
        }
    }

    public static async Task GetVeryRawWinrate(ReplayContext context)
    {
        DateTime fromDate = new DateTime(2023, 10, 1);
        DateTime endDate = DateTime.Today;
        // DateTime endDate = DateTime.Today.AddDays(-5);
        RatingType ratingType = RatingType.Cmdr;

        string sql = $@"SELECT 
    rp.Race as Commander,
	count(*) as Count,
    round(avg(rpr.Rating), 2) as AvgRating,
    round(avg(rpr.RatingChange), 2) as AvgGain,
    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as Wins
FROM Replays as r
INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
WHERE rr.RatingType = {(int)ratingType}
    AND r.GameTime >= '{fromDate.ToString("yyyy-MM-dd")}'
    {(endDate > DateTime.Today.AddDays(-2) ? "" : $"AND r.GameTime < '{endDate.ToString("yyyy-MM-dd")}'")}
    AND rp.Duration > 300
GROUP BY rp.Race
";

        var data = await context.Database
            .SqlQueryRaw<AvgResult>(sql)
            .ToListAsync();

        if (data.Count > 0)
        {
            Console.WriteLine(data[0]);
        }
    }

    public record AvgResult
    {
        public Commander Commander { get; set; }
        public int Count { get; set; }
        public double AvgRating { get; set; }
        public double AvgGain { get; set; }
        public int Wins { get; set; }
        public int Replays { get; set; }
    }
}
