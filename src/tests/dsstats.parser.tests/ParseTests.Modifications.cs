using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;
using System.Reflection;

namespace Sc2DirectStrike.Tests;

public sealed partial class ParseTests
{
    [TestMethod]
    public async Task DetectsAbathurBiomassAndBreakpointCounts()
    {
        (_, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (1164).SC2Replay");

        AssertModificationAnalysis(player, BuildUnitModificationType.Biomass);
        (int Gameloop, int TargetTag, string Name, int Amount)[] expected =
        [
            (2837, 76808205, "Mutalisk", 3),
            (6604, 46923782, "Mutalisk", 3),
            (10585, 15990821, "Mutalisk", 3),
            (14891, 59506699, "Mutalisk", 3),
            (19687, 247201795, "Mutalisk", 3),
            (25366, 257949841, "Viper", 5),
            (30814, 340000832, "Guardian", 4),
            (36114, 239599839, "Guardian", 4),
            (40822, 47710210, "VileRoach", 3),
            (47283, 310902976, "SwarmHost", 5),
        ];
        Assert.HasCount(expected.Length, player.BuildUnitModifications);
        Assert.AreEqual(36, player.BuildUnitModifications.Sum(modification => modification.Amount));
        for (int i = 0; i < expected.Length; i++)
        {
            DirectStrikeBuildUnitModification actual = player.BuildUnitModifications[i];
            Assert.AreEqual(expected[i].Gameloop, actual.Gameloop);
            Assert.AreEqual(expected[i].TargetTag, actual.TargetUnitTag);
            Assert.AreEqual(expected[i].Name, actual.TargetUnitName);
            Assert.AreEqual(expected[i].Amount, actual.Amount);
            Assert.IsNull(actual.SourceUnitTag);
        }

        AssertBreakpoint(dto, Breakpoint.Min5, ("Mutalisk", 2));
        AssertBreakpoint(dto, Breakpoint.Min10, ("Mutalisk", 3));
        AssertBreakpoint(dto, Breakpoint.Min15, ("Mutalisk", 5));
        AssertBreakpoint(dto, Breakpoint.All,
            ("Guardian", 2), ("Mutalisk", 5), ("SwarmHost", 1), ("VileRoach", 1), ("Viper", 1));
    }

    [TestMethod]
    public async Task DetectsAlarakPowerOverwhelmingAndDeduplicatesBreakpointCounts()
    {
        (_, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (1165).SC2Replay");

        AssertModificationAnalysis(player, BuildUnitModificationType.PowerOverwhelming);
        Assert.HasCount(22, player.BuildUnitModifications);
        CollectionAssert.AreEqual(
            new[] { 9537, 9596, 11059, 11289, 12454, 12540, 13916, 13991, 14970, 15284, 16353,
                16619, 17660, 17985, 19128, 19827, 20604, 20892, 22671, 22977, 24338, 25659 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { 76808195, 76283920 },
            player.BuildUnitModifications.Select(modification => modification.TargetUnitTag).Distinct().ToArray());
        Assert.AreEqual(13, player.BuildUnitModifications.Count(modification => modification.TargetUnitTag == 76808195));
        Assert.AreEqual(9, player.BuildUnitModifications.Count(modification => modification.TargetUnitTag == 76283920));
        Assert.IsTrue(player.BuildUnitModifications.All(modification =>
            modification is { TargetUnitName: "Ascendant", Amount: 1, SourceUnitName: "Supplicant" }
            && modification.SourceUnitTag is not null));

        AssertBreakpoint(dto, Breakpoint.Min5);
        AssertBreakpoint(dto, Breakpoint.Min10, ("Supplicant", 6));
        AssertBreakpoint(dto, Breakpoint.Min15, ("Supplicant", 16));
        AssertBreakpoint(dto, Breakpoint.All, ("Supplicant", 22));
    }

    [TestMethod]
    public async Task DetectsArtanisGuardianShellAndBreakpointCounts()
    {
        (Sc2Replay replay, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10853).SC2Replay");

        AssertModificationAnalysis(player, BuildUnitModificationType.GuardianShell);
        (int Gameloop, int TargetTag)[] expected =
        [
            (354, 74973186), (2287, 79691781), (2493, 54263819), (3879, 29097989),
            (3981, 15204357), (5573, 52690962), (5641, 39059473), (7178, 58458129),
            (8239, 25165848), (8728, 34603042), (10071, 51118121), (14328, 16252945),
            (18604, 59506809), (18714, 305659917), (19911, 301465660), (21256, 288358467),
        ];
        CollectionAssert.AreEqual(expected.Select(value => value.Gameloop).ToArray(),
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(expected.Select(value => value.TargetTag).ToArray(),
            player.BuildUnitModifications.Select(modification => modification.TargetUnitTag).ToArray());
        CollectionAssert.AreEqual(
            new[] { 6147, 11944, 17836 },
            (replay.GameEvents?.BaseGameEvents ?? [])
                .OfType<SCmdEvent>()
                .Where(command => command is { AbilLink: 1242, AbilCmdIndex: 0 })
                .Select(command => command.Gameloop)
                .ToArray());

        AssertBreakpoint(dto, Breakpoint.Min5, ("HonorGuard", 7));
        AssertBreakpoint(dto, Breakpoint.Min10, ("HonorGuard", 10), ("Observer", 1));
        AssertBreakpoint(dto, Breakpoint.Min15,
            ("HighArchon", 2), ("HonorGuard", 10), ("Immortal", 2), ("Observer", 1));
        AssertBreakpoint(dto, Breakpoint.All,
            ("HighArchon", 3), ("HonorGuard", 10), ("Immortal", 2), ("Observer", 1));
    }

    [TestMethod]
    public async Task DetectsKitchenSinkArtanisGuardianShellCommands()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10912).SC2Replay");
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);
        DirectStrikePlayer player = parsed.Players.Single(player => player.Name == "PAX");
        ReplayPlayerDto dto = DirectStrikeReplayDtoMapper.Map(replay, parsed)
            .Players.Single(player => player.Name == "PAX");
        Slot paxSlot = (replay.Initdata?.LobbyState?.Slots ?? [])
            .Single(slot => slot.WorkingSetSlotId == player.SlotId);

        AssertModificationAnalysis(player, BuildUnitModificationType.GuardianShell);
        Assert.AreEqual(Commander.Artanis, player.Commander);
        Assert.AreEqual(4, player.GamePos);
        CollectionAssert.AreEqual(
            new[] { 161, 367, 641, 1377, 1936, 2452, 3058, 4406, 6070, 8714, 13268, 20587, 23317, 30060 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard",
                "Observer", "Phoenix", "Phoenix", "Phoenix", "Reaver", "Reaver", "Reaver",
            },
            player.BuildUnitModifications.Select(modification => modification.TargetUnitName).ToArray());

        CollectionAssert.AreEqual(
            new[] { 7, 3, 1, 3 },
            (replay.GameEvents?.BaseGameEvents ?? [])
            .OfType<SCmdEvent>()
            .Where(command => command.UserId == paxSlot.UserId
                && command.AbilCmdIndex == 0
                && command.AbilLink is 1114 or 1118 or 1119 or 1121)
            .GroupBy(command => command.AbilLink)
            .OrderBy(group => group.Key)
            .Select(group => group.Count())
            .ToArray());
        HashSet<int> paxTargetTags = player.BuildUnitModifications
            .Select(modification => modification.TargetUnitTag)
            .ToHashSet();
        Assert.IsFalse(parsed.Players
            .Where(other => other != player)
            .SelectMany(other => other.BuildUnitModifications)
            .Any(modification => paxTargetTags.Contains(modification.TargetUnitTag)));

        AssertBreakpoint(dto, Breakpoint.Min5, ("HonorGuard", 7), ("Observer", 1), ("Phoenix", 1));
        AssertBreakpoint(dto, Breakpoint.Min10, ("HonorGuard", 7), ("Observer", 1), ("Phoenix", 3));
        AssertBreakpoint(dto, Breakpoint.Min15, ("HonorGuard", 7), ("Observer", 1), ("Phoenix", 3));
        AssertBreakpoint(dto, Breakpoint.All,
            ("HonorGuard", 7), ("Observer", 1), ("Phoenix", 3), ("Reaver", 3));
    }

    [TestMethod]
    public async Task DetectsHistoricalArtanisRosterAndGuardianShell()
    {
        (Sc2Replay replay, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10085).SC2Replay", "TigerPuppy");

        AssertModificationAnalysis(player, BuildUnitModificationType.GuardianShell);
        Assert.AreEqual(Commander.Artanis, player.Commander);
        Assert.AreEqual(5, player.GamePos);
        AssertGuardianShellCommandCounts(replay, player, (1119, 13), (1121, 2));
        Assert.HasCount(1, player.BuildUnitModifications);
        Assert.AreEqual(15500, player.BuildUnitModifications[0].Gameloop);
        Assert.AreEqual("Observer", player.BuildUnitModifications[0].TargetUnitName);

        UnitDto[] finalUnits = [.. dto.Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All)
            .Units
            .OrderBy(unit => unit.Name, StringComparer.Ordinal)];
        CollectionAssert.AreEqual(
            new[]
            {
                "ArtanisObserver", "DragoonStarlight", "HighArchon", "HighTemplarArtanis", "HonorGuard",
                "ImmortalArtanis", "PhoenixArtanis", "PurifierTempest", "ReaverStarlight",
            },
            finalUnits.Select(unit => unit.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { 1, 18, 3, 16, 18, 15, 4, 11, 6 },
            finalUnits.Select(unit => unit.Count).ToArray());

        AssertBreakpoint(dto, Breakpoint.Min5);
        AssertBreakpoint(dto, Breakpoint.Min10);
        AssertBreakpoint(dto, Breakpoint.Min15, ("Observer", 1));
        AssertBreakpoint(dto, Breakpoint.All, ("Observer", 1));
    }

    [TestMethod]
    public async Task DetectsArtanisHighTemplarAndPurifierTempestGuardianShell()
    {
        (Sc2Replay replay, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10804).SC2Replay", "MeisterEder");

        AssertModificationAnalysis(player, BuildUnitModificationType.GuardianShell);
        AssertGuardianShellCommandCounts(replay, player, (1114, 8), (1116, 3), (1119, 1), (1120, 2), (1122, 1));
        CollectionAssert.AreEqual(
            new[] { 4958, 5171, 7650, 7678, 7725, 7839, 7892, 8063, 13895, 19292, 19697, 22042, 22481, 25446, 26313 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard", "HonorGuard",
                "HonorGuard", "PurifierTempest", "Immortal", "Immortal", "HighTemplar", "HighTemplar",
                "HighTemplar", "Observer",
            },
            player.BuildUnitModifications.Select(modification => modification.TargetUnitName).ToArray());

        AssertBreakpoint(dto, Breakpoint.Min5, ("HonorGuard", 2));
        AssertBreakpoint(dto, Breakpoint.Min10, ("HonorGuard", 8));
        AssertBreakpoint(dto, Breakpoint.Min15, ("HonorGuard", 8), ("Immortal", 2), ("PurifierTempest", 1));
        AssertBreakpoint(dto, Breakpoint.All,
            ("HighTemplar", 3), ("HonorGuard", 8), ("Immortal", 2), ("Observer", 1), ("PurifierTempest", 1));
    }

    [TestMethod]
    public async Task DetectsArtanisDragoonGuardianShell()
    {
        (Sc2Replay replay, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10766).SC2Replay", "Sybion");

        AssertModificationAnalysis(player, BuildUnitModificationType.GuardianShell);
        AssertGuardianShellCommandCounts(replay, player, (1115, 13), (1120, 1));
        CollectionAssert.AreEqual(
            new[] { 2673, 3875, 4273, 4665, 5854, 6939, 7105, 8666, 8855, 8963, 9162, 9382, 9945, 19972 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(
            Enumerable.Repeat("Dragoon", 13).Append("Immortal").ToArray(),
            player.BuildUnitModifications.Select(modification => modification.TargetUnitName).ToArray());

        AssertBreakpoint(dto, Breakpoint.Min5, ("Dragoon", 5));
        AssertBreakpoint(dto, Breakpoint.Min10, ("Dragoon", 13));
        AssertBreakpoint(dto, Breakpoint.Min15, ("Dragoon", 13), ("Immortal", 1));
        AssertBreakpoint(dto, Breakpoint.All, ("Dragoon", 13), ("Immortal", 1));
    }

    [TestMethod]
    [DataRow("HonorGuard", "Honor Guard")]
    [DataRow("DragoonStarlight", "Dragoon")]
    [DataRow("Dragoon", "Dragoon")]
    [DataRow("HighTemplarArtanis", "High Templar")]
    [DataRow("HighTemplar", "High Templar")]
    [DataRow("HighArchon", "High Archon")]
    [DataRow("PhoenixArtanis", "Phoenix")]
    [DataRow("Phoenix", "Phoenix")]
    [DataRow("ArtanisObserver", "Observer")]
    [DataRow("Observer", "Observer")]
    [DataRow("ImmortalArtanis", "Immortal")]
    [DataRow("Immortal", "Immortal")]
    [DataRow("ReaverStarlight", "Reaver")]
    [DataRow("Reaver", "Reaver")]
    [DataRow("PurifierTempest", "Purifier Tempest")]
    public void NormalizesArtanisGuardianShellUnitNames(string rawUnitName, string expectedDisplayName)
    {
        MethodInfo method = typeof(Sc2DirectStrikeParser).GetMethod(
            "GetBuildUnitDisplayName",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find build-unit display-name method.");

        Assert.AreEqual(expectedDisplayName, method.Invoke(null, [rawUnitName]));
    }

    [TestMethod]
    public async Task DetectsKaraxOrbitalStrikeBeaconAndBreakpointCounts()
    {
        (_, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10822).SC2Replay");

        AssertModificationAnalysis(player, BuildUnitModificationType.OrbitalStrikeBeacon);
        CollectionAssert.AreEqual(
            new[] { 4588, 9021, 13494, 17970, 23111 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(
            new[] { 62390277, 77856771, 27000851, 77594634, 62652429 },
            player.BuildUnitModifications.Select(modification => modification.TargetUnitTag).ToArray());
        Assert.IsTrue(player.BuildUnitModifications.All(modification => modification.TargetUnitName == "Mirage"));

        AssertBreakpoint(dto, Breakpoint.Min5, ("Mirage", 1));
        AssertBreakpoint(dto, Breakpoint.Min10, ("Mirage", 2));
        AssertBreakpoint(dto, Breakpoint.Min15, ("Mirage", 4));
        AssertBreakpoint(dto, Breakpoint.All, ("Mirage", 5));
    }

    [TestMethod]
    public async Task DetectsVorazunDarkPylonAndBreakpointCounts()
    {
        (Sc2Replay replay, DirectStrikePlayer player, ReplayPlayerDto dto) =
            await ParseModificationPlayer("testdata/Direct Strike (10844).SC2Replay");

        AssertModificationAnalysis(player, BuildUnitModificationType.DarkPylon);
        CollectionAssert.AreEqual(
            new[] { 15203, 15216, 15228, 15244, 15257, 15290, 15305, 16104, 17439, 18879, 19044, 20235, 20566 },
            player.BuildUnitModifications.Select(modification => modification.Gameloop).ToArray());
        CollectionAssert.AreEqual(
            new[] { 43778052, 91750427, 85721102, 30933009, 267124745, 38010921, 273416208,
                305397774, 277086251, 328990730, 308019232, 60031075, 297271338 },
            player.BuildUnitModifications.Select(modification => modification.TargetUnitTag).ToArray());
        CollectionAssert.AreEqual(
            new[] { 15202, 16103, 17438, 18878, 19043, 20234, 20565 },
            (replay.GameEvents?.BaseGameEvents ?? [])
                .OfType<SCmdEvent>()
                .Where(command => command is { UserId: 0, AbilLink: 2013, AbilCmdIndex: 0 })
                .Select(command => command.Gameloop)
                .ToArray());
        Assert.IsTrue(player.BuildUnitModifications.All(modification => modification.TargetUnitName == "VoidRay"));

        AssertBreakpoint(dto, Breakpoint.Min5);
        AssertBreakpoint(dto, Breakpoint.Min10);
        AssertBreakpoint(dto, Breakpoint.Min15, ("VoidRay", 11));
        AssertBreakpoint(dto, Breakpoint.All, ("VoidRay", 13));
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow("testdata/Direct Strike (1164).SC2Replay", BuildUnitModificationType.Biomass, 97563, 10)]
    [DataRow("testdata/Direct Strike (1165).SC2Replay", BuildUnitModificationType.PowerOverwhelming, 97563, 22)]
    [DataRow("testdata/Direct Strike (10853).SC2Replay", BuildUnitModificationType.GuardianShell, 96883, 16)]
    [DataRow("testdata/Direct Strike (10822).SC2Replay", BuildUnitModificationType.OrbitalStrikeBeacon, 97425, 5)]
    [DataRow("testdata/Direct Strike (10844).SC2Replay", BuildUnitModificationType.DarkPylon, 97425, 13)]
    public async Task ModificationMinimumBuildsAreInclusiveAndForwardCompatible(
        string replayPath,
        BuildUnitModificationType expectedType,
        int minimumDataBuild,
        int expectedCount)
    {
        Sc2Replay replay = await GetReplay(replayPath);

        DirectStrikePlayer atMinimum = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, minimumDataBuild))
            .Players.Single(player => player.Name == "PAX");
        AssertModificationAnalysis(atMinimum, expectedType);
        Assert.HasCount(expectedCount, atMinimum.BuildUnitModifications);

        DirectStrikePlayer newerBuild = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, 100000))
            .Players.Single(player => player.Name == "PAX");
        AssertModificationAnalysis(newerBuild, expectedType);
        Assert.HasCount(expectedCount, newerBuild.BuildUnitModifications);

        DirectStrikePlayer beforeMinimum = Sc2DirectStrikeParser.Parse(WithDataBuild(replay, minimumDataBuild - 1))
            .Players.Single(player => player.Name == "PAX");
        Assert.HasCount(1, beforeMinimum.BuildUnitModificationAnalysis);
        Assert.AreEqual(expectedType, beforeMinimum.BuildUnitModificationAnalysis[0].Type);
        Assert.AreEqual(BuildUnitModificationAnalysisStatus.UnsupportedDataBuild,
            beforeMinimum.BuildUnitModificationAnalysis[0].Status);
        Assert.IsEmpty(beforeMinimum.BuildUnitModifications);
    }

    [TestMethod]
    public async Task CommandBasedModificationAnalysisIsUnavailableWithoutGameEvents()
    {
        ReplayDecoderOptions options = DirectStrikeDecoderOptions.Create();
        options.GameEvents = false;
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10853).SC2Replay", options);

        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);
        DirectStrikePlayer player = parsed.Players.Single(player => player.Name == "PAX");

        Assert.HasCount(1, player.BuildUnitModificationAnalysis);
        Assert.AreEqual(BuildUnitModificationAnalysisStatus.RequiredEventsUnavailable,
            player.BuildUnitModificationAnalysis[0].Status);
        Assert.IsEmpty(player.BuildUnitModifications);
    }

