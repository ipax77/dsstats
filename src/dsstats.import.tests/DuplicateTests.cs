using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dsstats.import.tests;

public class DuplicateTests : TestWithSqlite
{
    [Fact]
    public async Task BasicDuplicateTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }
        await importService.DEBUGSeedUploaders();

        Assert.True(context.Uploaders.Count() > 2);

        var testReplayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText("/data/ds/testdata/replayDto2.json"));
        Assert.NotNull(testReplayDtos);

        if (testReplayDtos == null)
        {
            return;
        }

        var uploader1 = context.Uploaders.OrderBy(o => o.UploaderId).First();
        var uploader2 = context.Uploaders.OrderBy(o => o.UploaderId).Last();

        var testReplayDto = testReplayDtos.First();
        testReplayDtos.Add(testReplayDto with { Duration = testReplayDto.Duration - 1 });


        var testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        testReplays.First().UploaderId = uploader1.UploaderId;
        testReplays.Last().UploaderId = uploader2.UploaderId;

        await importService.ImportReplays(testReplays, new());

        Assert.True(context.Replays.Any());

        uploader1 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .First();
        uploader2 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .Last();

        Assert.Equal(1, uploader1.Replays.Count);
        Assert.Equal(1, uploader2.Replays.Count);

        testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        await importService.ImportReplays(testReplays, new());

        uploader1 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .First();
        uploader2 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .Last();

        Assert.Equal(1, uploader1.Replays.Count);
        Assert.Equal(1, uploader2.Replays.Count);
    }

    [Fact]
    public async Task BasicDuplicate2Test()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }
        await importService.DEBUGSeedUploaders();

        string testFile = Startup.GetTestFilePath("uploadtest2.base64");

        var countBefore = await context.Replays.CountAsync();

        Assert.True(File.Exists(testFile));
        var base64String = File.ReadAllText(testFile);

        var replayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(await ImportService.UnzipAsync(base64String));
        Assert.True(replayDtos?.Any());

        if (replayDtos == null)
        {
            return;
        }

        List<Replay> replays = new();

        for (int i = 1; i <= 6; i++)
        {
            var uploaderReplays = replayDtos.Select(s => mapper.Map<Replay>(s)).ToList();
            uploaderReplays.ForEach(f => f.UploaderId = i);
            replays.AddRange(uploaderReplays);
        }

        await importService.ImportReplays(replays, new());

        var countAfter = await context.Replays.CountAsync();

        Assert.Equal(countBefore + 1, countAfter);

        var replay = await context.Replays
            .Include(i => i.Uploaders)
            .OrderBy(o => o.ReplayId)
            .LastOrDefaultAsync();

        Assert.Equal(6, replay?.Uploaders.Count);
    }

    [Fact]
    public async Task DuplicateWithUploadersTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }
        await importService.DEBUGSeedUploaders();

        Assert.True(context.Uploaders.Count() > 2);

        var testReplayDtos = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText("/data/ds/testdata/replayDto2.json"));
        Assert.NotNull(testReplayDtos);

        if (testReplayDtos == null)
        {
            return;
        }

        var uploader1 = context.Uploaders.OrderBy(o => o.UploaderId).First();
        var uploader2 = context.Uploaders.OrderBy(o => o.UploaderId).Last();

        var testReplayDto = testReplayDtos.First();
        testReplayDtos.Add(testReplayDto with { Duration = testReplayDto.Duration - 1 });


        var testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        testReplays.First().UploaderId = uploader1.UploaderId;
        testReplays.Last().UploaderId = uploader2.UploaderId;

        await importService.ImportReplays(testReplays, new());

        Assert.True(context.Replays.Any());

        uploader1 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .First();
        uploader2 = context.Uploaders
            .Include(i => i.Replays)
            .OrderBy(o => o.UploaderId)
            .Last();

        Assert.Equal(1, uploader1.Replays.Count);
        Assert.Equal(1, uploader2.Replays.Count);

        testReplays = testReplayDtos.Select(s => mapper.Map<Replay>(s)).ToList();

        testReplays.First().UploaderId = context.Uploaders.OrderBy(o => o.UploaderId).Skip(1).First().UploaderId;
        testReplays.Last().UploaderId = context.Uploaders.OrderBy(o => o.UploaderId).Skip(2).First().UploaderId;
        await importService.ImportReplays(testReplays, new());

        var replay = context.Replays
            .Include(i => i.Uploaders)
            .FirstOrDefault();

        Assert.Equal(4, replay?.Uploaders.Count);
    }

    [Fact]
    public async Task SetUploaderWithLeaverTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDbContext<ReplayContext>(options => options.UseSqlite(_connection),
        ServiceLifetime.Transient);

        serviceCollection.AddAutoMapper(typeof(AutoMapperProfile));
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<ImportService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetService<ImportService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        Assert.NotNull(importService);
        if (importService == null)
        {
            return;
        }
        await importService.DEBUGSeedUploaders();

        var countBefore = await context.Replays.CountAsync();

        // string testFile = "/data/ds/uploadtest3.json";
        string testFile = Startup.GetTestFilePath("replayDto1.json");

        Assert.True(File.Exists(testFile));
        ReplayDto? replayDto = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(testFile))?.FirstOrDefault();
        Assert.NotNull(replayDto);
        if (replayDto == null)
        {
            return;
        }

        List<Replay> replays = new();
        replayDto.ReplayPlayers.ToList().ForEach(f => f.IsUploader = false);

        replayDto.ReplayPlayers.ElementAt(0).IsUploader = true;
        var leaverDto = replayDto with { Duration = replayDto.Duration - 61 };

        replays.Add(mapper.Map<Replay>(leaverDto));
        replays.Last().UploaderId = 1;

        replayDto.ReplayPlayers.ElementAt(0).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(1).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayDto));
        replays.Last().UploaderId = 2;

        replayDto.ReplayPlayers.ElementAt(1).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(2).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayDto));
        replays.Last().UploaderId = 3;

        replayDto.ReplayPlayers.ElementAt(2).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(3).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayDto));
        replays.Last().UploaderId = 4;

        replayDto.ReplayPlayers.ElementAt(3).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(4).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayDto));
        replays.Last().UploaderId = 5;

        replayDto.ReplayPlayers.ElementAt(4).IsUploader = false;
        replayDto.ReplayPlayers.ElementAt(5).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayDto));
        replays.Last().UploaderId = 6;


        var report = await importService.ImportReplays(replays, new());

        var countAfter = await context.Replays.CountAsync();
        Assert.Equal(countBefore, countAfter - 1);

        var dbReplay = await context.Replays
            .Include(i => i.Uploaders)
            .Include(i => i.ReplayPlayers)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.NotNull(dbReplay);
        Assert.Equal(6, dbReplay?.ReplayPlayers.Count(c => c.IsUploader));
        Assert.Equal(6, dbReplay?.Uploaders.Count);
    }
}
