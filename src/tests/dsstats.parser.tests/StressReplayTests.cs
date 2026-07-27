using s2protocol.NET;
using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

[TestClass]
public sealed class StressReplayTests
{
    private readonly ReplayDecoder replayDecoder = new();
    private readonly ReplayDecoderOptions replayDecoderOptions = new()
    {
        Details = true,
        Initdata = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    [TestMethod]
    [TestCategory("Stress")]
    [RunStressReplay]
    [DataRow("testdata/Direct Strike (7586).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1046).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1106).SC2Replay")]
    public async Task StressReplayCanDecodeAndParse(string replayPath)
    {
        Sc2Replay replay = await replayDecoder.DecodeAsync(replayPath, replayDecoderOptions, CancellationToken.None)
            ?? throw new InvalidOperationException($"Could not decode replay '{replayPath}'.");

        Assert.IsNotNull(replay.Details);
        Assert.IsTrue(replay.Details.Title.StartsWith("Direct Strike", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(replay.TrackerEvents);
        Assert.IsGreaterThan(0, replay.TrackerEvents.SUnitBornEvents.Count);
        Assert.IsGreaterThan(0, replay.TrackerEvents.SPlayerStatsEvents.Count);

        DirectStrikeReplay directStrikeReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.IsGreaterThan(DateTime.MinValue, directStrikeReplay.GameTime);
        Assert.IsNotEmpty(directStrikeReplay.Players);
        Assert.IsTrue(directStrikeReplay.Players.All(player => !string.IsNullOrWhiteSpace(player.Name)));

        if (replay.Metadata is not null)
        {
            Assert.AreEqual(replay.Metadata.BaseBuild, directStrikeReplay.BaseBuild);
        }

        Assert.IsTrue(
            directStrikeReplay.Duration == directStrikeReplay.GameEndTime
                || directStrikeReplay.Duration == directStrikeReplay.Players.Select(player => player.Duration).DefaultIfEmpty().Max());
        Assert.IsTrue(
            directStrikeReplay.Players.Any(player => player.Stats.Count > 0 || player.Spawns.Count > 0)
                || directStrikeReplay.GameEndTime > TimeSpan.Zero
                || directStrikeReplay.CannonTime > TimeSpan.Zero
                || directStrikeReplay.BunkerTime > TimeSpan.Zero);
    }
}

internal sealed class RunStressReplayAttribute : ConditionBaseAttribute
{
    public RunStressReplayAttribute()
        : base(ConditionMode.Include)
    {
        IgnoreMessage = "Stress replay validation; set SC2DIRECTSTRIKE_RUN_STRESS_TESTS=1 to run before benchmarks.";
    }

    public override bool IsConditionMet
    {
        get
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("SC2DIRECTSTRIKE_RUN_STRESS_TESTS"),
                "1",
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public override string GroupName => nameof(RunStressReplayAttribute);
}
