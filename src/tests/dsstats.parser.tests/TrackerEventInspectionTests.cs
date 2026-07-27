using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

[TestClass]
public sealed class TrackerEventInspectionTests
{
    private readonly ReplayDecoder replayDecoder = new();
    private readonly ReplayDecoderOptions replayDecoderOptions = new()
    {
        Details = true,
        Initdata = false,
        Metadata = false,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Ignore("Diagnostic helper for inspecting gameloop-zero mode upgrade events across replay folders.")]
    public async Task InspectGameModeTrackerUpgradeEvents()
    {
        string replayDirectory = Environment.GetEnvironmentVariable("DIRECT_STRIKE_REPLAY_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "testdata");

        foreach (string replayPath in Directory.EnumerateFiles(replayDirectory, "*.SC2Replay", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            Sc2Replay replay = await replayDecoder.DecodeAsync(replayPath, replayDecoderOptions, CancellationToken.None)
                ?? throw new InvalidOperationException($"Could not decode replay '{replayPath}'.");

            DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);
            string[] modeUpgradeNames = [.. GetGameModeUpgradeNames(replay)];

            TestContext.WriteLine(
                "{0} | players={1} | gameMode={2} | upgrades={3}",
                Path.GetFileName(replayPath),
                replay.Details?.Players.Count ?? 0,
                dsReplay.GameMode,
                string.Join(", ", modeUpgradeNames));
        }
    }

    private static IEnumerable<string> GetGameModeUpgradeNames(Sc2Replay replay)
    {
        return (replay.TrackerEvents?.SUpgradeEvents ?? [])
            .Where(upgradeEvent => upgradeEvent.Gameloop == 0)
            .Select(GetUpgradeTypeName)
            .Where(IsGameModeUpgradeName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(upgradeTypeName => upgradeTypeName, StringComparer.Ordinal);
    }

    private static string GetUpgradeTypeName(SUpgradeEvent upgradeEvent)
    {
        return upgradeEvent.UpgradeTypeName;
    }

    private static bool IsGameModeUpgradeName(string upgradeTypeName)
    {
        return upgradeTypeName.StartsWith("GameMode", StringComparison.Ordinal)
            || upgradeTypeName.StartsWith("Mutation", StringComparison.Ordinal);
    }
}
