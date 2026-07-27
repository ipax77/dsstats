using dsstats.parser;
using dsstats.shared;
using s2protocol.NET;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using ExternalDirectStrikePlayer = Sc2DirectStrike.Parser.DirectStrikePlayer;
using ExternalDirectStrikePlayerSpawn = Sc2DirectStrike.Parser.DirectStrikePlayerSpawn;
using ExternalDirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;
using ExternalDirectStrikeSpawnUnit = Sc2DirectStrike.Parser.DirectStrikeSpawnUnit;

namespace dsstats.parser.tests;

[TestClass]
public sealed class DsstatsParserTests
{
    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanParseReplay()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsTrue(replayDto.Gametime > DateTime.MinValue);
        Assert.IsGreaterThan(0, replayDto.BaseBuild);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    [DeploymentItem("testdata/Direct Strike (8608).SC2Replay")]
    [DeploymentItem("testdata/Direct Strike (8609).SC2Replay")]
    public async Task CanComputeHash()
    {
        string replayPath1 = "Direct Strike (8607).SC2Replay";
        string replayPath2 = "Direct Strike (8608).SC2Replay";
        string replayPath3 = "Direct Strike (8609).SC2Replay";
        var replayDto1 = await GetReplayDto(replayPath1);
        var replayDto2 = await GetReplayDto(replayPath2);
        var replayDto3 = await GetReplayDto(replayPath3);
        var hash1 = replayDto1.ComputeHash();
        var hash2 = replayDto2.ComputeHash();
        var hash3 = replayDto3.ComputeHash();
        Assert.AreNotEqual(hash1, hash2);
        Assert.AreNotEqual(hash1, hash3);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanDetermineGameMode()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.AreNotEqual(GameMode.None, replayDto.GameMode);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanDetermineGamePos()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsTrue(replayDto.Players.All(a => a.GamePos > 0));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanSetApm()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsTrue(replayDto.Players.Any(a => a.Apm > 0));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanSetPlayerDuration()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsTrue(replayDto.Players.Any(a => a.Duration > 0));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (711).SC2Replay")]
    public async Task CanSetBunkerDownTime()
    {
        string replayPath = "Direct Strike (711).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsGreaterThan(0, replayDto.Bunker);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (711).SC2Replay")]
    public async Task CanCreateV2Dto()
    {
        string replayPath = "Direct Strike (711).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        string hash = Convert.ToHexString(
                MD5.HashData(Encoding.UTF8.GetBytes(replayDto.CompatHash)))
            .ToLowerInvariant();
        Assert.AreEqual("d23e01a839e35adac5c079a70156506d", hash);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1022).SC2Replay")]
    public async Task CanParseObserverReplay()
    {
        string replayPath = "Direct Strike TE (1022).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsTrue(replayDto.Players.All(a => a.GamePos > 0));
        Assert.IsTrue(replayDto.Players.All(a => a.Apm > 0));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1022).SC2Replay")]
    public async Task CanParseInHouseReplayObservers()
    {
        string replayPath = "Direct Strike TE (1022).SC2Replay";
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);

        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        var parsedReplay = DsstatsParser.ParseInHouseReplay(sc2Replay);

        Assert.AreEqual(replayDto.Players.Count, parsedReplay.Replay.Players.Count);
        Assert.AreEqual(replayDto.ComputeHash(), parsedReplay.Replay.ComputeHash());
        Assert.IsGreaterThan(0, parsedReplay.Observers.Count);
        Assert.IsTrue(parsedReplay.Observers.All(observer => observer.ToonId.Id > 0));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (9181).SC2Replay")]
    public async Task CanParseLastSpawnReplay()
    {
        string replayPath = "Direct Strike (9181).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        var player = replayDto.Players.FirstOrDefault(f => f.Name == "PAX");
        Assert.IsNotNull(player);
        var spawn = player.Spawns.FirstOrDefault(x => x.Breakpoint == Breakpoint.All);
        Assert.IsNotNull(spawn);
        var unit = spawn.Units.FirstOrDefault(f => f.Name == "Annihilator");
        Assert.IsNotNull(unit);
        Assert.AreEqual(10, unit.Count);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (10915).SC2Replay")]
    public async Task MapsCanonicalAbathurBiomassToRawSwarmHosts()
    {
        var replayDto = await GetReplayDto("Direct Strike (10915).SC2Replay");

        ReplayPlayerDto pax = replayDto.Players.Single(player => player.Name == "PAX");
        Assert.AreEqual(Commander.Abathur, pax.Race);
        Assert.AreEqual(5, pax.GamePos);
        UnitDto paxSwarmHosts = pax.Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All)
            .Units
            .Single(unit => unit.Name == "SwarmHostMP");
        Assert.AreEqual(5, paxSwarmHosts.Count);
        Assert.AreEqual(3, paxSwarmHosts.Special);

        ReplayPlayerDto mourissou = replayDto.Players.Single(player => player.Name == "Mourissou");
        Assert.AreEqual(Commander.Abathur, mourissou.Race);
        Assert.AreEqual(3, mourissou.GamePos);
        UnitDto mourissouSwarmHosts = mourissou.Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All)
            .Units
            .Single(unit => unit.Name == "SwarmHostMP");
        Assert.AreEqual(9, mourissouSwarmHosts.Count);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (10912).SC2Replay")]
    public async Task MapsCanonicalGuardianShellToRawArtanisUnits()
    {
        var replayDto = await GetReplayDto("Direct Strike (10912).SC2Replay");

        ReplayPlayerDto pax = replayDto.Players.Single(player => player.Name == "PAX");
        Assert.AreEqual(Commander.Artanis, pax.Race);
        Assert.AreEqual(4, pax.GamePos);
        List<UnitDto> finalUnits = pax.Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All)
            .Units;

        Assert.AreEqual(7, finalUnits.Single(unit => unit.Name == "HonorGuard").Special);
        Assert.AreEqual(3, finalUnits.Single(unit => unit.Name == "PhoenixArtanis").Special);
        Assert.AreEqual(1, finalUnits.Single(unit => unit.Name == "ArtanisObserver").Special);
        Assert.AreEqual(3, finalUnits.Single(unit => unit.Name == "ReaverStarlight").Special);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (1787).SC2Replay")]
    public async Task CanSetReplayLength()
    {
        string replayPath = "Direct Strike TE (1787).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        Assert.AreEqual(617, replayDto.Duration);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanMapCompatHashes()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        Assert.IsFalse(string.IsNullOrEmpty(replayDto.CompatHash));
        Assert.IsTrue(replayDto.Players.All(player => !string.IsNullOrEmpty(player.CompatHash)));
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8607).SC2Replay")]
    public async Task CanParseReplayImportWithSpawnPlaybackSidecar()
    {
        string replayPath = "Direct Strike (8607).SC2Replay";
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);

        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        var import = DsstatsParser.ParseReplayImport(sc2Replay);

        Assert.AreEqual(replayDto.ComputeHash(), import.Replay.ComputeHash());
        Assert.IsNotNull(import.SpawnPlayback);
        Assert.IsNotNull(import.Replay.SpawnPlayback);
        Assert.IsTrue(import.Replay.SpawnPlayback.Available);
        Assert.AreEqual(import.SpawnPlayback.FormatVersion, import.Replay.SpawnPlayback.FormatVersion);
        Assert.AreEqual(import.SpawnPlayback.Compression, import.Replay.SpawnPlayback.Compression);
        Assert.AreEqual(import.SpawnPlayback.CompressedLength, import.Replay.SpawnPlayback.CompressedLength);
        Assert.AreEqual(import.SpawnPlayback.UncompressedLength, import.Replay.SpawnPlayback.UncompressedLength);
        Assert.AreEqual(import.SpawnPlayback.UnitCount, import.Replay.SpawnPlayback.UnitCount);

        var decoded = SpawnPlaybackSidecarCodec.Decode(import.SpawnPlayback.Payload, import.SpawnPlayback.Compression);
        Assert.IsGreaterThan(0, decoded.Players.Sum(player => player.Units.Count));
    }

    [TestMethod]
    public void SpawnPlaybackSidecarFactory_ReturnsNullForIneligibleReplays()
    {
        Sc2Replay sc2Replay = new();

        Assert.IsNull(SpawnPlaybackSidecarFactory.Create(
            sc2Replay,
            CreateDirectStrikeReplay(
                TimeSpan.FromSeconds(301),
                CreateDirectStrikePlayer(1, 1))));

        Assert.IsNull(SpawnPlaybackSidecarFactory.Create(
            sc2Replay,
            CreateDirectStrikeReplay(
                TimeSpan.FromSeconds(301),
                CreateDirectStrikePlayer(1, 1),
                CreateDirectStrikePlayer(2, 4))));

        Assert.IsNull(SpawnPlaybackSidecarFactory.Create(
            sc2Replay,
            CreateDirectStrikeReplay(
                TimeSpan.FromSeconds(300),
                CreateDirectStrikePlayer(1, 1, CreateSpawnWithUnit()),
                CreateDirectStrikePlayer(2, 4))));

        Assert.IsNotNull(SpawnPlaybackSidecarFactory.Create(
            sc2Replay,
            CreateDirectStrikeReplay(
                TimeSpan.FromSeconds(301),
                CreateDirectStrikePlayer(1, 1, CreateSpawnWithUnit()),
                CreateDirectStrikePlayer(2, 4, CreateSpawnWithUnit()))));
    }

    private async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }

    private static ExternalDirectStrikeReplay CreateDirectStrikeReplay(
        TimeSpan duration,
        params ExternalDirectStrikePlayer[] players)
    {
        return new()
        {
            Duration = duration,
            Players = new ReadOnlyCollection<ExternalDirectStrikePlayer>(players)
        };
    }

    private static ExternalDirectStrikePlayer CreateDirectStrikePlayer(
        int teamId,
        int gamePos,
        params ExternalDirectStrikePlayerSpawn[] spawns)
    {
        return new()
        {
            TeamId = teamId,
            GamePos = gamePos,
            Spawns = new ReadOnlyCollection<ExternalDirectStrikePlayerSpawn>(spawns)
        };
    }

    private static ExternalDirectStrikePlayerSpawn CreateSpawnWithUnit()
    {
        return new()
        {
            Number = 1,
            StartGameloop = 112,
            EndGameloop = 224,
            Units = new ReadOnlyCollection<ExternalDirectStrikeSpawnUnit>(
            [
                new()
                {
                    UnitIndex = 1,
                    Name = "Marine",
                    Gameloop = 112,
                    X = 165,
                    Y = 174
                }
            ])
        };
    }
}
