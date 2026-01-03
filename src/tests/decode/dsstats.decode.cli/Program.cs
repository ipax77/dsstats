using System.Security.Cryptography;
using System.Text.Json;
using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.decode.cli;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No replay found.");
            return;
        }

        var replayPath = args[0];
        if (!File.Exists(replayPath))
        {
            Console.WriteLine("File not found.");
            return;
        }

        ReplayDecoder replayDecoder = new();
        ReplayDecoderOptions options = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            TrackerEvents = true,
        };

        var sc2Replay = await replayDecoder.DecodeAsync(replayPath, options);
        ArgumentNullException.ThrowIfNull(sc2Replay);

        var dsReplay = Parse.GetDsReplay(sc2Replay);
        ArgumentNullException.ThrowIfNull(dsReplay);

        using var md5 = MD5.Create();
        var replayDto = Parse.GetReplayDto(dsReplay, md5);

        ArgumentNullException.ThrowIfNull(replayDto);

        var json = JsonSerializer.Serialize(replayDto);
        Console.WriteLine(json);
    }
}
