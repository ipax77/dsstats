﻿using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
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
        Console.CancelKeyPress += Console_CancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

        // DEBUG
        if (args.Length == 0)
        {
            // await Decode("C:\\Users\\pax77\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer", "/data/ds/errorReplay/dummy", 8);
            //await CompareDb.CompareJsonToDb("/data/ds/errorReplay/dummy");

            //var mmrService = new MmrService.MmrService();
            //await mmrService.ConfidenceTest();
        }

        if (args.Length < 1)
        {
            WriteHowToUse();
            return;
        }

        if (args[0] == "decode")
        {
            if (args.Length != 3)
            {
                WriteHowToUse(args[0]);
                return;
            }

            if (!Directory.Exists(args[1]) || !Directory.Exists(args[2]))
            {
                Console.WriteLine($"Directory not found :(");
            }
            await Decode(args[1], args[2]);
        }
        else if (args[0] == "unzip")
        {
            if (args.Length != 3)
            {
                WriteHowToUse(args[0]);
                return;
            }

            await Unzip(args[1], args[2]);
        }
        else if (args[0] == "tourneyjob")
        {
            if (args.Length != 3 || !int.TryParse(args[1], out int cores))
            {
                WriteHowToUse(args[0]);
                return;
            }

            if (!Directory.Exists(args[2]))
            {
                Console.WriteLine($"tourney folder {args[2]} not found.");
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                TourneyService.DecodeTourneyFolders(cores, args[2], cts.Token).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"tourneyjob failed: {ex.Message}");
            }
            sw.Stop();
            Console.WriteLine($"tourneyjob done in {sw.ElapsedMilliseconds}ms");
        }
        else if (args[0] == "mmr")
        {
            if (args.Length < 2)
            {
                WriteHowToUse(args[0]);
                return;
            }

            var mmrService = new MmrService.MmrService();

            if (args[1] == "derivation")
            {
                var results = await mmrService.DerivationTest();
            }
            else if (args[1] == "confidence")
            {
                int playerId = 10758;
                if (args.Length == 3)
                {
                    if (!int.TryParse(args[2], out playerId))
                    {
                        playerId = 10758;
                    }
                }

                var confidence = await mmrService.ConfidenceTest(playerId);
                Console.WriteLine("MMR-Confidence Player {0} => {1}", playerId, Math.Round(confidence, 5));
            }
            else
            {
                WriteHowToUse(args[0]);
                return;
            }
        }
        else
        {
            if (args.Length > 0)
            {
                Console.WriteLine($"arg: {args[0]}");
            }
            WriteHowToUse();
            return;
        }
    }

    private static void AppDomain_ProcessExit(object? sender, EventArgs e)
    {
        cts.Cancel();
        cts.Dispose();
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        cts.Cancel();
        cts.Dispose();
    }

    private static void WriteHowToUse()
    {
        var versionString = Assembly.GetEntryAssembly()?
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                .InformationalVersion
                                .ToString();

        Console.WriteLine("----------------------------");
        Console.WriteLine($"dsstats.cli v{versionString}");
        Console.WriteLine("----------------------------");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  decode <replayPath> <outputPath>");
        Console.WriteLine("  unzip <base64Zipfile> <outputPath>");
        Console.WriteLine("  tourneyjob <int:cpuCoresToUse>");
        Console.WriteLine("  mmr <string:mode>");
    }

    private static void WriteHowToUse(string firstArg)
    {
        switch (firstArg)
        {
            case "mmr":
                {
                    Console.WriteLine("  mmr <string:mode>");
                    Console.WriteLine("    derivation (...explain...)");
                    Console.WriteLine("    confidence <int:playerId> (...explain...)");
                }
                break;
            default:
                break;
        }
    }

    private static async Task Unzip(string base64Zipfile, string outputPath)
    {
        if (!File.Exists(base64Zipfile))
        {
            Console.WriteLine($"file not found: {base64Zipfile}");
            return;
        }

        if (!Directory.Exists(outputPath))
        {
            Console.WriteLine($"directory not found: {outputPath}");
            return;
        }

        var bytes = Convert.FromBase64String(await File.ReadAllTextAsync(base64Zipfile, Encoding.UTF8));
        using var msi = new MemoryStream(bytes);
        var mso = new MemoryStream();
        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            await gs.CopyToAsync(mso);
        }
        mso.Position = 0;

        var replays = await JsonSerializer
            .DeserializeAsync<List<ReplayDto>>(mso);

        var outputFile = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(base64Zipfile)}.json");
        await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(replays, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task Decode(string replaysPath, string outputPath, int threads = 8)
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

        var replayPaths = Directory.GetFiles(replaysPath, "Direct Strike*.SC2Replay", SearchOption.TopDirectoryOnly);

        await foreach (var decodeResult in decoder.DecodeParallelWithErrorReport(replayPaths, threads, decoderOptions, cts.Token))
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
        var tempFileName = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(replayDto.FileName) + ".temp");
        File.WriteAllText(tempFileName, json);
        File.Move(tempFileName, outputFileName);
    }
}
