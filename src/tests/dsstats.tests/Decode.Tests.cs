using dsstats.parser;
using dsstats.shared;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.tests;

[TestClass]
public sealed class DecodeTests
{
    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (711).SC2Replay")]
    [DeploymentItem("testdata/Direct Strike (711).json")]
    [DeploymentItem("testdata/Direct Strike (8601).SC2Replay")]
    [DeploymentItem("testdata/Direct Strike (8601).json")]
    [DataRow("Direct Strike (711)", "d23e01a839e35adac5c079a70156506d")]
    [DataRow("Direct Strike (8601)", "945f16ab3e1843919696033575ce30b8")]
    public async Task CanComputeSameReplayHash(string replayName, string expectedCompatHashMd5)
    {
        string replayPath = $"{replayName}.SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        using var md5 = MD5.Create();
        var compatHash = ReplayV2DtoExtensions.GetMd5Hash(md5, replayDto.CompatHash);
        Assert.AreEqual(expectedCompatHashMd5, compatHash);
        Assert.IsGreaterThan(0, replayDto.Duration);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (8602).SC2Replay")]
    [DeploymentItem("testdata/Direct Strike (8602).json")]
    public async Task ShouldProduceLastSpawn()
    {
        string replayPath = "Direct Strike (8602).SC2Replay";
        // string jsonPath = "Direct Strike (8602).json";
        var replayDto = await GetReplayDto(replayPath);
        var player = replayDto.Players.FirstOrDefault(f => f.GamePos == 3);
        var spawn = player?.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
        foreach (var unit in spawn!.Units)
        {
            Console.WriteLine($"{unit.Name} => {unit.Count}");
        }
        Assert.IsNotNull(spawn);
        Assert.IsGreaterThan(1, spawn.Units.Count);

    }


    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (3).SC2Replay")]
    public async Task ShouldMapPlayer()
    {
        string replayPath = "Direct Strike TE (3).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        var player = replayDto.Players.FirstOrDefault(f => f.GamePos == 4);
        var spawn = player?.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
        Assert.IsNotNull(spawn);
        Assert.IsGreaterThan(1, spawn.Units.Count);
        Assert.IsGreaterThan(0, spawn.Income);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike TE (4545).SC2Replay")]
    public async Task ShouldDecodeTest1()
    {
        string replayPath = "Direct Strike TE (4545).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.IsNotNull(replayDto);
    }

    [TestMethod]
    [DeploymentItem("testdata/Direct Strike (26).SC2Replay")]
    public async Task CanSetGameModeForOldReplays()
    {
        string replayPath = "Direct Strike (26).SC2Replay";
        var replayDto = await GetReplayDto(replayPath);
        Assert.AreNotEqual(GameMode.Tutorial, replayDto.GameMode);
    }

    private static async Task<ReplayDto> GetReplayDto(string replayPath)
    {
        var sc2Replay = await DsstatsParser.GetSc2Replay(replayPath);
        Assert.IsNotNull(sc2Replay);
        var replayDto = DsstatsParser.ParseReplay(sc2Replay);
        Assert.IsNotNull(replayDto);
        return replayDto;
    }
}

public static class ReplayV2DtoExtensions
{
    public static void GenHash(this ReplayV2Dto replay, MD5 md5hash)
    {
        if (!replay.ReplayPlayers.Any())
        {
            throw new ArgumentOutOfRangeException(nameof(replay));
        }

        StringBuilder sb = new();
        foreach (var pl in replay.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            if (pl.Player == null)
            {
                throw new ArgumentOutOfRangeException(nameof(replay));
            }
            sb.Append(pl.GamePos + pl.Race + pl.Player.ToonId);
        }
        sb.Append(replay.GameMode + replay.Playercount);
        sb.Append(replay.Minarmy + replay.Minkillsum + replay.Minincome + replay.Maxkillsum);

        replay.ReplayHash = GetMd5Hash(md5hash, sb.ToString());
    }

    public static string GetMd5Hash(MD5 md5Hash, string input)
    {
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        return sBuilder.ToString();
    }
}
