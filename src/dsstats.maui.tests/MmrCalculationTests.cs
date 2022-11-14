
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using pax.dsstats.dbng.Repositories;
using System.Text.Json;
using pax.dsstats.shared;
using System.Reflection;

namespace dsstats.maui.tests;

public class MmrCalculationTests : TestWithSqlite
{
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    [Fact]
    public async Task MmrTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        foreach (var replayDto in testReplays1)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();

        // Assert
        Assert.True(mmrService.ToonIdRatings.Any());
    }

    [Fact]
    public async Task MmrConsistencyTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        foreach (var replayDto in testReplays1)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();
        var dataBefore = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);
        await mmrService.ReCalculateWithDictionary();
        var dataAfter = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        // Assert
        for (int i = 0; i < dataBefore.Count; i++)
        {
            var entBefore = dataBefore.ElementAt(i);
            var entAfter = dataAfter.ElementAt(i);
            Assert.Equal(entBefore.Value, entAfter.Value);
        }
    }

    [Fact]
    public async Task MmrContinueTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        var beforeReplays = testReplays1.Take(1).ToList();
        var afterReplays = testReplays1.Skip(1).Take(1).ToList();

        foreach (var replayDto in beforeReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();
        List<Replay> newReplays = new();
        foreach (var replayDto in afterReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }

        int countBefore = mmrService.ToonIdRatings.Count;
        await mmrService.ContinueCalculateWithDictionary(newReplays);
        int countAfter = mmrService.ToonIdRatings.Count;

        // Assert
        Assert.True(countBefore < countAfter);
    }

    [Fact]
    public async Task MmrContinueDateTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        var beforeReplays = testReplays1.Take(1).ToList();
        var afterFirstReplays = testReplays1.Skip(1).Take(1).ToList();
        var afterSecondReplays = testReplays1.Skip(2).Take(1).Select(s => s with { GameTime = new DateTime(2017, 1, 1) }).ToList();

        foreach (var replayDto in beforeReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();
        List<Replay> newReplays = new();

        foreach (var replayDto in afterFirstReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }

        await mmrService.ContinueCalculateWithDictionary(newReplays);

        newReplays = new();

        foreach (var replayDto in afterSecondReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }
        int countBefore = mmrService.ToonIdRatings.Count;

        await mmrService.ContinueCalculateWithDictionary(newReplays);
        int countAfter = mmrService.ToonIdRatings.Count;

        // Assert
        Assert.True(countBefore == countAfter);
    }

    [Fact]
    public async Task MmrContinueCompareTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        testReplays1 = testReplays1.OrderBy(o => o.GameTime).ToList();

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        var beforeReplays = testReplays1.Take(1).ToList();
        var afterReplays = testReplays1.Skip(1).Take(1).ToList();

        foreach (var replayDto in beforeReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();

        var dataBefore = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        List<Replay> newReplays = new();
        foreach (var replayDto in afterReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }

        await mmrService.ContinueCalculateWithDictionary(newReplays);

        var dataAfterContinue = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        int count = 0;
        int equal = 0;
        int newCount = 0;
        int different = 0;

        foreach (var ent in dataAfterContinue)
        {
            if (dataBefore.ContainsKey(ent.Key))
            {
                if (ent.Value == dataBefore[ent.Key])
                {
                    equal++;
                }
                else
                {
                    different++;
                }
            } else
            {
                newCount++;
            }
            count++;
        }

        await mmrService.ReCalculateWithDictionary();

        var dataAfterReRecalculate = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        // Assert
        for (int i = 0; i < dataAfterContinue.Count; i++)
        {
            var entBefore = dataAfterContinue.ElementAt(i);
            Assert.True(mmrService.ToonIdRatings.ContainsKey(entBefore.Key));
            var entAfter = mmrService.ToonIdRatings[entBefore.Key];
            Assert.Equal(entBefore.Value, entAfter.CmdrRatingStats.Mmr);
        }
    }

    [Fact]
    public async Task MmrContinueCompareMultipleTest()
    {
        var serviceProvider = GetTestServiceProvider();

        // GetRequiredServices
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        // PrepareData
        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));

        Assert.True(testReplays1?.Any());

        if (testReplays1 == null)
        {
            return;
        }

        testReplays1 = testReplays1.OrderBy(o => o.GameTime).ToList();

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        var beforeReplays = testReplays1.Take(20).ToList();
        var afterReplays = testReplays1.Skip(20).Take(7).ToList();

        foreach (var replayDto in beforeReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        // ProcessData
        await mmrService.ReCalculateWithDictionary();

        var dataBefore = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        List<Replay> newReplays = new();
        foreach (var replayDto in afterReplays)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }

        await mmrService.ContinueCalculateWithDictionary(newReplays);

        var dataAfterContinue = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        int count = 0;
        int equal = 0;
        int newCount = 0;
        int different = 0;

        foreach (var ent in dataAfterContinue)
        {
            if (dataBefore.ContainsKey(ent.Key))
            {
                if (ent.Value == dataBefore[ent.Key])
                {
                    equal++;
                }
                else
                {
                    different++;
                }
            }
            else
            {
                newCount++;
            }
            count++;
        }

        await mmrService.ReCalculateWithDictionary();

        var dataAfterReRecalculate = mmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        // Assert
        for (int i = 0; i < dataAfterContinue.Count; i++)
        {
            var entBefore = dataAfterContinue.ElementAt(i);
            Assert.True(mmrService.ToonIdRatings.ContainsKey(entBefore.Key));
            var entAfter = mmrService.ToonIdRatings[entBefore.Key];
            Assert.Equal(entBefore.Value, entAfter.CmdrRatingStats.Mmr);
        }
    }


    private ServiceProvider GetTestServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<MmrService>();
        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();

        return serviceCollection.BuildServiceProvider();
    }
}
