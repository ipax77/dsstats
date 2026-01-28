using dsstats.parser;
using dsstats.shared;
using System.Security.Cryptography;

namespace dsstats.tests;

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
        using var md5 = MD5.Create();
        var hash = ReplayV2DtoExtensions.GetMd5Hash(md5, replayDto.CompatHash);
        Assert.AreEqual("5c75850a73f9db18846748147e09f071", hash);
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
    [DeploymentItem("testdata/Direct Strike TE (1787).SC2Replay")]
    public async Task CanSetReplayLength()
    {
        string replayPath = "Direct Strike TE (1787).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        Assert.AreEqual(617, replayDto.Duration);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (7791).SC2Replay")]
    public async Task CanSetGameMode()
    {
        string replayPath = "Direct Strike (7791).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        Assert.AreNotEqual(GameMode.Tutorial, replayDto.GameMode);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (130).SC2Replay")]
    public async Task CanSetGameMode2()
    {
        string replayPath = "Direct Strike (130).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);

        Assert.AreNotEqual(GameMode.Tutorial, replayDto.GameMode);
    }

    private async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }
}
