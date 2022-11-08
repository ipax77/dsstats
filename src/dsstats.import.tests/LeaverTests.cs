using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dsstats.import.tests;

public class LeaverTests : TestWithSqlite
{
    [Fact]
    public async Task LeaverTest()
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

        var countBefore = await context.Replays.CountAsync();

        string testFile = Startup.GetTestFilePath("replayDto2.json");

        Assert.True(File.Exists(testFile));
        ReplayDto? replayDto = JsonSerializer.Deserialize<List<ReplayDto>>(File.ReadAllText(testFile))?.FirstOrDefault();
        Assert.NotNull(replayDto);
        if (replayDto == null)
        {
            return;
        }

        List<Replay> replays = new();

        (var leaverReplay, var leaverPlayer) = GetLeaverReplay(replayDto, replayDto.ReplayPlayers.ElementAt(0));

        leaverReplay.ReplayPlayers.ToList().ForEach(f => f.IsUploader = false);
        leaverPlayer.IsUploader = true;

        replays.Add(mapper.Map<Replay>(leaverReplay));
        replays.Last().UploaderId = 10;

        var replayWithLeaver = GetReplayDtoWithLeaverPlayer(replayDto, leaverPlayer);
        replayWithLeaver.ReplayPlayers.ElementAt(5).IsUploader = false;
        replayWithLeaver.ReplayPlayers.ElementAt(0).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayWithLeaver));
        replays.Last().UploaderId = 11;

        replayWithLeaver.ReplayPlayers.ElementAt(0).IsUploader = false;
        replayWithLeaver.ReplayPlayers.ElementAt(1).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayWithLeaver));
        replays.Last().UploaderId = 12;

        replayWithLeaver.ReplayPlayers.ElementAt(1).IsUploader = false;
        replayWithLeaver.ReplayPlayers.ElementAt(2).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayWithLeaver));
        replays.Last().UploaderId = 13;

        replayWithLeaver.ReplayPlayers.ElementAt(2).IsUploader = false;
        replayWithLeaver.ReplayPlayers.ElementAt(3).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayWithLeaver));
        replays.Last().UploaderId = 14;

        replayWithLeaver.ReplayPlayers.ElementAt(3).IsUploader = false;
        replayWithLeaver.ReplayPlayers.ElementAt(4).IsUploader = true;
        replays.Add(mapper.Map<Replay>(replayWithLeaver));
        replays.Last().UploaderId = 15;

        await importService.ImportReplays(replays, new());

        var countAfter = await context.Replays.CountAsync();
        Assert.Equal(countBefore + 1, countAfter);

        var dbReplay = await context.Replays
            .Include(i => i.ReplayPlayers)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.NotNull(dbReplay);
        Assert.Equal(6, dbReplay?.ReplayPlayers.Count(c => c.IsUploader));
    }

    private static ReplayDto GetReplayDtoWithLeaverPlayer(ReplayDto replayDto, ReplayPlayerDto leaverPlayer)
    {
        var currentPlayer = replayDto.ReplayPlayers.First(f => f.GamePos == leaverPlayer.GamePos);
        replayDto.ReplayPlayers.Remove(currentPlayer);
        replayDto.ReplayPlayers.Add(leaverPlayer);
        return replayDto;
    }

    private static (ReplayDto, ReplayPlayerDto) GetLeaverReplay(ReplayDto replayDto, ReplayPlayerDto leaver, int mod = 2)
    {
        DateTime newGameTime = replayDto.GameTime.AddSeconds(-replayDto.Duration / mod);
        int newDuration = replayDto.Duration / mod;

        List<ReplayPlayerDto> replayPlayers = new();
        for (int i = 0; i < replayDto.ReplayPlayers.Count; i++)
        {
            var player = replayDto.ReplayPlayers.ElementAt(i);
            replayPlayers.Add(player with
            {
                Income = player.Income / mod,
                Army = player.Army / mod,
                Kills = player.Kills / mod,
                UpgradesSpent = player.UpgradesSpent / mod,
                Upgrades = player.Upgrades.Take(player.Upgrades.Count / mod).ToList(),
                Spawns = player.Spawns,
                Duration = newDuration,
                PlayerResult = PlayerResult.None
            });
        }

        var newMiddle = String.Join('|', replayDto.Middle.Split('|').Chunk(2).First());

        var leaverReplay = replayDto with
        {
            GameTime = newGameTime,
            Duration = newDuration,
            ReplayPlayers = replayPlayers,
            WinnerTeam = 0,
            Minkillsum = replayPlayers.Select(s => s.Kills).Min(),
            Maxkillsum = replayPlayers.Select(s => s.Kills).Max(),
            Minarmy = replayPlayers.Select(s => s.Army).Min(),
            Minincome = replayPlayers.Select(s => s.Income).Min(),
            Maxleaver = newDuration - replayPlayers.Select(s => s.Duration).Min(),
            Middle = newMiddle
        };

        leaverReplay.ReplayHash = Data.GenHash(leaverReplay);

        return (leaverReplay, leaverReplay.ReplayPlayers.First(f => f.GamePos == leaver.GamePos));
    }
}
