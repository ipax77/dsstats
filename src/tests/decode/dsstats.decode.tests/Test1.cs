using dsstats.shared;
using s2protocol.NET;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace dsstats.decode.tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var replayPath = @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (9178).SC2Replay";
        ReplayDecoderOptions options = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            TrackerEvents = true,
        };

        ReplayDecoder replayDecoder = new();

        var sc2Replay = await replayDecoder.DecodeAsync(replayPath, options);
        Assert.IsNotNull(sc2Replay);

        var newReplayDto = dsstats.parser.DsstatsParser.ParseReplay(sc2Replay, true);
        Assert.IsGreaterThan(0, newReplayDto.CompatHash.Length);

        var minArmy = newReplayDto.Players.SelectMany(s => s.Spawns).Min(m => m.ArmyValue);
        var minKills = newReplayDto.Players.SelectMany(s => s.Spawns).Min(m => m.KilledValue);
        var minIncome = newReplayDto.Players.SelectMany(s => s.Spawns).Min(m => m.Income);
        var maxKills = newReplayDto.Players.SelectMany(s => s.Spawns).Max(m => m.KilledValue);

        var oldReplayDto = await GetOldReplay(replayPath);

        Assert.AreEqual(oldReplayDto.GameTime, newReplayDto.Gametime);

        //Assert.AreEqual(minArmy, oldReplayDto.Minarmy);
        //Assert.AreEqual(minKills, oldReplayDto.Minkillsum);
        //Assert.AreEqual(minIncome, oldReplayDto.Minincome);
        //Assert.AreEqual(maxKills, oldReplayDto.Maxkillsum);

        //using var md5 = MD5.Create();
        //var compatHash = GetMd5Hash(md5, newReplayDto.CompatHash);
        //Assert.AreEqual(oldReplayDto.ReplayHash, compatHash);
    }


    private async Task<ReplayV2Dto> GetOldReplay(string replayPath)
    {
        var decodeBinary =
            @"C:\Users\pax77\source\repos\dsstats10\src\tests\decode\dsstats.decode.cli\bin\Release\net10.0\dsstats.decode.cli.exe";

        var startInfo = new ProcessStartInfo
        {
            FileName = decodeBinary,
            Arguments = $"\"{replayPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Decoder failed with exit code {process.ExitCode}: {stderr}");
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException("Decoder returned no output.");
        }

        var replay = JsonSerializer.Deserialize<ReplayV2Dto>(stdout);
        ArgumentNullException.ThrowIfNull(replay);

        return replay;
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
