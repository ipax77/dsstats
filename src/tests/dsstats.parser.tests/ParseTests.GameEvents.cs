using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;
using System.Reflection;

namespace Sc2DirectStrike.Tests;

public sealed partial class ParseTests
{
    private const string ScanReplayPath = "testdata/Direct Strike (1155).SC2Replay";
    private const string RaynorScanReplayPath = "testdata/Direct Strike (10897).SC2Replay";

    [TestMethod]
    public async Task CanDetectPlayerScanCounts()
    {
        Sc2Replay replay = await GetReplay(ScanReplayPath);
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);

        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            ["HotDog"] = 4,
            ["NoNsenSe"] = 4,
            ["CoughTots"] = 1,
            ["PAX"] = 10,
            ["Mahala"] = 3,
            ["BadMoon"] = 5,
        };

        Assert.HasCount(expected.Count, parsed.Players);
        foreach (DirectStrikePlayer player in parsed.Players)
        {
            Assert.IsTrue(expected.TryGetValue(player.Name, out int expectedScans), $"Unexpected player '{player.Name}'.");
            Assert.AreEqual(expectedScans, player.ScanCount, player.Name);
        }

        Assert.AreEqual(27, parsed.Players.Sum(player => player.ScanCount));
        Assert.AreEqual(false, parsed.ResumedFromReplay);

        ReplayDto dto = DirectStrikeReplayDtoMapper.Map(replay, parsed);
        Assert.AreEqual(false, dto.ResumedFromReplay);
        foreach (ReplayPlayerDto player in dto.Players)
        {
            Assert.AreEqual(expected[player.Name], player.ScanCount, player.Name);
        }
    }

    [TestMethod]
    public async Task GameEventInformationIsUnknownWhenStreamWasNotDecoded()
    {
        ReplayDecoderOptions options = DirectStrikeDecoderOptions.Create();
        options.GameEvents = false;

        Sc2Replay replay = await GetReplay(ScanReplayPath, options);
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);

        Assert.IsNull(parsed.ResumedFromReplay);
        Assert.IsTrue(parsed.Players.All(player => player.ScanCount is null));
    }

    [TestMethod]
    public async Task CanDetectRaynorFreeScans()
    {
        Sc2Replay replay = await GetReplay(RaynorScanReplayPath);
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);

        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            ["Bear"] = 2,
            ["Shaddiee"] = 1,
            ["whaply"] = 0,
            ["PAX"] = 45,
            ["Flynnguin"] = 16,
            ["Gonduin"] = 0,
        };

        Assert.HasCount(expected.Count, parsed.Players);
        foreach (DirectStrikePlayer player in parsed.Players)
        {
            Assert.AreEqual(expected[player.Name], player.ScanCount, player.Name);
        }

        DirectStrikePlayer pax = parsed.Players.Single(player => player.Name == "PAX");
        Assert.AreEqual("Base97563", parsed.BaseBuild);
        Assert.AreEqual(Commander.Raynor, pax.Commander);
        Assert.AreEqual(4, pax.GamePos);
        Assert.AreEqual(64, parsed.Players.Sum(player => player.ScanCount));

        Assert.IsNotNull(replay.GameEvents);
        SCmdEvent[] raynorCommands = replay.GameEvents.BaseGameEvents
            .OfType<SCmdEvent>()
            .Where(command => command.UserId == 3
                && command.AbilLink == 1142
                && command.AbilCmdIndex == 0)
            .ToArray();
        Assert.HasCount(46, raynorCommands);

        SCmdEvent setupCommand = raynorCommands.Single(command => command.Gameloop == 109);
        Assert.IsNull(setupCommand.TargetX);
        Assert.IsNull(setupCommand.TargetY);

        int[] expectedGameloops =
        [
            6419, 7341, 7965, 9039, 9926, 10293, 11504, 12271, 12626, 12933,
            13572, 14584, 15230, 15832, 16134, 16850, 17928, 18191, 19537,
            20276, 21020, 22308, 23556, 24227, 24793, 26478, 27185, 30316,
            31659, 32334, 32731, 33294, 34038, 34231, 34944, 36215, 36636,
            37137, 37997, 39271, 40097, 40477, 42219, 44060, 44551,
        ];
        CollectionAssert.AreEqual(
            expectedGameloops,
            raynorCommands
                .Where(command => command.TargetX.HasValue && command.TargetY.HasValue)
                .Select(command => command.Gameloop)
                .ToArray());

        ReplayDto dto = DirectStrikeReplayDtoMapper.Map(replay, parsed);
        foreach (ReplayPlayerDto player in dto.Players)
        {
            Assert.AreEqual(expected[player.Name], player.ScanCount, player.Name);
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ScanMinimumBuildIsInclusiveAndForwardCompatible()
    {
        Sc2Replay replay = await GetReplay(ScanReplayPath);

        DirectStrikeReplay atMinimum = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, 97425));
        Assert.AreEqual(27, atMinimum.Players.Sum(player => player.ScanCount));

        DirectStrikeReplay newerBuild = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, 100000));
        Assert.AreEqual(27, newerBuild.Players.Sum(player => player.ScanCount));

        DirectStrikeReplay beforeMinimum = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, 97424));
        Assert.IsTrue(beforeMinimum.Players.All(player => player.ScanCount is null));

        Sc2Replay missingDataBuildReplay = WithDataBuild(replay, 0);
        PropertyInfo metadataProperty = typeof(Sc2Replay).GetProperty(nameof(Sc2Replay.Metadata))
            ?? throw new InvalidOperationException("Could not find Sc2Replay.Metadata.");
        metadataProperty.SetValue(missingDataBuildReplay, null);
        DirectStrikeReplay missingBuild = Sc2DirectStrikeParser.Parse(missingDataBuildReplay);
        Assert.IsTrue(missingBuild.Players.All(player => player.ScanCount is null));
    }

    [TestMethod]
    public async Task CanDetectExplicitReplayHijack()
    {
        ReplayDecoderOptions options = DirectStrikeDecoderOptions.Create();
        options.GameEvents = false;
        Sc2Replay replay = await GetReplay(ScanReplayPath, options);
        GameEvents gameEvents = new([new SHijackReplayGameEvent(0, 43, 0, 1_000, [], 0)]);
        PropertyInfo property = typeof(Sc2Replay).GetProperty(nameof(Sc2Replay.GameEvents))
            ?? throw new InvalidOperationException("Could not find Sc2Replay.GameEvents.");
        property.SetValue(replay, gameEvents);

        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);

        Assert.AreEqual(true, parsed.ResumedFromReplay);
    }
}
