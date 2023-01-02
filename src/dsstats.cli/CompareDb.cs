using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using System.Text.Json;

namespace dsstats.cli;

public static class CompareDb
{
    public static async Task CompareJsonToDb(string jsonDir, string sqliteDbFile = @"C:\Users\pax77\AppData\Local\Packages\29898PhilippHetzner.141231D0ED353_2yg8b125yd1c6\LocalState\dsstats3.db")
    {
        if (!Directory.Exists(jsonDir))
        {
            Console.WriteLine($"Json directory not found: {jsonDir}");
            return;
        }

        if (!File.Exists(sqliteDbFile))
        {
            Console.WriteLine($"Sqlite db not found: {sqliteDbFile}");
            return;
        }

        var services = new ServiceCollection();

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseSqlite($"Data Source={sqliteDbFile}",
                x =>
                {
                    x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                });
        });

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dbInfos = await context.Replays
            .Select(s => new
            {
                s.ReplayHash,
                s.GameTime,
                s.Playercount
            })
            .ToListAsync();

        Dictionary<string, KeyValuePair<DateTime, int>> dbDic =
            dbInfos.ToDictionary(k => k.ReplayHash, v => new KeyValuePair<DateTime, int>(v.GameTime, v.Playercount));

        int i = dbInfos.Count;
        foreach (var file in Directory.GetFiles(jsonDir, "*.json", SearchOption.TopDirectoryOnly))
        {
            var replayDto = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(file));
            if (replayDto == null)
            {
                Console.WriteLine($"failed reading replayDto from json: {file}");
                continue;
            }

            if (!dbDic.TryGetValue(replayDto.ReplayHash, out var info))
            {
                var dbReplay = await context.Replays
                    .FirstOrDefaultAsync(f => f.FileName == replayDto.FileName);
                if (dbReplay == null)
                {
                    Console.WriteLine($"replay hash not found in db: {replayDto.FileName} - {replayDto.ReplayHash}");
                }
                else
                {
                    Console.WriteLine($"replayHash missmatch: {replayDto.FileName}, {replayDto.GameMode} <=> {dbReplay.GameMode}");
                }
            }
            else
            {
                if (replayDto.GameTime != info.Key || replayDto.Playercount != info.Value)
                {
                    Console.WriteLine($"info missmatch for {replayDto.FileName}: {replayDto.GameTime} <=> {info.Key}, {replayDto.Playercount} <=> {info.Value}");
                }
            }
            i--;
        }
        Console.WriteLine($"dbInfos unchecked: {i}");
    }
}
