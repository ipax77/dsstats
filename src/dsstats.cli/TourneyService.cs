using System.Text.Json;
using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.cli;

public static class TourneyService
{
    // private const string tourneyFolder = "/data/ds/Tourneys";
    public static async Task DecodeTourneyFolders(int cores, string tourneyFolder, CancellationToken token = default)
    {
        var replays = GetDecodeReplays(tourneyFolder);
        if (replays.Any())
        {
            await DecodeReplays(replays, cores, token);
        }
    }

    private static async Task DecodeReplays(List<string> replays, int cores, CancellationToken token)
    {
        ReplayDecoder decoder = new(Program.assemblyPath);

        ReplayDecoderOptions decoderOptions = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        await foreach (var decodeResult in decoder.DecodeParallelWithErrorReport(replays, cores, decoderOptions, token))
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (decodeResult.Sc2Replay == null)
            {
                Console.WriteLine($"failed decoding {decodeResult.ReplayPath}: {decodeResult.Exception}");
                continue;
            }

            try
            {
                var dsRep = Parse.GetDsReplay(decodeResult.Sc2Replay);
                if (dsRep != null)
                {
                    var dtoRep = Parse.GetReplayDto(dsRep);
                    if (dtoRep != null)
                    {
                        var outPath = dtoRep.FileName[..^9] + "json";
                        File.WriteAllText(outPath, JsonSerializer.Serialize(dtoRep));
                    }
                    else
                    {
                        Console.WriteLine($"failed getting replayDto: {decodeResult.ReplayPath}");
                    }
                }
                else
                {
                    Console.WriteLine($"failed parsing sc2Replay (null): {decodeResult.ReplayPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed parsing sc2Replay: {ex.Message}");
            }
        }
        decoder.Dispose();
    }

    private static List<string> GetDecodeReplays(string tourneyFolder)
    {
        var replayPaths = Directory.GetFiles(tourneyFolder, "*.SC2Replay", SearchOption.AllDirectories).ToHashSet();
        var jsonPaths = Directory.GetFiles(tourneyFolder, "*.json", SearchOption.AllDirectories).Select(s => s[..^4] + "SC2Replay").ToList();
        replayPaths.ExceptWith(jsonPaths);
        return replayPaths.ToList();
    }
}