﻿
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

public class MmrContinueTests : TestWithSqlite
{
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    [Fact]
    public async Task DefaultMmrContinueTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<MmrService>();
        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));
        var testReplays2 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays2.json")));

        Assert.True(testReplays1?.Any());
        Assert.True(testReplays2?.Any());

        if (testReplays1 == null || testReplays2 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        foreach (var replayDto in testReplays1)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        await mmrService.ReCalculateWithDictionary();
        var dataVeryBefore = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        Assert.True(MmrService.ToonIdRatings.Any());

        List<Replay> newReplays = new();
        foreach (var replayDto in testReplays2)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
            break;
        }

        int countBefore = MmrService.ToonIdRatings.Count;
        await mmrService.ContinueCalculateWithDictionary(newReplays);

        Assert.True(MmrService.ToonIdRatings.Count > countBefore);

        var dataBefore = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        await mmrService.ReCalculateWithDictionary();

        var dataAfter = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        for (int i = 0; i < dataVeryBefore.Count; i++)
        {
            var entBefore = dataVeryBefore.ElementAt(i);
            var entAfter = dataAfter.ElementAt(i);

            Assert.Equal(entBefore.Value, entAfter.Value);
        }

        Assert.Equal(dataBefore.Count, dataAfter.Count);

        for (int i = 0; i < dataBefore.Count; i++)
        {
            var entBefore = dataBefore.ElementAt(i);
            var entAfter = dataAfter.ElementAt(i);

            Assert.Equal(entBefore.Value, entAfter.Value);
        }
    }

    [Fact]
    public async Task AdvancedMmrContinueTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<MmrService>();
        serviceCollection.AddTransient<IReplayRepository, ReplayRepository>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        await mmrService.SeedCommanderMmrs();

        var testReplays1 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays1.json")));
        var testReplays2 = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(Path.Combine(assemblyPath, "testdata", "testreplays2.json")));

        Assert.True(testReplays1?.Any());
        Assert.True(testReplays2?.Any());

        if (testReplays1 == null || testReplays2 == null)
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        foreach (var replayDto in testReplays1)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
        }

        await mmrService.ReCalculateWithDictionary();

        Assert.True(MmrService.ToonIdRatings.Any());

        List<Replay> newReplays = new();
        foreach (var replayDto in testReplays2)
        {
            (units, upgrades, var replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, null);
            newReplays.Add(replay);
        }

        int countBefore = MmrService.ToonIdRatings.Count;
        await mmrService.ContinueCalculateWithDictionary(newReplays);

        Assert.True(MmrService.ToonIdRatings.Count > countBefore);

        var dataBefore = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        await mmrService.ReCalculateWithDictionary();

        var dataAfter = MmrService.ToonIdRatings.ToDictionary(k => k.Key, v => v.Value.CmdrRatingStats.Mmr);

        Assert.Equal(dataBefore.Count, dataAfter.Count);

        for (int i = 0; i < dataBefore.Count; i++)
        {
            var entBefore = dataBefore.ElementAt(i);
            var entAfter = dataAfter.ElementAt(i);

            Assert.Equal(entBefore.Value, entAfter.Value);
        }
    }
}
