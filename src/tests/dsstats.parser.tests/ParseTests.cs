using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

[TestClass]
public sealed partial class ParseTests
{
    private readonly ReplayDecoder replayDecoder = new();
    private readonly ReplayDecoderOptions replayDecoderOptions = DirectStrikeDecoderOptions.Create();

    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay")]
    [DataRow("testdata/Direct Strike (10096).SC2Replay")]
    [DataRow("testdata/Direct Strike (10124).SC2Replay")]
    [DataRow("testdata/Direct Strike (10143).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task CanSetGameTime(string replayName)
    {
        var replay = await GetReplay(replayName);

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);
        Assert.IsNotNull(dsReplay);

        Assert.IsGreaterThan(DateTime.MinValue, dsReplay.GameTime);
        Assert.IsNotEmpty(dsReplay.Players);
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay")]
    [DataRow("testdata/Direct Strike (10096).SC2Replay")]
    [DataRow("testdata/Direct Strike (10124).SC2Replay")]
    [DataRow("testdata/Direct Strike (10143).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task CanSetReplayMetadata(string replayName)
    {
        var replay = await GetReplay(replayName);

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.IsNotNull(replay.Metadata);
        Assert.AreEqual(replay.Metadata.BaseBuild, dsReplay.BaseBuild);
        Assert.IsNotEmpty(dsReplay.BaseBuild);
        Assert.AreEqual(GetExpectedReplayDuration(replay, dsReplay), dsReplay.Duration);
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay", GameMode.Standard)]
    [DataRow("testdata/Direct Strike (10096).SC2Replay", GameMode.BrawlCommanders)]
    [DataRow("testdata/Direct Strike (10124).SC2Replay", GameMode.Standard)]
    [DataRow("testdata/Direct Strike (10143).SC2Replay", GameMode.Standard)]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay", GameMode.Standard)]
    public async Task CanSetGameMode(string replayName, GameMode gameMode)
    {
        var replay = await GetReplay(replayName);

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.AreEqual(gameMode, dsReplay.GameMode);
    }

    [TestMethod]
    public async Task CanSetPlayerMetadata()
    {
        var replay = await GetReplay("testdata/Direct Strike (10060).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);
        var firstPlayer = dsReplay.Players[0];
        var fifthPlayer = dsReplay.Players[4];

        Assert.AreEqual(20D, firstPlayer.APM);
        Assert.AreEqual(PlayerResult.Win, firstPlayer.Result);
        Assert.AreEqual(Race.Random, firstPlayer.SelectedRace);

        Assert.AreEqual(5D, fifthPlayer.APM);
        Assert.AreEqual(PlayerResult.Loss, fifthPlayer.Result);
        Assert.AreEqual(Race.Terran, fifthPlayer.SelectedRace);
    }

    [TestMethod]
    public async Task CanMapTeMetadataByPlayerIdAsListIndex()
    {
        var replay = await GetReplay("testdata/Direct Strike TE (1910).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);
        var playerWithSkippedSlotId = dsReplay.Players[3];

        Assert.AreEqual(5, playerWithSkippedSlotId.SlotId);
        Assert.AreEqual(62D, playerWithSkippedSlotId.APM);
        Assert.AreEqual(PlayerResult.Undecided, playerWithSkippedSlotId.Result);
        Assert.AreEqual(Race.Terran, playerWithSkippedSlotId.SelectedRace);
    }

    [TestMethod]
    public async Task CanSetTeObserversFromInitdata()
    {
        var replay = await GetReplay("testdata/Direct Strike TE (1904).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.HasCount(6, dsReplay.Players);
        Assert.HasCount(2, dsReplay.Observers);
        Assert.IsFalse(dsReplay.Players.Any(player => player.Name is "Apache" or "Nova"));

        DirectStrikeObserver apache = AssertObserver(dsReplay, "Apache");
        Assert.AreEqual("DIRSTK", apache.Clan);
        Assert.AreEqual(5, apache.SlotId);
        Assert.AreEqual(2, apache.Region);
        Assert.AreEqual(1, apache.Realm);
        Assert.AreEqual(8641442, apache.Id);

        DirectStrikeObserver nova = AssertObserver(dsReplay, "Nova");
        Assert.AreEqual("DIRSTK", nova.Clan);
        Assert.AreEqual(0, nova.SlotId);
        Assert.AreEqual(2, nova.Region);
        Assert.AreEqual(1, nova.Realm);
        Assert.AreEqual(9493659, nova.Id);
    }

    [TestMethod]
    public async Task CanSetStandardCommandersFromEarlyWorkerEvents()
    {
        var replay = await GetReplay("testdata/Direct Strike TE (1904).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        CollectionAssert.AreEqual(
            new[] { Commander.Protoss, Commander.Protoss, Commander.Protoss, Commander.Terran, Commander.Terran, Commander.Terran },
            dsReplay.Players.Select(player => player.Commander).ToArray());
    }

    [TestMethod]
    public async Task CanSetCommanderModeCommandersFromEarlyWorkerEvents()
    {
        var replay = await GetReplay("testdata/Direct Strike (10096).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        CollectionAssert.AreEqual(
            new[] { Commander.Dehaka, Commander.Tychus, Commander.Raynor, Commander.Stukov, Commander.Fenix, Commander.Vorazun },
            dsReplay.Players.Select(player => player.Commander).ToArray());
    }

    [TestMethod]
    public async Task CanPreferSpecificCommandersOverGenericRaceWorkers()
    {
        var replay = await GetReplay("testdata/Direct Strike (10147).SC2Replay");

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        CollectionAssert.AreEqual(
            new[] { Commander.Swann, Commander.Fenix, Commander.Swann, Commander.Dehaka, Commander.Dehaka, Commander.Fenix },
            dsReplay.Players
                .OrderBy(player => player.GamePos)
                .Select(player => player.Commander)
                .ToArray());
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike TE (1904).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task CanMapTeInitdataMetadataAndTrackerCommandersByControlPlayerId(string replayName)
    {
        Sc2Replay replay = await GetReplay(replayName);

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.IsNotNull(replay.Initdata);
        Assert.IsNotNull(replay.Metadata);
        Assert.IsNotNull(replay.TrackerEvents);

        var playersByToon = dsReplay.Players.ToDictionary(player => (player.Region, player.Realm, player.Id));
        var metadataPlayersByPlayerId = replay.Metadata.Players.ToDictionary(player => player.PlayerID);
        var firstWorkerByControlPlayerId = replay.TrackerEvents.SUnitBornEvents
            .Where(unitBornEvent => unitBornEvent.Gameloop <= 1440 && TryParseWorkerCommander(unitBornEvent.UnitTypeName, out _))
            .GroupBy(unitBornEvent => unitBornEvent.ControlPlayerId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (SPlayerSetupEvent setupEvent in replay.TrackerEvents.SPlayerSetupEvents)
        {
            Slot? slot = replay.Initdata.LobbyState.Slots.SingleOrDefault(slot => slot.UserId == setupEvent.UserId);
            Assert.IsNotNull(slot);
            Assert.IsTrue(TryParseToonHandle(slot.ToonHandle, out int region, out int realm, out int id));
            Assert.IsTrue(playersByToon.TryGetValue((region, realm, id), out DirectStrikePlayer? player));

            Assert.IsTrue(metadataPlayersByPlayerId.TryGetValue(setupEvent.PlayerId, out MetadataPlayer? metadataPlayer));
            Assert.IsTrue(firstWorkerByControlPlayerId.TryGetValue(setupEvent.PlayerId, out SUnitBornEvent? workerEvent));
            Assert.IsTrue(TryParseAssignedRaceCommander(metadataPlayer.AssignedRace, out Commander metadataCommander));
            Assert.IsTrue(TryParseWorkerCommander(workerEvent.UnitTypeName, out Commander trackerCommander));

            Assert.AreEqual(metadataCommander, player.Commander);
            Assert.AreEqual(metadataCommander, trackerCommander);
        }
    }

    [TestMethod]
    public async Task CanParseReplayWithoutMetadata()
    {
        ReplayDecoderOptions options = new()
        {
            Details = true,
            Initdata = true,
            Metadata = false,
            GameEvents = false,
            MessageEvents = false,
            TrackerEvents = true,
            AttributeEvents = false,
        };
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10060).SC2Replay", options);

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.AreEqual(string.Empty, dsReplay.BaseBuild);
        Assert.AreEqual(GetExpectedReplayDuration(replay, dsReplay), dsReplay.Duration);
        Assert.AreEqual(PlayerResult.Win, dsReplay.Players[0].Result);
        Assert.AreEqual(Race.None, dsReplay.Players[0].SelectedRace);
        Assert.IsEmpty(dsReplay.Observers);
    }

    [TestMethod]
    public async Task CanParseReplayWithoutTrackerEvents()
    {
        ReplayDecoderOptions options = new()
        {
            Details = true,
            Initdata = true,
            Metadata = true,
            GameEvents = false,
            MessageEvents = false,
            TrackerEvents = false,
            AttributeEvents = false,
        };
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10060).SC2Replay", options);

        var dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.AreEqual(GameMode.None, dsReplay.GameMode);
        Assert.AreEqual(0, dsReplay.WinnerTeam);
        Assert.AreEqual(TimeSpan.Zero, dsReplay.Duration);
        Assert.AreEqual(TimeSpan.Zero, dsReplay.GameEndTime);
        Assert.AreEqual(TimeSpan.Zero, dsReplay.CannonTime);
        Assert.AreEqual(TimeSpan.Zero, dsReplay.BunkerTime);
        Assert.AreEqual(0, dsReplay.FirstMiddleControlTeam);
        Assert.IsEmpty(dsReplay.MiddleChanges);
        Assert.IsTrue(dsReplay.Players.All(player => player.GamePos == 0));
        Assert.IsTrue(dsReplay.Players.All(player => player.TeamId == 0));
        Assert.IsTrue(dsReplay.Players.All(player => player.RefineryTimes.Length == 0));
        Assert.IsTrue(dsReplay.Players.All(player => player.TierUpgrades.Length == 0));
        Assert.IsTrue(dsReplay.Players.All(player => player.Upgrades.Count == 0));
        Assert.IsTrue(dsReplay.Players.All(player => player.Spawns.Count == 0));
        CollectionAssert.AreEqual(
            new[] { Commander.Zerg, Commander.Protoss, Commander.Protoss, Commander.Zerg, Commander.Terran, Commander.Zerg },
            dsReplay.Players.Select(player => player.Commander).ToArray());
    }

    [TestMethod]
    public void CanClassifyGameModeFallbacks()
    {
        Assert.AreEqual(GameMode.Tutorial, InvokeGetGameMode([], 1));
        Assert.AreEqual(GameMode.None, InvokeGetGameMode([], 6));
        Assert.AreEqual(GameMode.None, InvokeGetGameMode(["GameModeMystery"], 6));
        Assert.AreEqual(GameMode.BrawlStandard, InvokeGetGameMode(["GameModeBrawl", "GameModeStandard"], 6));
        Assert.AreEqual(GameMode.BrawlCommanders, InvokeGetGameMode(["GameModeBrawl", "MutationCommanders"], 6));
    }

    private static GameMode InvokeGetGameMode(string[] modes, int playerCount)
    {
        var method = typeof(Sc2DirectStrikeParser).GetMethod("GetGameMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(method);

        object? result = method.Invoke(null, [new HashSet<string>(modes, StringComparer.Ordinal), playerCount]);
        Assert.IsNotNull(result);

        return (GameMode)result;
    }

    private static DirectStrikeObserver AssertObserver(DirectStrikeReplay replay, string name)
    {
        DirectStrikeObserver? observer = replay.Observers.SingleOrDefault(observer => observer.Name == name);
        Assert.IsNotNull(observer);

        return observer;
    }

    private static bool TryParseAssignedRaceCommander(string assignedRace, out Commander commander)
    {
        commander = assignedRace switch
        {
            "Prot" => Commander.Protoss,
            "Terr" => Commander.Terran,
            "Zerg" => Commander.Zerg,
            _ => Commander.None,
        };

        return commander != Commander.None;
    }

    private static bool TryParseWorkerCommander(string unitTypeName, out Commander commander)
    {
        const string workerPrefix = "Worker";

        commander = Commander.None;
        if (!unitTypeName.StartsWith(workerPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string commanderName = unitTypeName[workerPrefix.Length..];
        commander = commanderName switch
        {
            nameof(Commander.Protoss) => Commander.Protoss,
            nameof(Commander.Terran) => Commander.Terran,
            nameof(Commander.Zerg) => Commander.Zerg,
            _ => Enum.TryParse(commanderName, out Commander parsedCommander) ? parsedCommander : Commander.None,
        };

        return commander != Commander.None;
    }

    private static bool TryParseToonHandle(string toonHandle, out int region, out int realm, out int id)
    {
        region = 0;
        realm = 0;
        id = 0;

        string[] parts = toonHandle.Split('-');
        return parts.Length == 4
            && string.Equals(parts[1], "S2", StringComparison.Ordinal)
            && int.TryParse(parts[0], out region)
            && int.TryParse(parts[2], out realm)
            && int.TryParse(parts[3], out id);
    }

    private async Task<Sc2Replay> GetReplay(string replayPath)
    {
        return await GetReplay(replayPath, replayDecoderOptions);
    }

    private async Task<Sc2Replay> GetReplay(string replayPath, ReplayDecoderOptions options)
    {
        return await replayDecoder.DecodeAsync(replayPath, options, CancellationToken.None) ?? throw new ArgumentNullException(nameof(replayPath));
    }
}
