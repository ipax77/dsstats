using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace dsstats.cli;

public class DbToJson
{
    readonly IServiceProvider serviceProvider;

    public DbToJson()
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 40));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(120);
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddLogging();

        services.AddTransient<IReplayRepository, ReplayRepository>();

        serviceProvider = services.BuildServiceProvider();
    }

    //public void TestDeserializeJson(string file)
    //{
    //    string json = File.ReadAllText(file);
    //    var jsonReplays = JsonSerializer.Deserialize<List<ReplayDsRDto>>(json);
    //}

    public async Task SaveDbAsJsonFile(string file)
    {
        string json = await ConvertDbToJson();
        File.WriteAllText(file, json);
    }

    public async Task<string> ConvertDbToJson()
    {
        var replayDsRDtos = await GetReplayDsRDtos(default, default);
        
        return JsonSerializer.Serialize(replayDsRDtos, new JsonSerializerOptions() { WriteIndented = true });
    }

    private async Task<List<ReplayDsRDto>> GetReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.Replays
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && gameModes.Contains(r.GameMode))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime > startTime);
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
}
