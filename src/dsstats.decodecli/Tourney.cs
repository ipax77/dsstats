
using System.Security.Cryptography;
using System.Text.Json;
using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.decodecli;

public static class Tourney
{
    public static async Task CreateTourneyJsons(string tourneyPath)
    {
        var replays = Directory.GetFiles(tourneyPath, "*.SC2Replay", SearchOption.AllDirectories)
            .ToHashSet();
        var existingJsons = replays.Where(x => File.Exists(Path.ChangeExtension(x, "json"))).ToList();
        replays.ExceptWith(existingJsons);

        if (replays.Count == 0)
        {
            Console.Write("not new tourney replays found.");
            return;
        }

        replays = replays.Select(s => s.Replace("\\", "/")).ToHashSet();

        ReplayDecoder decoder = new(Program.assemblyPath);

        ReplayDecoderOptions options = new()
        {
            Initdata = true,
            Details = true,
            Metadata = true,
            MessageEvents = false,
            TrackerEvents = true,
            GameEvents = false,
            AttributeEvents = false
        };

        using var md5 = MD5.Create();

        await foreach (var result in decoder.DecodeParallelWithErrorReport(replays, 8, options))
        {
            if (result.Sc2Replay is not null)
            {
                var dsReplay = Parse.GetDsReplay(result.Sc2Replay);
                
                if (dsReplay is null)
                {
                    continue;
                }

                var replayDto = Parse.GetReplayDto(dsReplay, md5);
                var json = JsonSerializer.Serialize(replayDto);
                File.WriteAllText(Path.ChangeExtension(result.ReplayPath, "json"), json);
            }
            else
            {
                Console.Write(result.Exception);
            }
        }

        Console.Write($"{replays.Count} new tourney replay jsons created.");
    }
}