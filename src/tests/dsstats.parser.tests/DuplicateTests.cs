using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

[TestClass]
public sealed class DuplicateTests
{
    private const int CompatHashGameloop = 6_720;

    private static readonly string[] DuplicateReplayCandidates =
    [
        "testdata/Direct Strike (10060).SC2Replay",
        "testdata/Direct Strike (10096).SC2Replay",
        "testdata/Direct Strike (10124).SC2Replay",
        "testdata/Direct Strike (10143).SC2Replay",
        "testdata/Direct Strike TE (1904).SC2Replay",
        "testdata/Direct Strike TE (1910).SC2Replay",
    ];

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
    public async Task ReplayDtoCompatHashIdentifiesEmulatedLeaverReplayDuplicate()
    {
        (string replayPath, int leaverGameloop) = await GetLeaverReplayCandidate();
        Sc2Replay fullReplay = await GetReplay(replayPath);
        Sc2Replay emulatedLeaverReplay = await GetReplay(replayPath);
        DirectStrikeReplay fullParsedReplay = Sc2DirectStrikeParser.Parse(fullReplay);
        TrimTrackerEventsAfter(emulatedLeaverReplay, leaverGameloop);

        ReplayDto fullDto = DsstatsParser.ParseReplay(fullReplay);
        ReplayDto emulatedLeaverDto = DsstatsParser.ParseReplay(emulatedLeaverReplay);

        if (ToGameloop(fullParsedReplay.Duration) < CompatHashGameloop)
        {
            Assert.IsEmpty(fullDto.CompatHash);
            Assert.IsEmpty(emulatedLeaverDto.CompatHash);
            return;
        }

        if (fullParsedReplay.GameEndTime == TimeSpan.Zero)
        {
            Assert.IsNotEmpty(fullDto.CompatHash);
            Assert.IsEmpty(emulatedLeaverDto.CompatHash);
            return;
        }

        Assert.IsNotEmpty(fullDto.CompatHash);
        Assert.IsNotEmpty(emulatedLeaverDto.CompatHash);
        Assert.AreEqual(fullDto.CompatHash, emulatedLeaverDto.CompatHash);

        string[] fullPlayerCompatHashes = GetOrderedPlayerCompatHashes(fullDto);
        string[] emulatedLeaverPlayerCompatHashes = GetOrderedPlayerCompatHashes(emulatedLeaverDto);
        Assert.IsTrue(fullPlayerCompatHashes.All(static compatHash => !string.IsNullOrEmpty(compatHash)));
        Assert.IsTrue(emulatedLeaverPlayerCompatHashes.All(static compatHash => !string.IsNullOrEmpty(compatHash)));
        CollectionAssert.AreEqual(fullPlayerCompatHashes, emulatedLeaverPlayerCompatHashes);
    }

    private async Task<(string ReplayPath, int LeaverGameloop)> GetLeaverReplayCandidate()
    {
        foreach (string replayPath in DuplicateReplayCandidates)
        {
            Sc2Replay replay = await GetReplay(replayPath);
            DirectStrikeReplay parsedReplay = Sc2DirectStrikeParser.Parse(replay);
            int leaverGameloop = parsedReplay.Players
                .Where(static player => player.DurationGameloop > 0)
                .Select(static player => player.DurationGameloop)
                .DefaultIfEmpty()
                .Min();

            if (leaverGameloop > 0 && leaverGameloop < CompatHashGameloop)
            {
                return (replayPath, leaverGameloop);
            }
        }

        Assert.Fail("Expected at least one fixture with a player duration before the 5 minute compat hash mark.");
        return default;
    }

    private static int ToGameloop(TimeSpan time)
    {
        return (int)Math.Round(time.TotalSeconds * 22.4D);
    }

    private static string[] GetOrderedPlayerCompatHashes(ReplayDto dto)
    {
        return
        [
            .. dto.Players
                .OrderBy(static player => player.TeamId)
                .ThenBy(static player => player.GamePos)
                .ThenBy(static player => player.Player.ToonId.Region)
                .ThenBy(static player => player.Player.ToonId.Realm)
                .ThenBy(static player => player.Player.ToonId.Id)
                .ThenBy(static player => player.Player.PlayerId)
                .ThenBy(static player => player.Name, StringComparer.Ordinal)
                .Select(static player => player.CompatHash ?? string.Empty),
        ];
    }

    private static void TrimTrackerEventsAfter(Sc2Replay replay, int gameloop)
    {
        Assert.IsNotNull(replay.TrackerEvents);

        ICollection<TrackerEvent> events = replay.TrackerEvents.BaseTrackerEvents;
        List<TrackerEvent> trimmedEvents = new(events.Count);
        foreach (TrackerEvent trackerEvent in events)
        {
            if (trackerEvent.Gameloop <= gameloop || !IsLeaverSensitiveEvent(trackerEvent))
            {
                trimmedEvents.Add(trackerEvent);
            }
        }

        events.Clear();
        foreach (TrackerEvent trackerEvent in trimmedEvents)
        {
            events.Add(trackerEvent);
        }
    }

    private static bool IsLeaverSensitiveEvent(TrackerEvent trackerEvent) => trackerEvent is
        SPlayerStatsEvent or
        SUnitBornEvent or
        SUnitDiedEvent or
        SUnitOwnerChangeEvent or
        SUnitPositionsEvent or
        SUnitTypeChangeEvent or
        SUpgradeEvent or
        SUnitInitEvent or
        SUnitDoneEvent;

    private async Task<Sc2Replay> GetReplay(string replayPath)
    {
        return await replayDecoder.DecodeAsync(replayPath, replayDecoderOptions, CancellationToken.None);
    }
}