    [TestMethod]
    public async Task ModificationAnalysisReportsUnsupportedDataBuild()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10853).SC2Replay");
        Header header = replay.Header;
        replay = replay with
        {
            Header = new Header(
                1,
                header.ElapsedGameLoops,
                header.UseScaledTime,
                header.Version,
                header.Signature,
                header.NgpdRootKey,
                header.CompatibilityHash,
                header.Type,
                header.Flags,
                header.Build,
                header.BaseBuild),
        };

        DirectStrikePlayer player = Sc2DirectStrikeParser.Parse(replay).Players.Single(player => player.Name == "PAX");

        Assert.HasCount(1, player.BuildUnitModificationAnalysis);
        Assert.AreEqual(BuildUnitModificationAnalysisStatus.UnsupportedDataBuild,
            player.BuildUnitModificationAnalysis[0].Status);
        Assert.IsEmpty(player.BuildUnitModifications);
    }

    [TestMethod]
    public void BreakpointCountsUseInclusiveFixedGameloopsAndDeterministicOrdering()
    {
        DirectStrikePlayer player = new()
        {
            BuildUnitModifications = new List<DirectStrikeBuildUnitModification>
            {
                new(BuildUnitModificationType.Biomass, 6720, 1, "Zeta", 3, null, null),
                new(BuildUnitModificationType.Biomass, 13440, 2, "Alpha", 3, null, null),
                new(BuildUnitModificationType.Biomass, 15000, 1, "Zeta", 3, null, null),
                new(BuildUnitModificationType.Biomass, 20160, 3, "Beta", 3, null, null),
            }.AsReadOnly(),
        };
        MethodInfo method = typeof(DirectStrikeReplayDtoMapper).GetMethod(
            "CreateBuildUnitModificationCounts",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find breakpoint aggregation method.");

        var counts = (IReadOnlyCollection<BuildUnitModificationCountDto>[])method.Invoke(null, [player])!;

        CollectionAssert.AreEqual(new[] { "Zeta" },
            counts[(int)Breakpoint.Min5].Select(value => value.TargetUnitName).ToArray());
        CollectionAssert.AreEqual(new[] { "Alpha", "Zeta" },
            counts[(int)Breakpoint.Min10].Select(value => value.TargetUnitName).ToArray());
        CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Zeta" },
            counts[(int)Breakpoint.Min15].Select(value => value.TargetUnitName).ToArray());
        Assert.AreEqual(1,
            counts[(int)Breakpoint.All].Single(value => value.TargetUnitName == "Zeta").Count);
    }

    [TestMethod]
    public async Task ModificationSummariesDoNotChangeCompatibilityHashes()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (1164).SC2Replay");
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);
        ReplayDto withModifications = DirectStrikeReplayDtoMapper.Map(replay, parsed);
        DirectStrikePlayer player = parsed.Players.Single(player => player.Name == "PAX");
        player.BuildUnitModifications = Array.AsReadOnly(Array.Empty<DirectStrikeBuildUnitModification>());
        ReplayDto withoutModifications = DirectStrikeReplayDtoMapper.Map(replay, parsed);

        Assert.AreEqual(withModifications.CompatHash, withoutModifications.CompatHash);
        Assert.AreEqual(
            withModifications.Players.Single(player => player.Name == "PAX").CompatHash,
            withoutModifications.Players.Single(player => player.Name == "PAX").CompatHash);
        Assert.IsTrue(withoutModifications.Players.Single(player => player.Name == "PAX")
            .Spawns.Single(spawn => spawn.Breakpoint == Breakpoint.All)
            .Units.All(unit => unit.Special is null));
    }

    private async Task<(Sc2Replay Replay, DirectStrikePlayer Player, ReplayPlayerDto Dto)> ParseModificationPlayer(
        string replayPath,
        string playerName = "PAX")
    {
        Sc2Replay replay = await GetReplay(replayPath);
        DirectStrikeReplay parsed = Sc2DirectStrikeParser.Parse(replay);
        DirectStrikePlayer player = parsed.Players.Single(player => player.Name == playerName);
        ReplayPlayerDto dto = DirectStrikeReplayDtoMapper.Map(replay, parsed)
            .Players.Single(player => player.Name == playerName);
        return (replay, player, dto);
    }

    private static void AssertGuardianShellCommandCounts(
        Sc2Replay replay,
        DirectStrikePlayer player,
        params (int AbilityLink, int Count)[] expected)
    {
        Slot playerSlot = (replay.Initdata?.LobbyState?.Slots ?? [])
            .Single(slot => slot.WorkingSetSlotId == player.SlotId);
        var actual = (replay.GameEvents?.BaseGameEvents ?? [])
            .OfType<SCmdEvent>()
            .Where(command => command.UserId == playerSlot.UserId
                && command.AbilCmdIndex == 0
                && command.AbilLink is >= 1114 and <= 1122)
            .GroupBy(command => command.AbilLink)
            .OrderBy(group => group.Key)
            .Select(group => (AbilityLink: group.Key, Count: group.Count()))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    private static Sc2Replay WithDataBuild(Sc2Replay replay, int dataBuild)
    {
        Header header = replay.Header;
        return replay with
        {
            Header = new Header(
                dataBuild,
                header.ElapsedGameLoops,
                header.UseScaledTime,
                header.Version,
                header.Signature,
                header.NgpdRootKey,
                header.CompatibilityHash,
                header.Type,
                header.Flags,
                header.Build,
                header.BaseBuild),
        };
    }

    private static void AssertModificationAnalysis(
        DirectStrikePlayer player,
        BuildUnitModificationType expectedType)
    {
        Assert.HasCount(1, player.BuildUnitModificationAnalysis);
        Assert.AreEqual(expectedType, player.BuildUnitModificationAnalysis[0].Type);
        Assert.AreEqual(BuildUnitModificationAnalysisStatus.Analyzed,
            player.BuildUnitModificationAnalysis[0].Status);
    }

    private static void AssertBreakpoint(
        ReplayPlayerDto player,
        Breakpoint breakpoint,
        params (string Name, int Count)[] expected)
    {
        SpawnDto spawn = player.Spawns.Single(spawn => spawn.Breakpoint == breakpoint);
        var actual = spawn.Units
            .Where(unit => unit.Special is not null)
            .Select(unit => (
                Name: UnitMap.GetNormalizedUnitName(unit.Name, player.Race),
                Count: unit.Special!.Value))
            .ToArray();

        Assert.HasCount(expected.Length, actual);
        foreach ((string name, int count) in expected)
        {
            string normalizedExpectedName = UnitMap.GetNormalizedUnitName(name, player.Race);
            var match = actual.SingleOrDefault(value => value.Name == normalizedExpectedName);
            Assert.IsNotNull(
                match.Name,
                $"Missing modification target '{normalizedExpectedName}' at {breakpoint}. Actual: "
                + string.Join(", ", actual.Select(value => $"{value.Name}={value.Count}")));
            Assert.AreEqual(
                count,
                match.Count,
                $"Unexpected modification count for '{name}' at {breakpoint}.");
        }
    }
}
