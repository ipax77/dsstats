using pax.dsstats.parser;
using s2protocol.NET;
using System.Reflection;

namespace dsstats.maui.tests;

public class ParseTests
{
    public static readonly string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    [Theory]
    [InlineData("test1.SC2Replay")]
    [InlineData("test2.SC2Replay")]
    public async Task ParseReplayTest(string replayFile)
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

        var dsReplay = Parse.GetDsReplay(replay);

        Assert.NotNull(dsReplay);
        if (dsReplay == null)
        {
            decoder.Dispose();
            return;
        }

        var replayDto = Parse.GetReplayDto(dsReplay);

        var replayPlayerMaxDuration = replayDto.ReplayPlayers.Select(s => s.Duration).Max();
        var uploader = replayDto.ReplayPlayers.FirstOrDefault(f => f.Duration == replayPlayerMaxDuration);

        Assert.NotNull(uploader);
        if (uploader == null)
        {
            decoder.Dispose();
            return;
        }

        foreach (var replayPlayer in replayDto.ReplayPlayers.Where(x => x.Spawns.Any()))
        {
            Assert.NotNull(replayPlayer.Spawns.FirstOrDefault(f => f.Breakpoint == pax.dsstats.shared.Breakpoint.All));
        }
        decoder.Dispose();
    }

    //[Fact]
    [Theory]
    [InlineData("/data/ds/errorReplay/nosetupEvent/Direct_Strike_TE_3.SC2Replay")]
    [InlineData("/data/ds/errorReplay/nosetupEvent/Direct_Strike_TE_2.SC2Replay")]
    [InlineData("/data/ds/errorReplay/nosetupEvent/Direct_Strike_TE_4.SC2Replay")]
    [InlineData("/data/ds/errorReplay/nosetupEvent/Direct_Strike_TE_38.SC2Replay")]
    public async Task ParseTEMaps(string replayFile)
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

        // var replayFile = "/data/ds/errorReplay/nosetupEvent/Direct_Strike_TE_2.SC2Replay";

        var replay = await decoder.DecodeAsync(replayFile, options).ConfigureAwait(false);
        Assert.True(replay != null, "Sc2Replay was null");
        if (replay == null)
        {
            decoder.Dispose();
            return;
        }

        var dsReplay = Parse.GetDsReplay(replay);

        Assert.NotNull(dsReplay);
        if (dsReplay == null)
        {
            decoder.Dispose();
            return;
        }

        var replayDto = Parse.GetReplayDto(dsReplay);

        var replayPlayerMaxDuration = replayDto.ReplayPlayers.Select(s => s.Duration).Max();
        var uploader = replayDto.ReplayPlayers.FirstOrDefault(f => f.Duration == replayPlayerMaxDuration);

        Assert.NotNull(uploader);
        if (uploader == null)
        {
            decoder.Dispose();
            return;
        }

        foreach (var replayPlayer in replayDto.ReplayPlayers.Where(x => x.Spawns.Any()))
        {
            Assert.NotNull(replayPlayer.Spawns.FirstOrDefault(f => f.Breakpoint == pax.dsstats.shared.Breakpoint.All));
        }
        decoder.Dispose();
    }
}