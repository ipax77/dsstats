using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using pax.dsstats.parser;
using s2protocol.NET;

namespace dsstats.decodecli;

class Program
{
    static readonly string replayFolder = "/data/ds/decode/input";
    static readonly string outputFolder = "/data/ds/decode/output";
    static readonly string errorFolder = "/data/ds/decode/error";
    static readonly string doneFolder = "/data/ds/decode/done";
    static readonly List<string> doneFiles = new();
    static readonly List<string> errorFiles = new();

    static async Task Main(string[] args)
    {
        if (args.Length > 0)
        {
            await Tourney.CreateTourneyJsons(args[0]);
            return;
        }

        var files = Directory.GetFiles(replayFolder, "*SC2Replay", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            Console.WriteLine("no replays found.");
            return;
        }

        Console.WriteLine($"found {files.Length} replays. Decoding ...");

        Stopwatch sw = Stopwatch.StartNew();

        ReplayDecoder decoder = new();

        ReplayDecoderOptions options = new ReplayDecoderOptions()
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

        foreach (var file in files)
        {
            var outputFile = GetOutputFile(file);
            try
            {
                var sc2Replay = await decoder.DecodeAsync(file, options);

                if (sc2Replay is null)
                {
                    ReportError(file, outputFile, "sc2Replay was null.");
                    continue;
                }

                var dsReplay = Parse.GetDsReplay(sc2Replay);

                if (dsReplay is null)
                {
                    ReportError(file, outputFile, "dsReplay was null.");
                    continue;
                }

                var replayDto = Parse.GetReplayDto(dsReplay, md5);

                if (replayDto is null)
                {
                    ReportError(file, outputFile, "replayDto was null.");
                    continue;
                }

                var json = JsonSerializer.Serialize(replayDto);
                File.WriteAllText(outputFile, json);
                doneFiles.Add(file);
            }
            catch (Exception ex)
            {
                ReportError(file, outputFile, ex.Message);
            }
        }

        decoder.Dispose();
        GC.WaitForPendingFinalizers();

        foreach (var file in errorFiles)
        {
            File.Move(file, Path.Combine(errorFolder, Path.GetFileName(file)));
        }

        foreach (var file in doneFiles)
        {
            File.Move(file, Path.Combine(doneFolder, Path.GetFileName(file)));
        }

        sw.Stop();
        Console.WriteLine($"replays decoded in {sw.ElapsedMilliseconds}ms");
    }

    public static string GetOutputFile(string inputFile)
    {
        var name = Path.GetFileNameWithoutExtension(inputFile);
        var output = Path.Combine(outputFolder, name + ".json");
        int i = 1;
        while (File.Exists(output))
        {
            output = Path.Combine(outputFolder, name + $"_{i}.json");
            i++;
        }
        return output;
    }

    public static void ReportError(string inputFile, string outputFile, string error)
    {
        var errorFile = outputFile + ".error";
        int i = 1;
        while (File.Exists(errorFile))
        {
            errorFile = outputFile + $"_{i}.error";
            i++;
        }
        File.WriteAllText(errorFile, error);
        errorFiles.Add(inputFile);
        Console.WriteLine($"error decoding {inputFile}: {error}");
    }
}
