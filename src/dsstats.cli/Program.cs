﻿using System.Reflection;
using System.Text.Json;
using pax.dsstats.parser;
using pax.dsstats.shared;
using s2protocol.NET;

namespace dsstats.cli;
class Program
{
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
    private static readonly CancellationTokenSource cts = new();

    static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            WriteHowToUse();
            return;
        }

        if (args[0] == "decode")
        {
            if (!Directory.Exists(args[1]) || !Directory.Exists(args[2]))
            {
                Console.WriteLine($"Directory not found :(");
            }
            await Decode(args[1], args[2]);
        }
        else
        {
            WriteHowToUse();
            return;
        }
    }

    private static void WriteHowToUse()
    {
        var versionString = Assembly.GetEntryAssembly()?
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                .InformationalVersion
                                .ToString();

        Console.WriteLine($"dsstats.cli v{versionString}");
        Console.WriteLine("-------------");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  decode <replayPath> <outputPath>");
    }

    private static async Task Decode(string replaysPath, string outputPath)
    {
        ReplayDecoder decoder = new(assemblyPath);

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

        var replayPaths = Directory.GetFiles(replaysPath, "*.SC2Replay", SearchOption.TopDirectoryOnly);

        await foreach (var decodeResult in decoder.DecodeParallelWithErrorReport(replayPaths, 4, decoderOptions, cts.Token))
        {
            if (cts.IsCancellationRequested)
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
                    SaveReplay(dtoRep, outputPath);
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
    }

    private static void SaveReplay(ReplayDto? replayDto, string outputPath)
    {
        if (replayDto == null || String.IsNullOrEmpty(replayDto.FileName))
        {
            return;
        }
        var json = JsonSerializer.Serialize(replayDto, new JsonSerializerOptions() { WriteIndented = true });
        var outputFileName = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(replayDto.FileName) + ".json");
        File.WriteAllText(outputFileName, json);
    }
}