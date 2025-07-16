using System.Reflection;
using s2protocol.NET;

namespace dsstats.challenge;

class Program
{
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    static void Main(string[] args)
    {
        string replayFile = @"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (8632).SC2Replay";
        if (!File.Exists(replayFile))
        {
            throw new FileNotFoundException();
        }
        ReplayDecoder decoder = new(assemblyPath);

        ReplayDecoderOptions options = new()
        {
            Details = true,
            Metadata = true,
            MessageEvents = true,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        CancellationTokenSource cts = new();

        Sc2Replay? sc2Replay = decoder.DecodeAsync(replayFile, options, cts.Token).GetAwaiter().GetResult();
        ArgumentNullException.ThrowIfNull(sc2Replay, "failed decoding replay.");

        DsReplay? dsReplay = Parse.GetDsReplay(sc2Replay);
        ArgumentNullException.ThrowIfNull(dsReplay, "failed parsing replay.");

        var p1 = dsReplay.Players.FirstOrDefault(f => f.GamePos == 1);
        ArgumentNullException.ThrowIfNull(p1, "player 1 not found.");

        foreach (var unit in p1.Units.Where(x => x.Gameloop >= 2300).OrderBy(o => o.Position.X).ThenBy(o => o.Position.Y))
        {
            Console.WriteLine($"({unit.Position.X},{unit.Position.Y})");
        }

        // var p2 = dsReplay.Players.FirstOrDefault(f => f.GamePos == 4);
        // ArgumentNullException.ThrowIfNull(p2, "player 4 not found.");

        cts.Dispose();
        decoder.Dispose();
    }


}
