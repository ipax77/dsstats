using pax.dsstats.parser;
using s2protocol.NET;
using System.Reflection;

namespace dsstats.maui.tests;

public class GameModeTests
{
    public static readonly string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (3553).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (1370).SC2Replay")]
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
}