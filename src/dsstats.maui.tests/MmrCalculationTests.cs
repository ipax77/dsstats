
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
        Assert.True(MmrService.ToonIdRatings.Any());
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
        var dataBefore = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);
        await mmrService.ReCalculateWithDictionary();
        var dataAfter = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

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

        int countBefore = MmrService.ToonIdRatings.Count;
        await mmrService.ContinueCalculateWithDictionary(newReplays);
        int countAfter = MmrService.ToonIdRatings.Count;

        // Assert
        Assert.True(countBefore < countAfter);
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

        await mmrService.ContinueCalculateWithDictionary(newReplays);

        var dataBefore = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);
        
        await mmrService.ReCalculateWithDictionary();

        // Assert
        for (int i = 0; i < dataBefore.Count; i++)
        {
            var entBefore = dataBefore.ElementAt(i);
            Assert.True(MmrService.ToonIdRatings.ContainsKey(entBefore.Key));
            var entAfter = MmrService.ToonIdRatings[entBefore.Key];
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
