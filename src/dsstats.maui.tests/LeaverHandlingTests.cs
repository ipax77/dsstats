using AutoMapper;
using dsstats.mmr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.parser;
using pax.dsstats.shared;
using s2protocol.NET;
using System.Reflection;

namespace dsstats.maui.tests;

public class LeaverHandlingTests : TestWithSqlite
{
    public static readonly string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    [Theory]
    [InlineData("C:\\Users\\Zehnder\\Documents\\StarCraft II\\Accounts\\450391902\\3-S2-1-7276247\\Replays\\Multiplayer\\Direct Strike (155).SC2Replay")] // 1 Leaver Only (1, 0)
    [InlineData("C:\\Users\\Zehnder\\Documents\\StarCraft II\\Accounts\\450391902\\2-S2-1-8509078\\Replays\\Multiplayer\\Direct Strike (1826).SC2Replay")] // 1 Leaver each Team (1, 1)
    [InlineData("C:\\Users\\Zehnder\\Documents\\StarCraft II\\Accounts\\450391902\\3-S2-1-7276247\\Replays\\Multiplayer\\Direct Strike (159).SC2Replay")] // 2 Leaver same Team (2, 0)
    [InlineData("C:\\Users\\Zehnder\\Documents\\StarCraft II\\Accounts\\450391902\\2-S2-1-8509078\\Replays\\Multiplayer\\Direct Strike (1592).SC2Replay")] // 4 Leaver (2, 2)
    public async Task OnLeaverTest(string replayFile)
    {
        Assert.True(assemblyPath != null, "Could not get ExecutingAssembly path");
        if (assemblyPath == null)
        {
            return;
        }

        ReplayDecoder decoder = new(assemblyPath);
        ReplayDecoderOptions options = new ReplayDecoderOptions()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false,
        };

        var replay = await decoder.DecodeAsync(Path.Combine(assemblyPath, "testdata", replayFile), options).ConfigureAwait(false);
        Assert.True(replay != null, "Sc2Replay was null");
        if (replay == null)
        {
            decoder.Dispose();
            return;
        }

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapperProfile());
        });
        var mapper = mapperConfiguration.CreateMapper();

        var dsReplay = Parse.GetDsReplay(replay);
        Assert.NotNull(dsReplay);
        if (dsReplay == null)
        {
            decoder.Dispose();
            return;
        }

        var replayDto = Parse.GetReplayDto(dsReplay);
        var replayDsrDto = mapper.Map<ReplayDsRDto>(replayDto);

        //mocking
        //var mockReplay = replayDsrDto with { };
        //mockReplay.ReplayPlayers.ForEach(f => f = f with { Duration = replayDto.Duration });

        var mmrIdRatings = new Dictionary<RatingType, Dictionary<int, CalcRating>>()
        {
            { RatingType.Cmdr, new() },
            { RatingType.Std, new() },
        };
        
        var results = await MmrService.GeneratePlayerRatings(new List<ReplayDsRDto>() { replayDsrDto }, new(), mmrIdRatings, new MmrOptions(true), 0, null, true);

        Assert.True(true);
    }
}