using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using dsstats.raven;
using pax.dsstats.dbng;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using Raven.Client.Documents.Session;
using System.Text;
using System.Globalization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;

namespace dsstats.mmrconsole;

internal class Program
{
    static void Main(string[] args)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 40));

        var services = new ServiceCollection();

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddSingleton<DocumentStoreHolder>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddLogging(builder =>
            {
                builder.ClearProviders();
                // Clear Microsoft's default providers (like eventlogs and others)
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
                }).SetMinimumLevel(LogLevel.Warning);
            });
        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            // .EnableDetailedErrors()
            // .EnableSensitiveDataLogging()
            ;
        });

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        // END SERVICE CONFIG

        Stopwatch sw = Stopwatch.StartNew();

        //Produce(serviceProvider);
        // ProduceStd(serviceProvider);
        ChunkProduce(serviceProvider);

        sw.Stop();
        Console.WriteLine($"jobs done in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Request()
    {
        RatingsRequest ratingsRequest = new()
        {
            Skip = 20,
            Take = 40,
            // Search = "Feralan",
            Orders = new()
            {
                new()
                {
                    Property = "Games",
                    Ascending = true
                },

            }
        };

        // var ratings = RatingRepository.GetPlayerRatings(ratingsRequest).GetAwaiter().GetResult();
    }

    internal static void CollectInital(int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        var ratings = session.Query<PlayerRating>()
            .Statistics(out QueryStatistics stats)
            .OrderByDescending(o => o.Mmr)
            .Take(take)
            .ToList();

        Console.WriteLine($"got init data ({ratings.Count}|{stats.TotalResults}) in {stats.DurationInMs} ms");
    }

    internal static void Collect(int skip, int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        Stopwatch sw = Stopwatch.StartNew();

        // var ratings = session.Query<PlayerInfo_ByPlayerIdAndRatingTypeCmdr.Result>()
        //     .Where(x => x.Rating != null && x.Rating.Games > 10)
        //     .OfType<Rating>()
        //     .Skip(skip)
        //     .Take(take)
        //     .ToList();

        sw.Stop();

        //Console.WriteLine($"got data ({ratings.Count}) in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Produce(IServiceProvider serviceProvider)
    {

        Stopwatch sw = Stopwatch.StartNew();

        var replays = GetCmdrReplayDsRDtos(serviceProvider, DateTime.MinValue, DateTime.MinValue)
            .GetAwaiter().GetResult();
        sw.Stop();
        Console.WriteLine($"got data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        var cmdrMmrs = GetCommanderMmrs(serviceProvider).GetAwaiter().GetResult();
        var cmdrMmrDic = cmdrMmrs.ToDictionary(k => new CmdrMmmrKey(k.Race, k.OppRace), v => new CmdrMmmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
        Dictionary<int, CalcRating> mmrIdRatigns = new();

        (mmrIdRatigns, var maxMmr) = MmrService.GeneratePlayerRatings(replays, cmdrMmrDic, mmrIdRatigns, MmrService.startMmr, ratingRepository, new())
            .GetAwaiter().GetResult();

        sw.Stop();

        Console.WriteLine($"calculated data ({replays.Count}) in {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        var result = ratingRepository.UpdatePlayerRatings<PlayerRatingCmdr>(MmrService.GetPlayerRatings(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct().ToList(), mmrIdRatigns))
            .GetAwaiter().GetResult();

        Console.WriteLine(result);
        sw.Stop();

        Console.WriteLine($"data stored in {sw.ElapsedMilliseconds} ms");
    }

    internal static void ChunkProduce(IServiceProvider serviceProvider)
    {

        Stopwatch sw = Stopwatch.StartNew();

        DateTime startTime = new DateTime(2018, 1, 1);
        
        Dictionary<int, CalcRating> mmrIdRatigns = new();
        var cmdrMmrs = GetCommanderMmrs(serviceProvider).GetAwaiter().GetResult();
        var cmdrMmrDic = cmdrMmrs.ToDictionary(k => new CmdrMmmrKey(k.Race, k.OppRace), v => new CmdrMmmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        HashSet<PlayerDsRDto> players = new();
        
        int replayCount = 0;

        while (startTime < DateTime.Today)
        {
            var replays = GetCmdrReplayDsRDtos(serviceProvider, startTime, startTime.AddYears(1))
                .GetAwaiter().GetResult();
            
            startTime = startTime.AddYears(1);
            
            if (!replays.Any())
            {
                continue;
            }
            
            replayCount += replays.Count;
            players.UnionWith(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct());
            
            sw.Stop();
            Console.WriteLine($"got data in {sw.ElapsedMilliseconds} ms");
            (mmrIdRatigns, var maxMmr) = MmrService.GeneratePlayerRatings(replays, cmdrMmrDic, mmrIdRatigns, MmrService.startMmr, ratingRepository, new())
                .GetAwaiter().GetResult();
            sw.Restart();
        }

        sw.Stop();

        Console.WriteLine($"calculated data ({replayCount}) in {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        var result = ratingRepository.UpdatePlayerRatings<PlayerRatingCmdr>(MmrService.GetPlayerRatings(players.ToList(), mmrIdRatigns))
            .GetAwaiter().GetResult();

        Console.WriteLine(result);
        sw.Stop();

        Console.WriteLine($"data stored in {sw.ElapsedMilliseconds} ms");
    }

    internal static void ProduceStd(IServiceProvider serviceProvider)
    {

        Stopwatch sw = Stopwatch.StartNew();

        var replays = GetStdReplayDsRDtos(serviceProvider, DateTime.MinValue, DateTime.MinValue)
            .GetAwaiter().GetResult();
        sw.Stop();
        Console.WriteLine($"got data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        var cmdrMmrs = GetCommanderMmrs(serviceProvider).GetAwaiter().GetResult();
        var cmdrMmrDic = cmdrMmrs.ToDictionary(k => new CmdrMmmrKey(k.Race, k.OppRace), v => new CmdrMmmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
        Dictionary<int, CalcRating> mmrIdRatigns = new();

        (mmrIdRatigns, var maxMmr) = MmrService.GeneratePlayerRatings(replays, cmdrMmrDic, mmrIdRatigns, MmrService.startMmr, ratingRepository, new())
            .GetAwaiter().GetResult();

        sw.Stop();

        Console.WriteLine($"calculated data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        //var result = ratingRepository.UpdatePlayerRatings(ratingResult.Values.ToList()).GetAwaiter().GetResult();
        var result = ratingRepository.UpdatePlayerRatings<PlayerRatingStd>(MmrService.GetPlayerRatings(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct().ToList(), mmrIdRatigns))
            .GetAwaiter().GetResult();

        Console.WriteLine(result);
        sw.Stop();

        Console.WriteLine($"data stored in {sw.ElapsedMilliseconds} ms");
    }

    private static async Task SaveReplayPlayersData(IServiceProvider serviceProvider, List<ReplayPlayerMmrChange> replayPlayerMmrChanges)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var replayPlayerMmrChange in replayPlayerMmrChanges)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.ReplayPlayers)}" +
                $" SET {nameof(ReplayPlayer.MmrChange)} = {replayPlayerMmrChange.MmrChange.ToString(CultureInfo.InvariantCulture)}" +
                $" WHERE {nameof(ReplayPlayer.ReplayPlayerId)} = {replayPlayerMmrChange.ReplayPlayerId}; ");
            i++;
            if (i % 1000 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    private static async Task SavePlayersData(IServiceProvider serviceProvider, List<PlayerRating> playerRatings)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var playerRating in playerRatings)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.Players)}" +
                $" SET {nameof(Player.Mmr)} = {playerRating.Mmr.ToString(CultureInfo.InvariantCulture)}, {nameof(Player.MmrOverTime)} = ''" +
                $" WHERE {nameof(Player.PlayerId)} = {playerRating.PlayerId}; ");

            i++;
            if (i % 500 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }


        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    public static async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(IServiceProvider serviceProvider, DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime >= startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public static async Task<List<ReplayDsRDto>> GetStdReplayDsRDtos(IServiceProvider serviceProvider, DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && r.GameMode == GameMode.Standard)
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime >= startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public static async Task<List<CommanderMmr>> GetCommanderMmrs(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var cmdrMmrs = await context.CommanderMmrs
            .AsNoTracking()
            .ToListAsync()
        ;

        if (!cmdrMmrs.Any())
        {
            var commanderMmrs = await context.CommanderMmrs.ToListAsync();
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            foreach (var race in allCommanders)
            {
                foreach (var oppRace in allCommanders)
                {
                    CommanderMmr cmdrMmr = new()
                    {
                        Race = race,
                        OppRace = oppRace,

                        SynergyMmr = MmrService.startMmr,
                        AntiSynergyMmr = MmrService.startMmr
                    };
                    cmdrMmrs.Add(cmdrMmr);
                }
            }
            context.CommanderMmrs.AddRange(cmdrMmrs);
            await context.SaveChangesAsync();
        }
        return cmdrMmrs;
    }
}


