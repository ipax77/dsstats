using pax.dsstats.parser;
using s2protocol.NET;
using System.Reflection;

namespace dsstats.maui.tests;

public class GameModeTests
{
    public static readonly string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\1-S2-1-10188255\\Replays\\Multiplayer\\Direct Strike (206).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\1-S2-1-10188255\\Replays\\Multiplayer\\Direct Strike TE (14).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\1-S2-1-10188255\\Replays\\Multiplayer\\Direct Strike TE (13).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\1-S2-1-10188255\\Replays\\Multiplayer\\Direct Strike TE (12).SC2Replay")]
    public async Task GameModeCommandersTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.Commanders);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (5190).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (3553).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (1370).SC2Replay")]
    public async Task GameModeStandardTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.Standard);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (763).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (32).SC2Replay")]
    [InlineData("C:\\data\\ds\\errorReplay\\gamemode\\Direct Strike_149.SC2Replay")]
    [InlineData("C:\\data\\ds\\errorReplay\\gamemode\\Direct Strike_1902.SC2Replay")]
    public async Task GameModeHeroicTest(string replayFile)
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

        Assert.Equal(replayDto?.GameMode, pax.dsstats.shared.GameMode.CommandersHeroic);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (5057).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (1782).SC2Replay")]
    public async Task GameModeBrawlCmdrTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.BrawlCommanders);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (1312).SC2Replay")]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (4547).SC2Replay")]
    public async Task GameModeBrawlStdTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.BrawlStandard);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (5191).SC2Replay")]
    public async Task GameModeGearTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.Gear);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (5192).SC2Replay")]
    public async Task GameModeSwitchTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.Switch);
    }

    [Theory]
    [InlineData("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer\\Direct Strike (5193).SC2Replay")]
    public async Task GameModeSabotageTest(string replayFile)
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

        Assert.True(replayDto.GameMode == pax.dsstats.shared.GameMode.Sabotage);
    }
}